using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Tiempo trabajado en una sesión; se SUMA al que ya tenía la incidencia.</summary>
public class RegistrarHorasRequest
{
    /// <summary>Horas completas de la sesión (0 a 24). Ej: para 1 h 30 min se manda 1.</summary>
    /// <example>1</example>
    [Range(0, 24, ErrorMessage = "Las horas deben estar entre 0 y 24.")]
    public int Horas { get; set; }

    /// <summary>Minutos de la sesión (0 a 59). Ej: para 1 h 30 min se manda 30.</summary>
    /// <example>30</example>
    [Range(0, 59, ErrorMessage = "Los minutos deben estar entre 0 y 59.")]
    public int Minutos { get; set; }

    /// <summary>Comentario opcional de la sesión de trabajo.</summary>
    /// <example>Cableado del rack B.</example>
    [StringLength(300, ErrorMessage = "El comentario no puede tener más de 300 caracteres.")]
    public string? Comentario { get; set; }
}
