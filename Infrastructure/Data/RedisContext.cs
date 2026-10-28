using StackExchange.Redis;

namespace Infrastructure.Data;

/// <summary>
/// Conexión a Redis (Singleton). La conexión se abre hasta el primer uso,
/// así la API arranca aunque Redis no esté disponible en ese momento.
/// </summary>
public class RedisContext : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _conexion;

    public RedisContext(string? connectionString)
    {
        _conexion = new Lazy<ConnectionMultiplexer>(() =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Falta configurar 'ConnectionStrings:Redis' en appsettings.json (ej. \"localhost:6379\").");

            var opciones = ConfigurationOptions.Parse(NormalizarCadena(connectionString));
            opciones.AbortOnConnectFail = false;   // reintenta en lugar de tumbar la app
            return ConnectionMultiplexer.Connect(opciones);
        }, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Acepta "host:puerto,password=..." (local) y también URLs redis://usuario:clave@host:puerto
    /// o rediss://... (las que entregan Render, Upstash y Railway), que se convierten al formato de StackExchange.Redis.
    /// </summary>
    internal static string NormalizarCadena(string cadena)
    {
        if (!cadena.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) &&
            !cadena.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            return cadena;

        var uri = new Uri(cadena);
        var puerto = uri.Port > 0 ? uri.Port : 6379;
        var resultado = $"{uri.Host}:{puerto}";

        var info = uri.UserInfo;
        if (!string.IsNullOrEmpty(info))
        {
            var partes = info.Split(':', 2);
            var clave = Uri.UnescapeDataString(partes.Length == 2 ? partes[1] : partes[0]);
            if (!string.IsNullOrEmpty(clave)) resultado += $",password={clave}";
            if (partes.Length == 2 && !string.IsNullOrEmpty(partes[0]) && partes[0] != "default")
                resultado += $",user={Uri.UnescapeDataString(partes[0])}";
        }

        if (uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase))
            resultado += ",ssl=true";

        return resultado;
    }

    public IDatabase GetDatabase() => _conexion.Value.GetDatabase();

    public void Dispose()
    {
        if (_conexion.IsValueCreated)
            _conexion.Value.Dispose();
    }
}
