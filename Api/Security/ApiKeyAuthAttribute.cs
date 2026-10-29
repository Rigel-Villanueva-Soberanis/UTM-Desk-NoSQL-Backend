using System.Security.Cryptography;
using System.Text;
using Api.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Security;

/// <summary>Exige el header X-Api-Key (comparación en tiempo constante).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var esperada = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()[ApiKey.ConfigSection];

        var recibida = context.HttpContext.Request.Headers[ApiKey.HeaderName].FirstOrDefault();

        // Si no hay clave configurada, el endpoint queda cerrado.
        if (string.IsNullOrWhiteSpace(esperada) || string.IsNullOrEmpty(recibida) ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(esperada)),
                SHA256.HashData(Encoding.UTF8.GetBytes(recibida))))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("API Key inválida o ausente"))
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}
