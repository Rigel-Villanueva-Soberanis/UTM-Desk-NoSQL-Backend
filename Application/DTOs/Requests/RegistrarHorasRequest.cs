using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Horas trabajadas en una sesión; se SUMAN a las que ya tenía la incidencia.</summary>
public class RegistrarHorasRequest
{
    /// <summary>Horas de la sesión (de 0.25 a 24). Ej: 1.5 = una hora y media.</summary>
    /// <example>2</example>
    [Range(0.25, 24, ErrorMessage = "Las horas deben estar entre 0.25 y 24.")]
    public double Horas { get; set; }

    /// <summary>Comentario opcional de la sesión de trabajo.</summary>
    /// <example>Cableado del rack B.</example>
    [StringLength(300, ErrorMessage = "El comentario no puede tener más de 300 caracteres.")]
    public string? Comentario { get; set; }
}
