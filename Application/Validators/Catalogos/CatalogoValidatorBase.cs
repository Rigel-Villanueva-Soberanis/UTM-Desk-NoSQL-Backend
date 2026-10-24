using System.Text.RegularExpressions;
using Application.Common;
using Application.Common.ErrorCatalog;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Validators.Catalogos;

/// <summary>
/// Base para los validadores de catálogos. Acumula todos los errores encontrados
/// (cada uno con su campo y su mensaje) para devolverlos juntos al frontend,
/// y ofrece helpers para leer campos con su tipo.
/// </summary>
public abstract class CatalogoValidatorBase : ICatalogoValidator
{
    /// <summary>Formato final de las claves internas: minúsculas, números y guion bajo. Ej: "lab_c3".</summary>
    protected static readonly Regex PatronClave = new(@"^[a-z0-9]+(_[a-z0-9]+)*$", RegexOptions.Compiled);

    private static readonly char[] OperadoresNoSql = ['$', '{', '}'];

    public abstract string ClaveCatalogo { get; }
    public abstract IReadOnlyCollection<string> RolesEditores { get; }

    /// <summary>Por defecto cada elemento se identifica por su "clave". Sobrescribir si es otro campo.</summary>
    public virtual string CampoIdentificador => "clave";

    /// <summary>Por defecto se permite agregar elementos con POST. Sobrescribir para bloquearlo.</summary>
    public virtual bool PermiteAgregarElementos => true;

    public BsonValue Validar(BsonValue contenidoNuevo, BsonValue contenidoActual)
    {
        var errores   = new List<ErrorDetalle>();
        var resultado = ValidarContenido(contenidoNuevo, contenidoActual, errores);

        if (errores.Count > 0)
            throw CatalogoErrors.ContenidoInvalido(ClaveCatalogo, errores);

        return resultado;
    }

    /// <summary>Reglas propias de cada catálogo. Agrega a <paramref name="errores"/> lo que no cumpla.</summary>
    protected abstract BsonValue ValidarContenido(BsonValue contenidoNuevo, BsonValue contenidoActual, List<ErrorDetalle> errores);

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Registra un error en un campo. Ej: Error(errores, "contenido[2].salon", "No puede estar vacío.").</summary>
    protected static void Error(List<ErrorDetalle> errores, string campo, string mensaje)
        => errores.Add(new ErrorDetalle(campo, mensaje));

    protected static BsonArray? LeerArregloRaiz(BsonValue valor, List<ErrorDetalle> errores)
    {
        if (!valor.IsBsonArray)
        {
            Error(errores, "contenido", "Debe ser un arreglo (array).");
            return null;
        }
        if (valor.AsBsonArray.Count == 0)
        {
            Error(errores, "contenido", "Debe tener al menos un elemento.");
            return null;
        }
        return valor.AsBsonArray;
    }

    protected static BsonDocument? LeerObjeto(BsonValue valor, string ruta, List<ErrorDetalle> errores)
    {
        if (valor.IsBsonDocument) return valor.AsBsonDocument;
        Error(errores, ruta, "Debe ser un objeto.");
        return null;
    }

    /// <summary>Rechaza campos que no están definidos para el catálogo (evita basura u operadores $).</summary>
    protected static void ValidarCamposPermitidos(BsonDocument doc, string ruta, List<ErrorDetalle> errores, params string[] permitidos)
    {
        foreach (var nombre in doc.Names)
        {
            if (!permitidos.Contains(nombre))
                Error(errores, $"{ruta}.{nombre}", $"Campo no permitido. Campos válidos: {string.Join(", ", permitidos)}.");
        }
    }

    /// <summary>Lee un texto obligatorio: sin espacios sobrantes, no vacío, sin operadores NoSQL y con longitud máxima.</summary>
    protected static string? LeerTexto(BsonDocument doc, string campo, string ruta, List<ErrorDetalle> errores, int maxLongitud = 100)
    {
        if (!doc.Contains(campo))
        {
            Error(errores, $"{ruta}.{campo}", "Es obligatorio.");
            return null;
        }
        return ValidarTexto(doc[campo], $"{ruta}.{campo}", errores, maxLongitud);
    }

    /// <summary>
    /// Lee una clave interna y la normaliza con <see cref="TextoNormalizer.NormalizarClave"/>
    /// ("Lab C3" → "lab_c3"). Solo da error si después de normalizar sigue sin ser válida (ej. "lab#3").
    /// </summary>
    protected static string? LeerClave(BsonDocument doc, string campo, string ruta, List<ErrorDetalle> errores, int maxLongitud = 50)
    {
        var texto = LeerTexto(doc, campo, ruta, errores, maxLongitud);
        if (texto is null) return null;

        var clave = TextoNormalizer.NormalizarClave(texto);
        if (!PatronClave.IsMatch(clave))
        {
            Error(errores, $"{ruta}.{campo}", $"'{texto}' no es válida. Usa solo letras, números, espacios o guion bajo (ej. 'lab_c3').");
            return null;
        }
        return clave;
    }

    protected static bool? LeerBooleano(BsonDocument doc, string campo, string ruta, List<ErrorDetalle> errores)
    {
        if (!doc.Contains(campo))
        {
            Error(errores, $"{ruta}.{campo}", "Es obligatorio.");
            return null;
        }
        if (!doc[campo].IsBoolean)
        {
            Error(errores, $"{ruta}.{campo}", "Debe ser true o false.");
            return null;
        }
        return doc[campo].AsBoolean;
    }

    /// <summary>Lee un arreglo de textos sin repetidos (ignorando mayúsculas y acentos).</summary>
    protected static List<string>? LeerListaTexto(BsonDocument doc, string campo, string ruta, List<ErrorDetalle> errores,
        bool permitirVacia, int maxLongitud = 100)
    {
        var rutaCampo = $"{ruta}.{campo}";

        if (!doc.Contains(campo))
        {
            Error(errores, rutaCampo, "Es obligatorio.");
            return null;
        }
        if (!doc[campo].IsBsonArray)
        {
            Error(errores, rutaCampo, "Debe ser un arreglo de textos.");
            return null;
        }

        var arreglo = doc[campo].AsBsonArray;
        if (!permitirVacia && arreglo.Count == 0)
        {
            Error(errores, rutaCampo, "Debe tener al menos un elemento.");
            return null;
        }

        var resultado = new List<string>();
        for (var i = 0; i < arreglo.Count; i++)
        {
            var texto = ValidarTexto(arreglo[i], $"{rutaCampo}[{i}]", errores, maxLongitud);
            if (texto is null) continue;

            if (resultado.Any(r => TextoNormalizer.Iguales(r, texto)))
                Error(errores, $"{rutaCampo}[{i}]", $"El valor '{texto}' está repetido.");
            else
                resultado.Add(texto);
        }
        return resultado;
    }

    /// <summary>
    /// Marca un error en cada aparición repetida (ignorando mayúsculas y acentos),
    /// indicando dónde apareció primero. Ej: contenido[3].clave → "La clave 'lab_c3' ya existe en contenido[0].clave."
    /// </summary>
    protected static void ValidarUnicos(IEnumerable<(string Valor, string Campo)> items, string descripcion, List<ErrorDetalle> errores)
    {
        var primeros = new Dictionary<string, string>();
        foreach (var (valor, campo) in items)
        {
            var llave = TextoNormalizer.SinAcentos(valor);
            if (primeros.TryGetValue(llave, out var campoOriginal))
                Error(errores, campo, $"{descripcion} '{valor}' ya existe en {campoOriginal}.");
            else
                primeros[llave] = campo;
        }
    }

    private static string? ValidarTexto(BsonValue valor, string ruta, List<ErrorDetalle> errores, int maxLongitud)
    {
        if (!valor.IsString)
        {
            Error(errores, ruta, "Debe ser texto.");
            return null;
        }

        var texto = valor.AsString.Trim();
        if (texto.Length == 0)
        {
            Error(errores, ruta, "No puede estar vacío.");
            return null;
        }
        if (texto.Length > maxLongitud)
        {
            Error(errores, ruta, $"No puede tener más de {maxLongitud} caracteres.");
            return null;
        }
        if (texto.IndexOfAny(OperadoresNoSql) >= 0)
        {
            Error(errores, ruta, "Contiene caracteres no permitidos ($ { }).");
            return null;
        }
        return texto;
    }
}
