using System.Text.RegularExpressions;
using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Utilidades;
using Application.DTOs.Requests;

namespace Application.Validators.Incidencias;

/// <summary>Etiquetas ya normalizadas (minúsculas, sin acentos), sin repetidos.</summary>
public record TagsValidados(IReadOnlyList<string> Agregar, IReadOnlyList<string> Quitar);

/// <summary>
/// Reglas de las etiquetas (tags) de una incidencia.
/// Los mensajes de error están en <see cref="TagErrors"/>.
/// </summary>
public static partial class TagValidator
{
    public const int LongitudMinima    = 2;
    public const int LongitudMaxima    = 30;
    public const int MaximoPorPeticion = 10;
    public const int MaximoPorIncidencia = 15;

    /// <summary>Palabras de letras/números separadas por un espacio, guion o guion bajo. Ej: "soporte-laptop", "proyector electrico", "switch_desconectado".</summary>
    [GeneratedRegex(@"^[a-z0-9ñ]+([ _\-][a-z0-9ñ]+)*$")]
    private static partial Regex FormatoEtiqueta();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();

    /// <summary>
    /// Convierte una etiqueta a su forma guardada: minúsculas, sin acentos y con un solo espacio entre palabras.
    /// Ej: "  Proyector   Eléctrico " → "proyector electrico".
    /// </summary>
    public static string Normalizar(string? etiqueta)
        => Espacios().Replace(TextoNormalizer.SinAcentos(etiqueta), " ");

    /// <summary>Valida y normaliza ambas listas; lanza <see cref="TagErrors"/> si hay errores.</summary>
    public static TagsValidados Validar(ActualizarTagsRequest request)
    {
        var errores = new List<ErrorDetalle>();

        var agregar = ValidarLista(request.Agregar, "agregar", errores);
        var quitar  = ValidarLista(request.Quitar,  "quitar",  errores);

        if (errores.Count > 0)
            throw TagErrors.DatosInvalidos(errores);

        if (agregar.Count == 0 && quitar.Count == 0)
            throw TagErrors.SinCambios();

        // Una etiqueta no puede ir en las dos listas
        for (var i = 0; i < quitar.Count; i++)
        {
            if (agregar.Contains(quitar[i]))
                errores.Add(TagErrors.AgregarYQuitar($"quitar[{i}]", quitar[i]));
        }

        if (errores.Count > 0)
            throw TagErrors.DatosInvalidos(errores);

        return new TagsValidados(agregar, quitar);
    }

    private static List<string> ValidarLista(List<string>? lista, string campo, List<ErrorDetalle> errores)
    {
        var resultado = new List<string>();
        if (lista is null || lista.Count == 0) return resultado;

        if (lista.Count > MaximoPorPeticion)
        {
            errores.Add(TagErrors.DemasiadasEtiquetas(campo, MaximoPorPeticion));
            return resultado;
        }

        for (var i = 0; i < lista.Count; i++)
        {
            var nombreCampo = $"{campo}[{i}]";
            NoSqlSanitizer.Validar(lista[i], nombreCampo);

            var etiqueta = Normalizar(lista[i]);

            if (etiqueta.Length == 0)
                errores.Add(TagErrors.EtiquetaVacia(nombreCampo));
            else if (etiqueta.Length is < LongitudMinima or > LongitudMaxima)
                errores.Add(TagErrors.LongitudNoValida(nombreCampo, LongitudMinima, LongitudMaxima));
            else if (!FormatoEtiqueta().IsMatch(etiqueta))
                errores.Add(TagErrors.CaracteresNoValidos(nombreCampo));
            else if (!resultado.Contains(etiqueta))
                resultado.Add(etiqueta);   // repetidas en la misma petición se toman una sola vez
        }

        return resultado;
    }
}
