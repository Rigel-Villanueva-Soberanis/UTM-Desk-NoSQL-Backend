using Microsoft.AspNetCore.Mvc;

namespace Api.Configuration;

/// <summary>
/// Extensiones para la configuración general de controladores y validación de la API.
/// </summary>
public static class ApiConfigurationExtensions
{
    public static IServiceCollection AddCustomControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Reemplaza el formato por defecto (RFC 7807 con traceId) por nuestro ApiResponse uniforme
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errores = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            e => e.Key,
                            e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
                        );

                    return new ObjectResult(new
                    {
                        success = false,
                        message = "Uno o más campos no son válidos. Revisa los errores.",
                        errors  = errores
                    })
                    { StatusCode = 400 };
                };
            });

        return services;
    }
}
