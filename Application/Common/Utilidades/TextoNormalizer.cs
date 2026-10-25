using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace Application.Common.Utilidades;

/// <summary>
/// Utilidades para normalizar y comparar textos que llegan del frontend o de la BD.
/// Regla del proyecto: se comparan sin importar mayúsculas, espacios ni acentos.
/// </summary>
public static class TextoNormalizer
{
    private static readonly Regex SeparadoresClave = new(@"[\s\-]+", RegexOptions.Compiled);
    private static readonly Regex GuionesBajosRepetidos = new(@"_+", RegexOptions.Compiled);

    /// <summary>Quita espacios al inicio/fin y convierte a minúsculas. Ej: " Soporte " → "soporte".</summary>
    public static string Normalizar(string? texto)
        => (texto ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Normaliza y además elimina acentos. Ej: "Técnico" → "tecnico".</summary>
    public static string SinAcentos(string? texto)
    {
        var normalizado = Normalizar(texto).Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Convierte un texto en clave interna snake_case:
    /// minúsculas, sin acentos, espacios y guiones → "_".
    /// Ej: "Lab C3" → "lab_c3", "Cómputo-General" → "computo_general".
    /// </summary>
    public static string NormalizarClave(string? texto)
    {
        var clave = SeparadoresClave.Replace(SinAcentos(texto), "_");
        clave = GuionesBajosRepetidos.Replace(clave, "_");
        return clave.Trim('_');
    }

    /// <summary>Compara dos textos ignorando mayúsculas, espacios y acentos.</summary>
    public static bool Iguales(string? a, string? b)
        => SinAcentos(a) == SinAcentos(b);

    /// <summary>
    /// Patrón de búsqueda (regex) que ignora acentos: "jose" → "j[oóö]s[eéë]".
    /// Escapa los caracteres especiales para que el texto se busque literal (sin inyección de regex).
    /// Usar con la opción "i" para ignorar mayúsculas.
    /// </summary>
    public static string PatronSinAcentos(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in SinAcentos(texto))
        {
            sb.Append(c switch
            {
                'a' => "[aáàä]",
                'e' => "[eéèë]",
                'i' => "[iíìï]",
                'o' => "[oóòö]",
                'u' => "[uúùü]",
                'n' => "[nñ]",
                _   => Regex.Escape(c.ToString())
            });
        }
        return sb.ToString();
    }
}
