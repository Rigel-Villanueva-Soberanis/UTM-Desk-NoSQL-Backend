namespace Application.DTOs.Requests;

/// <summary>Filtros de GET /api/analitica/tiempos-edificio.</summary>
public class TiemposEdificioFiltroRequest
{
    /// <summary>Resueltas desde este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? FechaDesde { get; set; }

    /// <summary>Resueltas hasta este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-31</example>
    public DateOnly? FechaHasta { get; set; }

    /// <summary>Clave de la categoría.</summary>
    /// <example>computo</example>
    public string? Categoria { get; set; }

    /// <summary>baja, media, alta o critica.</summary>
    /// <example>alta</example>
    public string? Prioridad { get; set; }

    /// <summary>reportes (más reportes primero, por defecto) o tiempo (tiempo promedio más alto primero).</summary>
    /// <example>reportes</example>
    public string? OrdenarPor { get; set; }
}
