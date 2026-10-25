namespace Application.DTOs.Requests;

/// <summary>
/// Filtros para GET /api/Usuario/tecnicos (selector para asignar técnico).
/// Siempre devuelve solo técnicos con cuenta activa.
/// </summary>
public class TecnicoFiltroRequest
{
    /// <summary>Busca en nombres y apellidos a la vez.</summary>
    /// <example>carlos</example>
    public string? Busqueda { get; set; }

    /// <summary>Busca solo en nombres.</summary>
    /// <example>Carlos</example>
    public string? Nombres { get; set; }

    /// <summary>Busca en apellido paterno o materno.</summary>
    /// <example>Dzib</example>
    public string? Apellidos { get; set; }

    /// <summary>Especialidad del técnico (clasificación): mantenimiento, redes, sistemas…</summary>
    /// <example>redes</example>
    public string? Clasificacion { get; set; }
}
