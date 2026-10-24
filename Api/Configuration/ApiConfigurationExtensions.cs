using Application.Common;
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
                // Reemplaza el formato por defecto (RFC 7807 con traceId) por el mismo formato que usa
                // DomainException: errors = [{ campo, mensaje }], así el frontend maneja un solo formato.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errores = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors.Select(err => new ErrorDetalle(
                            NombreCampo(e.Key),
                            string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Valor inválido." : err.ErrorMessage)))
                        .ToList();

                    return new ObjectResult(new
                    {
                        success = false,
                        code    = ApiErrors.ValidationFailed,
                        message = "Uno o más campos no son válidos. Revisa los errores.",
                        errors  = errores
                    })
                    { StatusCode = 400 };
                };
            });

        return services;
    }

    /// <summary>
    /// Convierte la llave de ModelState al nombre que usa el frontend en el JSON.
    /// Ej: "ApellidoPaterno" → "apellidoPaterno", "$.correo" → "correo".
    /// </summary>
    private static string NombreCampo(string llave)
    {
        var campo = llave.StartsWith("$.") ? llave[2..] : llave;
        if (string.IsNullOrEmpty(campo) || campo == "$") return "body";
        return char.ToLowerInvariant(campo[0]) + campo[1..];
    }
}
