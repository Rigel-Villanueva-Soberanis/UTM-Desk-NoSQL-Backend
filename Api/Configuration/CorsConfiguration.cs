namespace Api.Configuration;

/// <summary>
/// CORS: permite que el frontend (Vue) llame a la API desde otro origen (otro puerto/dominio).
/// Los orígenes se leen de appsettings.json → "Cors:OrigenesPermitidos".
/// Si la sección no existe, se permiten los de desarrollo de Vite (https y http en el puerto 5173).
/// </summary>
public static class CorsConfiguration
{
    /// <summary>Nombre de la política que se usa en Program.cs (app.UseCors).</summary>
    public const string Politica = "FrontendVue";

    private static readonly string[] OrigenesDesarrollo =
    [
        "https://localhost:5173",
        "http://localhost:5173"
    ];

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var origenes = configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>();
        if (origenes is null || origenes.Length == 0)
            origenes = OrigenesDesarrollo;

        services.AddCors(options =>
        {
            options.AddPolicy(Politica, politica => politica
                .WithOrigins(origenes.Select(o => o.TrimEnd('/')).ToArray())
                .AllowAnyHeader()     // Authorization, Content-Type, X-Api-Key…
                .AllowAnyMethod()     // GET, POST, PUT, PATCH, DELETE
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));   // el navegador cachea el OPTIONS
        });

        return services;
    }
}
