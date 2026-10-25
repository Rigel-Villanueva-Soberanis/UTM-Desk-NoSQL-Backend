using Application.Common;
using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Bson;
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
    /// Con IdReportante usa el índice ix_reportante_fecha.
    /// </summary>
    public async Task<(IReadOnlyList<Incidencia> Items, long Total)> GetPaginadoAsync(IncidenciaConsulta consulta)
    {
        var f      = Builders<Incidencia>.Filter;
        var filtro = f.Empty;

        if (consulta.IdReportante is not null) filtro &= f.Eq(i => i.DatosReportante.IdUsuario, consulta.IdReportante);
        if (consulta.Estado       is not null) filtro &= f.Eq(i => i.Estado, consulta.Estado);
        if (consulta.Prioridad    is not null) filtro &= f.Eq(i => i.Prioridad, consulta.Prioridad);
        if (consulta.Categoria    is not null) filtro &= f.Eq(i => i.Categoria, consulta.Categoria);
        if (consulta.Edificio     is not null) filtro &= f.Eq(i => i.Ubicacion.Edificio, consulta.Edificio);

        var total = await _collection.CountDocumentsAsync(filtro);

        var items = await _collection.Find(filtro)
            .SortByDescending(i => i.Fechas.Creacion)
            .Skip((consulta.Pagina - 1) * consulta.TamanoPagina)
            .Limit(consulta.TamanoPagina)
            .ToListAsync();

        return (items, total);
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
