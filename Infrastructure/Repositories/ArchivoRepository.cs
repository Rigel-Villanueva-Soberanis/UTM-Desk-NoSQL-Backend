using Application.Interfaces;
using Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Infrastructure.Repositories;

/// <summary>
/// Almacenamiento de archivos en Cloudinary (RF-17), por su API REST.
/// Los archivos se suben con acceso restringido ("authenticated"): la URL guardada NO sirve para verlos;
/// se ven con un enlace de descarga firmado que vence a los pocos minutos (<see cref="VigenciaEnlace"/>).
/// Los archivos antiguos subidos como públicos ("upload") se siguen reconociendo.
/// Las imágenes se guardan optimizadas: WebP, calidad automática y máximo 1920 px por lado.
/// </summary>
public class ArchivoRepository : IArchivoRepository
{
    private const string TipoEntrega       = "authenticated";
    private const string TipoEntregaPublica = "upload";   // archivos subidos antes del acceso restringido

    /// <summary>Minutos que dura un enlace temporal.</summary>
    public static readonly TimeSpan VigenciaEnlace = TimeSpan.FromMinutes(15);

    /// <summary>Imágenes y PDF se manejan en Cloudinary como recurso "image".</summary>
    private const string TipoRecurso = "image";

    /// <summary>Formato en el que se guardan las imágenes (más ligero que JPG/PNG con buena calidad).</summary>
    private const string FormatoImagen = "webp";

    /// <summary>Lado máximo en píxeles; las fotos más grandes se reducen sin deformarse.</summary>
    private const int LadoMaximo = 1920;

    /// <summary>Reduce solo si es más grande (c_limit) y comprime con buena calidad visual (q_auto:good).</summary>
    private static readonly string Transformacion = $"c_limit,h_{LadoMaximo},q_auto:good,w_{LadoMaximo}";

    private readonly CloudinaryContext _cloudinary;

    public ArchivoRepository(CloudinaryContext cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<ArchivoSubido> SubirAsync(Stream contenido, string nombreArchivo, string carpeta, bool optimizarImagen)
    {
        var parametros = new Dictionary<string, string>
        {
            ["folder"] = carpeta,
            ["type"]   = TipoEntrega
        };

        if (optimizarImagen)
        {
            // Cloudinary convierte y comprime ANTES de guardar: en la nube solo queda la versión ligera
            parametros["format"]         = FormatoImagen;
            parametros["transformation"] = Transformacion;
        }

        using var formulario = new MultipartFormDataContent();
        foreach (var (nombre, valor) in _cloudinary.Firmar(parametros))
            formulario.Add(new StringContent(valor), nombre);

        var archivo = new StreamContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formulario.Add(archivo, "file", NombreSeguro(nombreArchivo));

        using var respuesta = await _cloudinary.Http.PostAsync(_cloudinary.UrlAccion(TipoRecurso, "upload"), formulario);
        var json = await LeerJsonAsync(respuesta);

        var error = MensajeDeError(json);
        if (error is not null)
            throw new InvalidOperationException($"Cloudinary rechazó la subida: {error}");
        if (json is null)
            throw new InvalidOperationException($"Cloudinary rechazó la subida: respuesta inesperada ({(int)respuesta.StatusCode}).");

        var raiz     = json.Value;
        var publicId = raiz.GetProperty("public_id").GetString()!;
        var formato  = raiz.TryGetProperty("format", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString()! : string.Empty;
        var bytes    = raiz.TryGetProperty("bytes", out var b) && b.TryGetInt64(out var n) ? n : 0;
        var url      = raiz.TryGetProperty("secure_url", out var u) && u.ValueKind == JsonValueKind.String
            ? u.GetString()!
            : ConstruirUrl(publicId, formato);

        return new ArchivoSubido(publicId, url, formato, bytes);
    }

    /// <summary>
    /// Borra el archivo (acción destroy) e invalida la copia en caché del CDN.
    /// "ok" = borrado · "not found" = ya no existía (también cuenta como borrado).
    /// </summary>
    public async Task<ArchivoEliminado> EliminarAsync(string publicId)
    {
        try
        {
            // Primero como archivo restringido; si no está ahí, como archivo público antiguo
            foreach (var tipo in new[] { TipoEntrega, TipoEntregaPublica })
            {
                var parametros = new Dictionary<string, string>
                {
                    ["public_id"]  = publicId,
                    ["type"]       = tipo,
                    ["invalidate"] = "true"
                };

                using var formulario = new MultipartFormDataContent();
                foreach (var (nombre, valor) in _cloudinary.Firmar(parametros))
                    formulario.Add(new StringContent(valor), nombre);

                using var respuesta = await _cloudinary.Http.PostAsync(_cloudinary.UrlAccion(TipoRecurso, "destroy"), formulario);
                var json = await LeerJsonAsync(respuesta);

                var error = MensajeDeError(json);
                if (error is not null)
                    return new ArchivoEliminado(publicId, false, error);

                var resultado = json is not null && json.Value.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.String
                    ? r.GetString()
                    : null;

                if (resultado == "ok")
                    return new ArchivoEliminado(publicId, true, null);
                if (resultado != "not found")
                    return new ArchivoEliminado(publicId, false, $"Respuesta inesperada de Cloudinary: '{resultado}'.");
            }

            // No estaba en ninguno de los dos tipos: ya no existía, también cuenta como borrado
            return new ArchivoEliminado(publicId, true, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ArchivoEliminado(publicId, false, "No se pudo conectar con Cloudinary.");
        }
    }

    /// <summary>
    /// Admin API: GET resources/image/{tipo}/{public_id}. Busca primero como archivo restringido y luego como
    /// público antiguo. 404 en ambos = no existe. Cualquier otra falla = no disponible.
    /// </summary>
    public async Task<ConsultaArchivo> ConsultarAsync(string publicId)
    {
        try
        {
            var ruta = string.Join("/", publicId.Split('/').Select(Uri.EscapeDataString));

            foreach (var tipo in new[] { TipoEntrega, TipoEntregaPublica })
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Get,
                    _cloudinary.UrlAccion("resources/" + TipoRecurso, $"{tipo}/{ruta}"));
                peticion.Headers.TryAddWithoutValidation("Authorization", _cloudinary.AutorizacionBasica());

                using var respuesta = await _cloudinary.Http.SendAsync(peticion);

                if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                    continue;

                var json = await LeerJsonAsync(respuesta);
                if (!respuesta.IsSuccessStatusCode || json is null || MensajeDeError(json) is not null)
                    return new ConsultaArchivo(EstadoConsulta.NoDisponible, 0);

                return json.Value.TryGetProperty("bytes", out var b) && b.TryGetInt64(out var bytes)
                    ? new ConsultaArchivo(EstadoConsulta.Encontrado, bytes)
                    : new ConsultaArchivo(EstadoConsulta.NoDisponible, 0);
            }

            return new ConsultaArchivo(EstadoConsulta.NoExiste, 0);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ConsultaArchivo(EstadoConsulta.NoDisponible, 0);
        }
    }

    public string ConstruirUrl(string publicId, string formato)
        => $"https://res.cloudinary.com/{_cloudinary.CloudName}/{TipoRecurso}/{TipoEntrega}/{publicId}.{formato}";

    public EnlaceTemporal GenerarEnlaceTemporal(string publicId, string? formato, string? urlGuardada)
    {
        var vence = DateTime.UtcNow.Add(VigenciaEnlace);

        var parametros = new Dictionary<string, string>
        {
            ["public_id"]  = publicId,
            ["type"]       = TipoEntregaDe(urlGuardada),
            ["expires_at"] = new DateTimeOffset(vence).ToUnixTimeSeconds().ToString()
        };
        if (!string.IsNullOrWhiteSpace(formato))
            parametros["format"] = formato;

        return new EnlaceTemporal(_cloudinary.UrlDescargaFirmada(TipoRecurso, parametros), vence);
    }

    /// <summary>Tipo de entrega del archivo según su URL guardada: los antiguos dicen "/upload/", los nuevos "/authenticated/".</summary>
    private static string TipoEntregaDe(string? urlGuardada)
        => urlGuardada is not null && urlGuardada.Contains($"/{TipoEntregaPublica}/", StringComparison.Ordinal)
            ? TipoEntregaPublica
            : TipoEntrega;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Lee el cuerpo como JSON; devuelve null si no es JSON (ej. una página de error).</summary>
    private static async Task<JsonElement?> LeerJsonAsync(HttpResponseMessage respuesta)
    {
        var texto = await respuesta.Content.ReadAsStringAsync();
        try
        {
            return JsonDocument.Parse(texto).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Mensaje de { "error": { "message": "..." } }, o null si la respuesta no trae error.</summary>
    private static string? MensajeDeError(JsonElement? json)
    {
        if (json is null || json.Value.ValueKind != JsonValueKind.Object) return null;
        if (!json.Value.TryGetProperty("error", out var error)) return null;

        return error.ValueKind == JsonValueKind.Object
               && error.TryGetProperty("message", out var mensaje)
               && mensaje.ValueKind == JsonValueKind.String
            ? mensaje.GetString()
            : "error desconocido";
    }

    /// <summary>Nombre del archivo apto para el encabezado HTTP (solo letras, números, punto, guion y guion bajo).</summary>
    private static string NombreSeguro(string nombreArchivo)
    {
        var nombre = Path.GetFileName(nombreArchivo);
        var limpio = new string(nombre.Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());
        return string.IsNullOrWhiteSpace(limpio) ? "archivo" : limpio;
    }
}
