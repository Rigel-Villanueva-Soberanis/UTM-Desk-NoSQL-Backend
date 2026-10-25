using Api.Security;
using Microsoft.OpenApi.Models;

namespace Api.Configuration;

/// <summary>
/// Extensiones para la configuración de Swagger / OpenAPI.
/// </summary>
public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.OperationFilter<ApiKeyOperationFilter>();

            // Documentación tomada de los comentarios /// de controllers y DTOs
            // (summary, remarks, response, example). Así no se llenan los controllers de atributos.
            foreach (var archivo in new[] { "Api.xml", "Application.xml" })
            {
                var ruta = Path.Combine(AppContext.BaseDirectory, archivo);
                if (File.Exists(ruta))
                    c.IncludeXmlComments(ruta);
            }

            // JWT Bearer en Swagger UI (el candadito 🔒)
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "Bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Pega aquí tu token JWT. Ejemplo: eyJhbGci..."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
