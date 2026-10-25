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

    /// <summary>Folio de la incidencia más reciente que empieza con <paramref name="prefijo"/> (ej. "INC-2026-"), o null.</summary>
    Task<string?> ObtenerUltimoFolioAsync(string prefijo);
}

/// <summary>
/// Filtros ya validados y normalizados por el servicio.
/// <see cref="IdReportante"/> lo pone el servicio (obligatorio para Reportante), nunca el cliente.
/// </summary>
public record IncidenciaConsulta(
    int Pagina,
    int TamanoPagina,
    string? IdReportante,
    string? Estado,
    string? Prioridad,
    string? Categoria,
    string? Edificio);
