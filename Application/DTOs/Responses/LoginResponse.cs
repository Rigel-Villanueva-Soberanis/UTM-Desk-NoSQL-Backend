namespace Application.DTOs.Responses;

/// <summary>
/// Respuesta del login: token JWT + datos del usuario.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// El token JWT. Se envía en cada petición protegida como:
    /// Header → Authorization: Bearer {token}
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Siempre "Bearer" — indica al cliente cómo usar el token en el header.
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Segundos de vida del token desde que se generó (8 horas = 28800).
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Fecha y hora exacta en UTC cuando el token expira.
    /// El frontend compara con la hora actual para saber si debe re-loguearse.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    // ─── Datos del usuario ────────────────────────────────────────────────────
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RolSistema { get; set; } = string.Empty;
    public string ClasificacionRol { get; set; } = string.Empty;
}
