using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using Application.DTOs.Requests;

namespace Application.Validators.Evidencias;

/// <summary>Evidencia ya validada, lista para guardarse en la incidencia.</summary>
public record EvidenciaValidada(string PublicId, string Formato, string NombreArchivo);

/// <summary>Archivo del upload ya validado.</summary>
public record ArchivoValidado(ArchivoEntrada Archivo, string Nombre, bool EsImagen);

/// <summary>
/// Reglas de validación de evidencias:
///   • los archivos que se suben (cantidad, tamaño, extensión y tipo de contenido);
///   • las referencias que se ligan a una incidencia (que sean del usuario, formato y repetidos).
/// Los mensajes de error están en <see cref="EvidenciaErrors"/>.
/// </summary>
public static class EvidenciaValidator
{
    /// <summary>Tamaño máximo permitido por archivo.</summary>
    public const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>Máximo de archivos por petición (upload o PATCH). Igual al límite por rol más alto.</summary>
    public const int MaxArchivosPorPeticion = 3;

    /// <summary>
    /// Máximo de evidencias que cada rol puede tener en UNA incidencia (cuentan solo las que subió ese rol).
    /// Un rol que no aparece aquí (Root) no tiene límite.
    /// </summary>
    private static readonly Dictionary<string, int> LimitePorRol = new()
    {
        [RolesSistema.Reportante] = 3,
        [RolesSistema.Tecnico]    = 3
    };

    /// <summary>Límite de evidencias por incidencia para el rol, o null si no tiene límite.</summary>
    public static int? LimiteDeRol(string rol)
        => LimitePorRol.TryGetValue(TextoNormalizer.SinAcentos(rol), out var limite) ? limite : null;

    /// <summary>Extensiones permitidas y su tipo de contenido esperado.</summary>
    private static readonly Dictionary<string, string[]> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"]  = ["image/png"],
        [".webp"] = ["image/webp"],
        [".pdf"]  = ["application/pdf"]
    };

    /// <summary>Extensiones que son imagen: se convierten a WebP y se comprimen al subir.</summary>
    private static readonly HashSet<string> ExtensionesImagen = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    /// <summary>Formatos que se aceptan al ligar una evidencia (sin el punto).</summary>
    public static IEnumerable<string> FormatosPermitidos
        => TiposPermitidos.Keys.Select(k => k.TrimStart('.'));

    /// <summary>Carpeta de Cloudinary donde se guardan las evidencias de un usuario.</summary>
    public static string CarpetaDe(string idUsuario) => $"utmdesk/evidencias/{idUsuario}";

    // ─── Upload ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Valida TODOS los archivos antes de subir alguno (si uno está mal, no se sube ninguno).
    /// Los errores salen por archivo: "archivos[0]", "archivos[1]", …
    /// </summary>
    public static List<ArchivoValidado> ValidarArchivos(IReadOnlyList<ArchivoEntrada> archivos)
    {
        if (archivos.Count == 0)
            throw EvidenciaErrors.SinArchivos();

        if (archivos.Count > MaxArchivosPorPeticion)
            throw EvidenciaErrors.DemasiadosArchivos(MaxArchivosPorPeticion);

        var errores    = new List<ErrorDetalle>();
        var resultado  = new List<ArchivoValidado>();

        for (var i = 0; i < archivos.Count; i++)
        {
            var archivo   = archivos[i];
            var campo     = $"archivos[{i}]";
            var nombre    = Path.GetFileName(archivo.Nombre ?? string.Empty).Trim();
            var extension = Path.GetExtension(nombre);

            if (archivo.Tamano <= 0)
                errores.Add(EvidenciaErrors.ArchivoVacio(campo));
            else if (archivo.Tamano > TamanoMaximoBytes)
                errores.Add(EvidenciaErrors.ArchivoMuyGrande(campo, TamanoMaximoBytes / (1024 * 1024)));
            else if (string.IsNullOrWhiteSpace(nombre) || !TiposPermitidos.TryGetValue(extension, out var tiposEsperados))
                errores.Add(EvidenciaErrors.TipoArchivoNoPermitido(campo, TiposPermitidos.Keys));
            else if (!tiposEsperados.Contains(TextoNormalizer.Normalizar(archivo.TipoContenido)))
                errores.Add(EvidenciaErrors.ContenidoNoCoincide(campo));
            else
                resultado.Add(new ArchivoValidado(archivo, nombre, ExtensionesImagen.Contains(extension)));
        }

        if (errores.Count > 0)
            throw EvidenciaErrors.ArchivosInvalidos(errores);

        return resultado;
    }

    // ─── Ligar evidencias a una incidencia ───────────────────────────────────

    /// <summary>
    /// Valida las referencias de archivos ya subidos. Agrega a <paramref name="errores"/> lo que no cumpla
    /// (campo "evidencias[i].xxx") y devuelve las válidas. No lanza excepción: así, al crear la incidencia,
    /// estos errores salen junto con los demás errores de la incidencia.
    /// </summary>
    /// <param name="referencias">Evidencias enviadas (puede ser null o vacía).</param>
    /// <param name="idUsuario">Usuario que liga los archivos: deben estar en SU carpeta.</param>
    /// <param name="errores">Lista donde se acumulan los errores.</param>
    /// <param name="yaLigadas">publicId que la incidencia ya tiene (para no repetirlos).</param>
    public static List<EvidenciaValidada> ValidarReferencias(
        IReadOnlyList<EvidenciaReferenciaRequest>? referencias,
        string idUsuario,
        List<ErrorDetalle> errores,
        IReadOnlyCollection<string>? yaLigadas = null)
    {
        var resultado = new List<EvidenciaValidada>();
        if (referencias is null || referencias.Count == 0)
            return resultado;

        if (referencias.Count > MaxArchivosPorPeticion)
        {
            errores.Add(EvidenciaErrors.DemasiadasEvidencias("evidencias", MaxArchivosPorPeticion));
            return resultado;
        }

        var carpeta = CarpetaDe(idUsuario) + "/";
        var vistos  = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < referencias.Count; i++)
        {
            var referencia = referencias[i];
            var prefijo    = $"evidencias[{i}]";

            NoSqlSanitizer.ValidarTodos(
                (referencia.PublicId,      $"{prefijo}.publicId"),
                (referencia.Formato,       $"{prefijo}.formato"),
                (referencia.NombreArchivo, $"{prefijo}.nombreArchivo")
            );

            var erroresAntes = errores.Count;
            var publicId     = referencia.PublicId.Trim();
            var formato      = TextoNormalizer.Normalizar(referencia.Formato);

            if (!publicId.StartsWith(carpeta, StringComparison.Ordinal))
                errores.Add(EvidenciaErrors.ArchivoAjeno($"{prefijo}.publicId"));
            else if (!vistos.Add(publicId))
                errores.Add(EvidenciaErrors.EvidenciaRepetida($"{prefijo}.publicId"));
            else if (yaLigadas is not null && yaLigadas.Contains(publicId))
                errores.Add(EvidenciaErrors.EvidenciaYaLigada($"{prefijo}.publicId"));

            if (!FormatosPermitidos.Contains(formato))
                errores.Add(EvidenciaErrors.FormatoNoPermitido($"{prefijo}.formato", FormatosPermitidos));

            if (errores.Count == erroresAntes)
                resultado.Add(new EvidenciaValidada(publicId, formato, referencia.NombreArchivo.Trim()));
        }

        return resultado;
    }
}
