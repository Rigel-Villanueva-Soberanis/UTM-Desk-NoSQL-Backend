namespace Application.DTOs.Requests;

/// <summary>
/// Datos que el cliente envía para iniciar sesión.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Nombre de usuario. Ej: "rigel.villanueva"
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano — se compara contra el hash en BD.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
