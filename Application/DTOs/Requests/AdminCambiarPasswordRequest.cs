using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para que Soporte de TI cambie la contraseña de cualquier usuario.
/// </summary>
public class AdminCambiarPasswordRequest
{
    /// <summary>Nueva contraseña en texto plano. El backend la encripta con BCrypt.</summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
