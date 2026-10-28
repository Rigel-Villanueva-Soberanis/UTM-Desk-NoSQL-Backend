using Application.Common.ErrorCatalog;
using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación MongoDB del repositorio de incidencias (colección "Incidencias").
/// </summary>
public class IncidenciaRepository : IIncidenciaRepository
{
    private readonly IMongoCollection<Incidencia> _collection;

    public IncidenciaRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Incidencia>("Incidencias");
    }

    public async Task<Incidencia> CreateAsync(Incidencia incidencia)
    {
        try
        {
            await _collection.InsertOneAsync(incidencia);
            return incidencia;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey
                                             && ex.WriteError.Message.Contains("folio"))
        {
            // El índice único ux_folio_incidencia rechazó el folio → el servicio resincroniza y reintenta
            throw new FolioDuplicadoException(incidencia.Folio);
        }
    }

    /// <summary>Una sola lectura por _id: el expediente completo viene embebido.</summary>
    public async Task<Incidencia?> GetByIdAsync(string id)
        => await _collection.Find(i => i.Id == id).FirstOrDefaultAsync();

    /// <summary>
    /// Listado paginado ordenado por fecha de creación (más recientes primero).
    /// Con IdReportante usa el índice ix_reportante_fecha; con IdTecnico, ix_tecnico_estado.
    /// </summary>
    public async Task<(IReadOnlyList<Incidencia> Items, long Total)> GetPaginadoAsync(IncidenciaConsulta consulta)
    {
        var filtro = ConstruirFiltro(consulta);

        var total = await _collection.CountDocumentsAsync(filtro);

        var items = await _collection.Find(filtro)
            .SortByDescending(i => i.Fechas.Creacion)
            .Skip((consulta.Pagina - 1) * consulta.TamanoPagina)
            .Limit(consulta.TamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// $text sobre el índice ix_texto_incidencias (titulo peso 10, descripcion peso 5, idioma español).
    /// La proyección { score: { $meta: "textScore" } } trae el documento completo + su puntuación,
    /// y se ordena por esa puntuación (más relevante primero).
    /// </summary>
    public async Task<(IReadOnlyList<IncidenciaConRelevancia> Items, long Total)> BuscarTextoAsync(IncidenciaConsulta consulta, string texto)
    {
        const string CampoScore = "score";

        var filtro = Builders<Incidencia>.Filter.Text(texto) & ConstruirFiltro(consulta);

        var total = await _collection.CountDocumentsAsync(filtro);

        var documentos = await _collection.Find(filtro)
            .Project<BsonDocument>(Builders<Incidencia>.Projection.MetaTextScore(CampoScore))
            .Sort(Builders<Incidencia>.Sort.MetaTextScore(CampoScore).Descending(i => i.Fechas.Creacion))
            .Skip((consulta.Pagina - 1) * consulta.TamanoPagina)
            .Limit(consulta.TamanoPagina)
            .ToListAsync();

        var items = documentos
            .Select(d => new IncidenciaConRelevancia(
                BsonSerializer.Deserialize<Incidencia>(d),          // "score" se ignora (BsonIgnoreExtraElements)
                Math.Round(d[CampoScore].ToDouble(), 4)))
            .ToList();

        return (items, total);
    }

    /// <summary>Filtros comunes del listado y la búsqueda (los valores ya vienen validados por el servicio).</summary>
    private static FilterDefinition<Incidencia> ConstruirFiltro(IncidenciaConsulta consulta)
    {
        var f      = Builders<Incidencia>.Filter;
        var filtro = f.Empty;

        if (consulta.IdReportante is not null) filtro &= f.Eq(i => i.DatosReportante.IdUsuario, consulta.IdReportante);
        if (consulta.IdTecnico    is not null) filtro &= f.Eq(i => i.TecnicoAsignado!.IdUsuario, consulta.IdTecnico);
        if (consulta.Estado       is not null) filtro &= f.Eq(i => i.Estado, consulta.Estado);
        if (consulta.Prioridad    is not null) filtro &= f.Eq(i => i.Prioridad, consulta.Prioridad);
        if (consulta.Categoria    is not null) filtro &= f.Eq(i => i.Categoria, consulta.Categoria);
        if (consulta.Edificio     is not null) filtro &= f.Eq(i => i.Ubicacion.Edificio, consulta.Edificio);
        if (consulta.Tag          is not null) filtro &= f.AnyEq(i => i.Tags, consulta.Tag);
        if (consulta.Desde        is not null) filtro &= f.Gte(i => i.Fechas.Creacion, consulta.Desde.Value);
        if (consulta.Hasta        is not null) filtro &= f.Lt(i => i.Fechas.Creacion, consulta.Hasta.Value);

        // Asignada: tecnico_asignado con datos / tecnico_asignado null (o sin el campo)
        if (consulta.Asignada == true)  filtro &= f.Ne(i => i.TecnicoAsignado, null);
        if (consulta.Asignada == false) filtro &= f.Eq(i => i.TecnicoAsignado, null);

        // Proveedor externo: tecnico_externo con datos y sin requiere_servicio_externo = false
        var conExterno = f.Type(i => i.TecnicoExterno, BsonType.Document)
                       & f.Ne(i => i.TecnicoExterno!.RequiereServicioExterno, false);
        if (consulta.ConProveedorExterno == true)  filtro &= conExterno;
        if (consulta.ConProveedorExterno == false) filtro &= f.Not(conExterno);

        return filtro;
    }

    /// <summary>
    /// Cuenta, por técnico, las incidencias asignadas que siguen activas (ni resueltas, cerradas ni canceladas).
    /// Una sola agregación para todos los técnicos; usa el índice ix_tecnico_estado.
    /// </summary>
    public async Task<Dictionary<string, int>> ContarActivasPorTecnicoAsync(IEnumerable<string> idsTecnicos)
    {
        var ids = idsTecnicos.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, int>();

        var f      = Builders<Incidencia>.Filter;
        var filtro = f.In(i => i.TecnicoAsignado!.IdUsuario, ids)
                   & f.Nin(i => i.Estado, new[] { "resuelta", "cerrada", "cancelada" });

        var conteos = await _collection.Aggregate()
            .Match(filtro)
            .Group(i => i.TecnicoAsignado!.IdUsuario, g => new { Id = g.Key, Total = g.Count() })
            .ToListAsync();

        return conteos.ToDictionary(c => c.Id, c => c.Total);
    }

    /// <summary>
    /// Arma un solo update con todos los cambios ($set, $inc, $push, $addToSet) + las entradas de bitácora
    /// y lo aplica de forma atómica. Devuelve el documento ya actualizado.
    /// Caso especial: MongoDB no permite $addToSet y $pull sobre "tags" en el mismo update, así que si
    /// llegan ambos se mandan dos updates ordenados en un solo viaje (BulkWrite): primero todo lo demás
    /// + $addToSet, y después el $pull.
    /// </summary>
    public async Task<Incidencia?> AplicarCambiosAsync(string id, CambiosIncidencia cambios)
    {
        var u       = Builders<Incidencia>.Update;
        var updates = new List<UpdateDefinition<Incidencia>>
        {
            u.PushEach(i => i.HistorialSeguimiento, cambios.Bitacora)   // la bitácora siempre va
        };

        if (cambios.NuevoEstado is not null)  updates.Add(u.Set(i => i.Estado, cambios.NuevoEstado));
        if (cambios.NuevoTecnico is not null) updates.Add(u.Set(i => i.TecnicoAsignado, cambios.NuevoTecnico));
        if (cambios.CambiarResolucion)        updates.Add(u.Set(i => i.Fechas.Resolucion, cambios.FechaResolucion));
        if (cambios.HorasASumar is not null)
            updates.Add(cambios.InicializarHoras
                ? u.Set(i => i.HorasInvertidas, cambios.HorasASumar)
                : u.Inc(i => i.HorasInvertidas, cambios.HorasASumar));
        if (cambios.EvidenciasNuevas is { Count: > 0 })
            updates.Add(u.PushEach(i => i.EvidenciasAdjuntas, cambios.EvidenciasNuevas));
        if (cambios.NuevoTecnicoExterno is not null)
            updates.Add(u.Set(i => i.TecnicoExterno, cambios.NuevoTecnicoExterno));

        if (cambios.NuevosDatosGenerales is { } datos)
        {
            updates.Add(u.Set(i => i.Titulo,            datos.Titulo));
            updates.Add(u.Set(i => i.Descripcion,       datos.Descripcion));
            updates.Add(u.Set(i => i.Ubicacion,         datos.Ubicacion));
            updates.Add(u.Set(i => i.DetalleEspecifico, datos.DetalleEspecifico));
        }

        var agregar = cambios.TagsAgregar is { Count: > 0 };
        var quitar  = cambios.TagsQuitar  is { Count: > 0 };
        if (agregar)
            updates.Add(cambios.InicializarTags
                ? u.Set(i => i.Tags, cambios.TagsAgregar!.ToList())
                : u.AddToSetEach(i => i.Tags, cambios.TagsAgregar));

        var f      = Builders<Incidencia>.Filter;
        var porId  = f.Eq(i => i.Id, id);
        var filtro = porId;
        if (cambios.EstadoEsperado is not null)
            filtro &= f.Eq(i => i.Estado, cambios.EstadoEsperado);

        // Solo $pull (sin $addToSet): va en el mismo update
        if (quitar && !agregar)
            updates.Add(PullTags(cambios.TagsQuitar!));

        if (!(quitar && agregar))
        {
            return await _collection.FindOneAndUpdateAsync(
                filtro,
                u.Combine(updates),
                new FindOneAndUpdateOptions<Incidencia> { ReturnDocument = ReturnDocument.After });
        }

        // $addToSet + $pull: dos updates ordenados en un solo viaje.
        // El segundo usa el estado que queda tras el primero, así no se aplica si el primero no encontró la incidencia.
        var filtroPull = porId;
        var estadoTrasPrimero = cambios.NuevoEstado ?? cambios.EstadoEsperado;
        if (estadoTrasPrimero is not null)
            filtroPull &= f.Eq(i => i.Estado, estadoTrasPrimero);

        var resultado = await _collection.BulkWriteAsync(
        [
            new UpdateOneModel<Incidencia>(filtro,     u.Combine(updates)),
            new UpdateOneModel<Incidencia>(filtroPull, PullTags(cambios.TagsQuitar!))
        ], new BulkWriteOptions { IsOrdered = true });

        if (resultado.MatchedCount == 0)
            return null;

        return await GetByIdAsync(id);
    }

    /// <summary>{ $pull: { tags: { $in: [...] } } } — quita todas las etiquetas indicadas.</summary>
    private static UpdateDefinition<Incidencia> PullTags(IEnumerable<string> etiquetas)
        => new BsonDocument("$pull", new BsonDocument("tags", new BsonDocument("$in", new BsonArray(etiquetas))));

    /// <summary>Distinct sobre el arreglo tags: MongoDB devuelve cada etiqueta una sola vez.</summary>
    public async Task<List<string>> GetTagsDistintosAsync()
    {
        var cursor = await _collection.DistinctAsync<string>("tags", FilterDefinition<Incidencia>.Empty);
        return await cursor.ToListAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var resultado = await _collection.DeleteOneAsync(i => i.Id == id);
        return resultado.DeletedCount > 0;
    }

    /// <summary>
    /// Folio de la incidencia más reciente del prefijo (ej. "INC-2026-").
    /// El filtro "empieza con" usa el índice ux_folio_incidencia.
    /// </summary>
    public async Task<string?> ObtenerUltimoFolioAsync(string prefijo)
    {
        var filtro = Builders<Incidencia>.Filter.Regex(
            i => i.Folio, new BsonRegularExpression("^" + Regex.Escape(prefijo)));

        var ultima = await _collection.Find(filtro)
            .SortByDescending(i => i.Fechas.Creacion)
            .Limit(1)
            .FirstOrDefaultAsync();

        return ultima?.Folio;
    }
}
