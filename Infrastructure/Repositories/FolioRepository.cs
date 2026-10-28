using Application.Interfaces;
using Infrastructure.Data;
using StackExchange.Redis;

namespace Infrastructure.Repositories;

/// <summary>
/// Contador de folios con Redis. INCR es atómico: aunque lleguen muchas peticiones
/// al mismo tiempo, nunca se repite un número.
/// </summary>
public class FolioRepository : IFolioRepository
{
    /// <summary>
    /// Script Lua: Redis lo ejecuta completo sin que otra petición se meta en medio.
    /// Si el contador actual es menor que el mínimo, lo sube; si no, lo deja igual.
    /// </summary>
    private const string ScriptAsegurarMinimo = @"
        local actual = tonumber(redis.call('GET', KEYS[1]) or '0')
        local minimo = tonumber(ARGV[1])
        if actual < minimo then
            redis.call('SET', KEYS[1], minimo)
        end
        return 0";

    private readonly RedisContext _redis;

    public FolioRepository(RedisContext redis)
    {
        _redis = redis;
    }

    public async Task<long> IncrementarAsync(string clave)
        => await _redis.GetDatabase().StringIncrementAsync(clave);

    public async Task<long> IncrementarConVigenciaAsync(string clave, TimeSpan vigencia)
    {
        var db    = _redis.GetDatabase();
        var valor = await db.StringIncrementAsync(clave);
        await db.KeyExpireAsync(clave, vigencia);
        return valor;
    }

    public async Task AsegurarMinimoAsync(string clave, long minimo)
        => await _redis.GetDatabase().ScriptEvaluateAsync(
            ScriptAsegurarMinimo,
            new RedisKey[] { clave },
            new RedisValue[] { minimo });
}
