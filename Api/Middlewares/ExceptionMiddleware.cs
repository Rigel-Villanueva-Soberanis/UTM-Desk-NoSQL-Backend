using Application.Common;
using MongoDB.Driver;
using System.Net;

namespace Api.Middlewares;

/// <summary>
/// Middleware global para capturar excepciones no manejadas y devolver respuestas estandarizadas.
/// NUNCA expone detalles internos (stack trace, nombres de clases, queries) al cliente.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            // Error de negocio controlado → código HTTP correcto según el código del dominio
            var statusCode = ex.Code switch
            {
                ApiErrors.NotFound         => (int)HttpStatusCode.NotFound,       // 404
                ApiErrors.Unauthorized     => (int)HttpStatusCode.Unauthorized,   // 401
                ApiErrors.Conflict         => (int)HttpStatusCode.Conflict,       // 409
                ApiErrors.ValidationFailed => (int)HttpStatusCode.BadRequest,     // 400
                _                          => (int)HttpStatusCode.BadRequest      // 400 por defecto
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                code    = ex.Code,
                message = ex.Message
            });
        }
        catch (MongoException ex)
        {
            // Error de MongoDB → mensaje genérico, los detalles solo van al log del servidor
            _logger.LogError(ex, "Error de MongoDB en {Path}", context.Request.Path);

            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "No se pudo completar la operación. Intenta de nuevo más tarde."
            });
        }
        catch (FormatException ex)
        {
            // Datos con formato incorrecto (ej: ObjectId inválido, fecha mal formada)
            _logger.LogWarning(ex, "FormatException en {Path}", context.Request.Path);

            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "El formato de los datos enviados no es correcto."
            });
        }
        catch (Exception ex)
        {
            // Error inesperado → mensaje genérico, los detalles solo van al log
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Error interno del servidor. Contacta al administrador."
            });
        }
    }
}
