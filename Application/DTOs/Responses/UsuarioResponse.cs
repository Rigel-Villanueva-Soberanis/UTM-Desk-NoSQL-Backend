namespace Application.DTOs.Responses;

/// <summary>
/// Datos del usuario devueltos al cliente.
/// NUNCA incluye password_hash ni datos internos de seguridad.
/// </summary>
public class UsuarioResponse
{
    public string Id { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;

    /// <summary>Nombre completo concatenado para mostrar en UI.</summary>
    public string NombreCompleto => $"{Nombres} {ApellidoPaterno} {ApellidoMaterno}".Trim();

    public string Usuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string EstadoCuenta { get; set; } = string.Empty;
    public string RolSistema { get; set; } = string.Empty;
    public string ClasificacionRol { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
