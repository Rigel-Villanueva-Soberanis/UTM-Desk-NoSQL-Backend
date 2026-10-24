using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para que un usuario cambie su propia contraseña.
/// Debe proporcionar la contraseña actual para confirmar identidad.
/// </summary>
public class CambiarMiPasswordRequest
{
    /// <summary>Contraseña actual. Se verifica contra el hash guardado en BD.</summary>
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    /// <summary>Nueva contraseña. Mínimo 8 caracteres.</summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
