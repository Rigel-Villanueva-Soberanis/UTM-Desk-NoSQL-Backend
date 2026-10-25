namespace Application.Interfaces;

/// <summary>
/// Contador atómico para generar folios consecutivos (implementado con Redis).
/// </summary>
public interface IFolioRepository
{
    /// <summary>Incrementa atómicamente el contador de <paramref name="clave"/> y devuelve el nuevo valor (INCR).</summary>
    Task<long> IncrementarAsync(string clave);

    /// <summary>
    /// Sube el contador a <paramref name="minimo"/> solo si actualmente es menor (operación atómica).
    /// Se usa para recuperar el contador si Redis perdió sus datos.
    /// </summary>
    Task AsegurarMinimoAsync(string clave, long minimo);
}
