using Application.Common.Utilidades;
using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Interfaces;
using Domain;

namespace Application.Services;

/// <summary>
/// Generación de folios de incidencias (INC-AAAA-XXXX) con el contador atómico de Redis (INCR).
/// IncidenciaService solo lo consulta: <c>await _folioService.GuardarConFolioAsync(incidencia)</c>.
///
/// Además lleva un contador de estadísticas por día y categoría (contador:incidencias:AAAA-MM-DD:categoria).
///
/// El número SIEMPRE lo da Redis. Mongo solo se consulta como respaldo:
///   • si Redis responde 1 (año nuevo o Redis perdió sus datos), para continuar desde el último folio guardado;
///   • si Mongo rechaza un folio repetido, para resincronizar el contador y reintentar.
/// </summary>
public class FolioService
{
    private const int MaxIntentos = 3;

    /// <summary>Los contadores por día y categoría se borran solos 90 días después de su último uso.</summary>
    private static readonly TimeSpan VigenciaContadorDiario = TimeSpan.FromDays(90);

    private readonly IFolioRepository _folioRepository;
    private readonly IIncidenciaRepository _incidenciaRepository;

    public FolioService(IFolioRepository folioRepository, IIncidenciaRepository incidenciaRepository)
    {
        _folioRepository      = folioRepository;
        _incidenciaRepository = incidenciaRepository;
    }

    /// <summary>
    /// Asigna el folio a la incidencia y la guarda.
    /// Si el folio sale repetido, resincroniza el contador y reintenta (hasta 3 veces).
    /// </summary>
    public async Task<Incidencia> GuardarConFolioAsync(Incidencia incidencia)
    {
        var anio = AnioLocal(incidencia.Fechas.Creacion);

        // Número del día y la categoría (se pide una sola vez, aunque el folio se reintente)
        incidencia.ConsecutivoDia = await ContarPorDiaYCategoriaAsync(incidencia);

        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            incidencia.Folio = await GenerarFolioAsync(anio);
            try
            {
                return await _incidenciaRepository.CreateAsync(incidencia);
            }
            catch (FolioDuplicadoException)
            {
                await SincronizarContadorAsync(anio);
            }
        }
        throw FolioErrors.NoAsignado();
    }

    /// <summary>
    /// Suma 1 al contador del día (hora de México) y la categoría de la incidencia y devuelve el número obtenido
    /// (se guarda en la incidencia como consecutivo_dia).
    /// La categoría es la clave validada contra el catálogo (se limpia otra vez por seguridad), así que
    /// las categorías nuevas del catálogo funcionan sin tocar código. Es solo estadística: si Redis falla
    /// devuelve null y la incidencia se crea igual, sin ese campo.
    /// </summary>
    private async Task<long?> ContarPorDiaYCategoriaAsync(Incidencia incidencia)
    {
        try
        {
            return await _folioRepository.IncrementarConVigenciaAsync(
                ClaveContadorDiario(incidencia.Fechas.Creacion, incidencia.Categoria), VigenciaContadorDiario);
        }
        catch
        {
            return null;   // contador de estadísticas: no debe tumbar el alta de la incidencia
        }
    }

    /// <summary>Ej: contador:incidencias:2026-10-28:computo</summary>
    public static string ClaveContadorDiario(DateTime fechaUtc, string categoria)
    {
        var clave = new string(TextoNormalizer.NormalizarClave(categoria)
            .Where(c => char.IsAsciiLetterOrDigit(c) || c == '_').ToArray());
        return $"contador:incidencias:{HoraMexico.Dia(fechaUtc)}:{(clave.Length == 0 ? "sin_categoria" : clave)}";
    }

    /// <summary>Folio INC-AAAA-XXXX con Redis INCR (una llave por año, así se reinicia cada año).</summary>
    private async Task<string> GenerarFolioAsync(int anio)
    {
        var clave       = ClaveContador(anio);
        var consecutivo = await _folioRepository.IncrementarAsync(clave);

        // 1 = año nuevo o Redis perdió sus datos → continuar desde el último folio guardado (si existe)
        if (consecutivo == 1 && await SincronizarContadorAsync(anio))
            consecutivo = await _folioRepository.IncrementarAsync(clave);

        return $"{PrefijoFolio(anio)}{consecutivo:D4}";
    }

    /// <summary>
    /// Sube el contador de Redis al nivel del último folio guardado en Mongo para ese año.
    /// Devuelve true si encontró folios existentes.
    /// </summary>
    private async Task<bool> SincronizarContadorAsync(int anio)
    {
        var prefijo = PrefijoFolio(anio);
        var ultimo  = await _incidenciaRepository.ObtenerUltimoFolioAsync(prefijo);

        if (ultimo is null || !long.TryParse(ultimo[prefijo.Length..], out var numero))
            return false;

        await _folioRepository.AsegurarMinimoAsync(ClaveContador(anio), numero);
        return true;
    }

    private static string PrefijoFolio(int anio)  => $"INC-{anio}-";
    private static string ClaveContador(int anio) => $"utmdesk:folio:incidencias:{anio}";

    /// <summary>Año según la hora de México (para que el 31 de diciembre en la noche no salte de año).</summary>
    private static int AnioLocal(DateTime fechaUtc) => HoraMexico.Anio(fechaUtc);
}
