using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para que Soporte de TI actualice un usuario completo.
/// No incluye <c>usuario</c> ni <c>password</c>.
/// </summary>
public class UpdateUsuarioRequest
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

    [Required(ErrorMessage = "El estado de cuenta es obligatorio.")]
    [RegularExpression(@"^(activo|inactivo|bloqueado)$", ErrorMessage = "El estado debe ser 'activo', 'inactivo' o 'bloqueado'.")]
    public string EstadoCuenta { get; set; } = "activo";

    [Required(ErrorMessage = "El rol del sistema es obligatorio.")]
    public string RolSistema { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clasificación del rol es obligatoria.")]
    public string ClasificacionRol { get; set; } = string.Empty;
}
