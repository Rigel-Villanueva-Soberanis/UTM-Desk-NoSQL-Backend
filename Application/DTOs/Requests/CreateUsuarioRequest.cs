using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para crear un nuevo usuario. Solo Soporte de TI puede usarlo.
/// </summary>
public class CreateUsuarioRequest
{
    /// <summary>Nombres de pila. Ej: "Rigel Alejandro"</summary>
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(80, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    /// <summary>Apellido paterno. Ej: "Villanueva"</summary>
    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    [StringLength(60, MinimumLength = 2)]
    public string ApellidoPaterno { get; set; } = string.Empty;

    /// <summary>Apellido materno. Ej: "Soberanis"</summary>
    [StringLength(60)]
    public string ApellidoMaterno { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de usuario para login. Solo letras, números, puntos y guiones. Sin espacios.
    /// </summary>
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "El usuario solo puede contener letras, números, puntos, guiones y guiones bajos.")]
    [StringLength(50, MinimumLength = 3)]
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Correo institucional. Debe ser único en el sistema.</summary>
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Contraseña en texto plano. El backend la encripta con BCrypt antes de guardar.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Rol del usuario en el sistema.</summary>
    [Required(ErrorMessage = "El rol del sistema es obligatorio.")]
    public string RolSistema { get; set; } = string.Empty;

    /// <summary>Clasificación del rol. Ej: "Docente", "Estudiante", "General".</summary>
    [Required(ErrorMessage = "La clasificación del rol es obligatoria.")]
    public string ClasificacionRol { get; set; } = string.Empty;
}
