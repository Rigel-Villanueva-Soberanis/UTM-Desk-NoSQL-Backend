using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Nota de texto libre del técnico (avance, diagnóstico, notas internas).</summary>
public class NotaSeguimientoRequest
{
    /// <summary>Texto de la nota.</summary>
    /// <example>Se revisó el rack B: el puerto 12 del switch no da enlace. Se pidió refacción.</example>
    [Required(ErrorMessage = "La nota es obligatoria.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "La nota debe tener entre 3 y 2000 caracteres.")]
    public string Nota { get; set; } = null!;   // null si no se envía: así solo sale "obligatoria"
}
