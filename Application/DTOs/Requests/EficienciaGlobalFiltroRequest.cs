namespace Application.DTOs.Requests;

/// <summary>Filtros de GET /api/analitica/eficiencia-global (sobre la fecha de creación).</summary>
public class EficienciaGlobalFiltroRequest
{
    /// <summary>Creadas desde este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? FechaDesde { get; set; }

    /// <summary>Creadas hasta este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-31</example>
    public DateOnly? FechaHasta { get; set; }

    /// <summary>Clave de la categoría.</summary>
    /// <example>computo</example>
    public string? Categoria { get; set; }

    /// <summary>Nombre del edificio (tal como aparece en el catálogo edificios).</summary>
    /// <example>Edificio C</example>
    public string? Edificio { get; set; }
}
