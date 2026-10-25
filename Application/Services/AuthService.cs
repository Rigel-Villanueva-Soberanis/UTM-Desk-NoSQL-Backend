using Application.Common.ErrorsUsuarios;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Services;

/// <summary>
/// Servicio de autenticación.
/// Valida credenciales, verifica el hash BCrypt y genera el token JWT.
/// </summary>
public class AuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Protección anti-inyección NoSQL (endpoint público, mayor riesgo)
        NoSqlSanitizer.ValidarTodos(
            (request.Usuario,  "usuario"),
            (request.Password, "password")
        );

        // 1. Buscar el usuario por su nombre de usuario
        var usuario = await _usuarioRepository.GetByUsernameAsync(request.Usuario.Trim().ToLowerInvariant())
            ?? throw AuthErrors.CredencialesInvalidas();

        // 2. Verificar que la cuenta esté activa
        if (usuario.EstadoCuenta != "activo")
            throw AuthErrors.CuentaInactiva();

        // 3. Verificar la contraseña contra el hash BCrypt guardado en BD
        var passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        if (!passwordValida)
            throw AuthErrors.CredencialesInvalidas();

        // 4. Generar el token JWT
        var token = GenerarToken(usuario.Id, usuario.Username, usuario.NombreCompleto, usuario.RolSistema, usuario.ClasificacionRol);
        var expiraEn = int.Parse(_configuration["Jwt:ExpiresInSeconds"] ?? "86400");

        return new LoginResponse
        {
            Token            = token,
            TokenType        = "Bearer",
            ExpiresIn        = expiraEn,
            ExpiresAt        = DateTime.UtcNow.AddSeconds(expiraEn),
            Id               = usuario.Id,
            Nombres          = usuario.Nombres,
            ApellidoPaterno  = usuario.ApellidoPaterno,
            ApellidoMaterno  = usuario.ApellidoMaterno,
            Usuario          = usuario.Username,
            Correo           = usuario.Correo,
            RolSistema       = usuario.RolSistema,
            ClasificacionRol = usuario.ClasificacionRol
        };
    }

    // ─── Generación del token ─────────────────────────────────────────────────

    private string GenerarToken(string id, string username, string nombreCompleto, string rol, string clasificacion)
    {
        var claveSecreta = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada.");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiraEn    = int.Parse(_configuration["Jwt:ExpiresInSeconds"] ?? "86400");

        // Claims = datos que van DENTRO del token (se pueden leer sin llamar al API)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        id),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim("nombre_completo",                  nombreCompleto), // ← para datos_reportante de incidencias
            new Claim(ClaimTypes.Role,                    rol),          // ← para [Authorize(Roles="...")]
            new Claim("rol_sistema",                      rol),          // ← claim personalizado legible
            new Claim("clasificacion_rol",                clasificacion),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer:   _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddSeconds(expiraEn),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
