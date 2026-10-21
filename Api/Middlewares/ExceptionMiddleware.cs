using Application.Common;
using System.Net;

namespace Api.Middlewares;

/// <summary>
/// Middleware global para capturar excepciones no manejadas y devolver respuestas estandarizadas.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            // Mapeamos el código del dominio al HTTP status correcto
            var statusCode = ex.Code switch
            {
                ApiErrors.NotFound        => (int)HttpStatusCode.NotFound,           // 404
                ApiErrors.Unauthorized    => (int)HttpStatusCode.Unauthorized,       // 401
                ApiErrors.Conflict        => (int)HttpStatusCode.Conflict,           // 409
                ApiErrors.ValidationFailed => (int)HttpStatusCode.BadRequest,        // 400
                _                         => (int)HttpStatusCode.BadRequest          // 400 por defecto
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                code    = ex.Code,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Error interno del servidor.",
                detail  = ex.Message   // ← útil para depuración; quitar en producción
            });
        }
    }
}
