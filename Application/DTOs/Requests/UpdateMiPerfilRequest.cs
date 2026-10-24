using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para que un usuario edite su propio perfil.
/// No puede cambiar: usuario, contraseña, rol ni estado de cuenta.
/// </summary>
public class UpdateMiPerfilRequest
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(80, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    [StringLength(60, MinimumLength = 2)]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [StringLength(60)]
    public string ApellidoMaterno { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;
}
