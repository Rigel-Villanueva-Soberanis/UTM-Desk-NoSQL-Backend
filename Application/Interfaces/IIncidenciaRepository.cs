using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de incidencias.
/// </summary>
public interface IIncidenciaRepository
{
    Task<Incidencia> CreateAsync(Incidencia incidencia);
    Task<Incidencia?> GetByIdAsync(string id);
    Task<(IReadOnlyList<Incidencia> Items, long Total)> GetPaginadoAsync(IncidenciaConsulta consulta);

    /// <summary>
    /// Búsqueda full-text ($text) con el índice de texto de titulo y descripcion (español),
    /// ordenada por relevancia (textScore) de mayor a menor. Acepta los mismos filtros del listado.
    /// </summary>
    Task<(IReadOnlyList<IncidenciaConRelevancia> Items, long Total)> BuscarTextoAsync(IncidenciaConsulta consulta, string texto);

    /// <summary>
    /// ÚNICA forma de modificar una incidencia existente. Aplica los cambios y SIEMPRE agrega
    /// las entradas de bitácora, todo en una sola operación atómica.
    /// Devuelve la incidencia ya actualizada, o null si no existe o si su estado ya no es
    /// <see cref="CambiosIncidencia.EstadoEsperado"/> (otra persona la cambió al mismo tiempo).
    /// </summary>
    Task<Incidencia?> AplicarCambiosAsync(string id, CambiosIncidencia cambios);

    /// <summary>Cuántas incidencias sin terminar (ni resueltas, cerradas ni canceladas) tiene asignadas cada técnico.</summary>
    Task<Dictionary<string, int>> ContarActivasPorTecnicoAsync(IEnumerable<string> idsTecnicos);

    /// <summary>Todas las etiquetas distintas usadas en las incidencias (distinct sobre el arreglo tags).</summary>
    Task<List<string>> GetTagsDistintosAsync();

    /// <summary>Borrado físico del documento. Devuelve false si ya no existía.</summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>Folio de la incidencia más reciente que empieza con <paramref name="prefijo"/> (ej. "INC-2026-"), o null.</summary>
    Task<string?> ObtenerUltimoFolioAsync(string prefijo);
}

/// <summary>
/// Cambios a aplicar sobre una incidencia. Solo se aplican las propiedades con valor.
/// <see cref="Bitacora"/> es obligatoria: ningún cambio se guarda sin su registro en la bitácora.
/// </summary>
public record CambiosIncidencia
{
    /// <summary>Entradas para historial_seguimiento ($push). Obligatorio.</summary>
    public required IReadOnlyList<SeguimientoIncidencia> Bitacora { get; init; }

    /// <summary>Solo aplica si el estado actual sigue siendo este (evita choques entre dos usuarios).</summary>
    public string? EstadoEsperado { get; init; }

    public string? NuevoEstado { get; init; }
    public UsuarioReferencia? NuevoTecnico { get; init; }

    /// <summary>true para modificar fechas.resolucion con <see cref="FechaResolucion"/> (null la limpia).</summary>
    public bool CambiarResolucion { get; init; }
    public DateTime? FechaResolucion { get; init; }

    /// <summary>Horas a sumar con $inc.</summary>
    public double? HorasASumar { get; init; }

    /// <summary>true si horas_invertidas está en null: se guarda el valor con $set (Mongo no puede hacer $inc sobre null).</summary>
    public bool InicializarHoras { get; init; }

    /// <summary>Evidencias a agregar con $push.</summary>
    public IReadOnlyList<EvidenciaIncidencia>? EvidenciasNuevas { get; init; }

    /// <summary>Etiquetas a agregar con $addToSet (no se duplican).</summary>
    public IReadOnlyList<string>? TagsAgregar { get; init; }

    /// <summary>Etiquetas a quitar con $pull.</summary>
    public IReadOnlyList<string>? TagsQuitar { get; init; }

    /// <summary>true si tags está en null: se guarda <see cref="TagsAgregar"/> con $set (Mongo no puede hacer $addToSet sobre null).</summary>
    public bool InicializarTags { get; init; }

    /// <summary>Subdocumento tecnico_externo completo ($set).</summary>
    public TecnicoExterno? NuevoTecnicoExterno { get; init; }

    /// <summary>Corrección de la captura: título, descripción, ubicación y detalle específico ($set).</summary>
    public DatosGeneralesIncidencia? NuevosDatosGenerales { get; init; }
}

/// <summary>Datos generales editables de una incidencia (PUT).</summary>
public record DatosGeneralesIncidencia(string Titulo, string Descripcion, UbicacionIncidencia Ubicacion, MongoDB.Bson.BsonValue DetalleEspecifico);

/// <summary>
/// Filtros ya validados y normalizados por el servicio.
/// <see cref="IdReportante"/> e <see cref="IdTecnico"/> los pone el servicio según el rol, nunca el cliente.
/// </summary>
public record IncidenciaConsulta(
    int Pagina,
    int TamanoPagina,
    string? IdReportante,
    string? IdTecnico,
    string? Estado,
    string? Prioridad,
    string? Categoria,
    string? Edificio,
    bool? Asignada,
    string? Tag = null,
    bool? ConProveedorExterno = null,
    DateTime? Desde = null,
    DateTime? Hasta = null);

/// <summary>Incidencia encontrada por la búsqueda full-text con su puntuación (textScore).</summary>
public record IncidenciaConRelevancia(Incidencia Incidencia, double Relevancia);
