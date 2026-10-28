using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Data;

/// <summary>
/// Acceso a la API REST de Cloudinary (Singleton) con la configuración de appsettings.json → "CloudinarySettings".
/// Se habla con Cloudinary por HTTP directo (sin el paquete CloudinaryDotNet): así no depende de ninguna DLL
/// externa que Windows pueda bloquear (Smart App Control).
/// Las peticiones se firman con SHA-1: parámetros ordenados por nombre + el secreto de la API.
/// </summary>
public class CloudinaryContext
{
    private const string UrlApiPorDefecto = "https://api.cloudinary.com/v1_1";

    private readonly string _apiSecret;
    private readonly string _urlApi;

    public string CloudName { get; }
    public string ApiKey { get; }
    public HttpClient Http { get; }

    /// <param name="urlApi">Solo para pruebas: dirección base de la API. Por defecto la de Cloudinary.</param>
    /// <param name="http">Solo para pruebas: cliente HTTP a usar.</param>
    public CloudinaryContext(string? cloudName, string? apiKey, string? apiSecret, string? urlApi = null, HttpClient? http = null)
    {
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Falta configurar 'CloudinarySettings' (CloudName, ApiKey, ApiSecret) en appsettings.json.");

        CloudName  = cloudName;
        ApiKey     = apiKey;
        _apiSecret = apiSecret;
        _urlApi    = (urlApi ?? UrlApiPorDefecto).TrimEnd('/');
        Http       = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    /// <summary>Dirección de una acción de la API. Ej: UrlAccion("image", "upload").</summary>
    public string UrlAccion(string tipoRecurso, string accion) => $"{_urlApi}/{CloudName}/{tipoRecurso}/{accion}";

    /// <summary>
    /// Devuelve los parámetros listos para enviar: los mismos + timestamp, api_key y signature.
    /// Pasa solo los parámetros que se firman (nunca el archivo).
    /// </summary>
    public Dictionary<string, string> Firmar(IDictionary<string, string> parametros)
    {
        var firmados = new Dictionary<string, string>(parametros)
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        };

        var resultado = new Dictionary<string, string>(firmados)
        {
            ["api_key"]   = ApiKey,
            ["signature"] = CalcularFirma(firmados, _apiSecret)
        };
        return resultado;
    }

    /// <summary>Encabezado Authorization (Basic api_key:api_secret) que pide la Admin API de Cloudinary.</summary>
    public string AutorizacionBasica()
        => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiKey}:{_apiSecret}"));

    /// <summary>
    /// Enlace de descarga firmado (acción "download" de la API): los parámetros, incluido expires_at, quedan dentro de la firma,
    /// así que quitar la firma o cambiar la hora de vencimiento hace que Cloudinary rechace la petición.
    /// </summary>
    public string UrlDescargaFirmada(string tipoRecurso, IDictionary<string, string> parametros)
    {
        var query = string.Join("&", Firmar(parametros)
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{UrlAccion(tipoRecurso, "download")}?{query}";
    }

    /// <summary>
    /// Firma de Cloudinary: "nombre=valor" de cada parámetro con valor, ordenados por nombre y unidos con "&amp;";
    /// se le pega el secreto al final y se saca SHA-1 en hexadecimal minúsculas.
    /// (api_key, file, cloud_name y resource_type no se firman.)
    /// </summary>
    public static string CalcularFirma(IDictionary<string, string> parametros, string secreto)
    {
        var texto = string.Join("&", parametros
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}"));

        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(texto + secreto))).ToLowerInvariant();
    }
}
