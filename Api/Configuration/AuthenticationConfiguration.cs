using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Configuration;

/// <summary>
/// Extensiones para la configuración de autenticación JWT y manejo de respuestas 401/403.
/// </summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret   = configuration["Jwt:SecretKey"]!;
        var jwtIssuer   = configuration["Jwt:Issuer"]!;
        var jwtAudience = configuration["Jwt:Audience"]!;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtIssuer,
                    ValidAudience            = jwtAudience,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew                = TimeSpan.Zero
                };

                // Respuestas JSON personalizadas para 401 (no autenticado) y 403 (rol denegado)
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode  = 401;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        var mensaje = context.AuthenticateFailure?.Message?.Contains("expired") == true
                            ? "El token ha expirado. Inicia sesión nuevamente."
                            : "No autorizado. Se requiere un token JWT válido.";

                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = mensaje
                        }));
                    },

                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode  = 403;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "Acceso denegado. No tienes permisos para realizar esta acción."
                        }));
                    }
                };
            });

        return services;
    }
}
