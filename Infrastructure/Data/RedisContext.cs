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

            var opciones = ConfigurationOptions.Parse(connectionString);
            opciones.AbortOnConnectFail = false;   // reintenta en lugar de tumbar la app
            return ConnectionMultiplexer.Connect(opciones);
        }, LazyThreadSafetyMode.PublicationOnly);
    }

    public IDatabase GetDatabase() => _conexion.Value.GetDatabase();

    public void Dispose()
    {
        if (_conexion.IsValueCreated)
            _conexion.Value.Dispose();
    }
}
