using Application.Common;
using MongoDB.Bson;

namespace Application.Validators.Catalogos;

/// <summary>
/// Catálogo <c>categorias_incidencia</c>.
/// Estructura: [{ clave, nombre, campos_dinamicos: [{ clave, etiqueta, tipo_input, requerido, opciones? }] }]
/// Pueden editar: Root y Soporte.
/// </summary>
public class CategoriasIncidenciaValidator : CatalogoValidatorBase
{
    /// <summary>Tipos de input que el frontend sabe dibujar. Agrega aquí si el frontend soporta uno nuevo.</summary>
    private static readonly string[] TiposInput = ["text", "number", "select", "textarea", "date"];

    public override string ClaveCatalogo => "categorias_incidencia";

    public override IReadOnlyCollection<string> RolesEditores { get; } = ["root", "soporte"];

    protected override BsonValue ValidarContenido(BsonValue contenidoNuevo, BsonValue contenidoActual, List<ErrorDetalle> errores)
    {
        var arreglo = LeerArregloRaiz(contenidoNuevo, errores);
        if (arreglo is null) return contenidoNuevo;

        var resultado = new BsonArray();
        var claves    = new List<(string Valor, string Campo)>();

        for (var i = 0; i < arreglo.Count; i++)
        {
            var ruta = $"contenido[{i}]";
            var doc  = LeerObjeto(arreglo[i], ruta, errores);
            if (doc is null) continue;

            ValidarCamposPermitidos(doc, ruta, errores, "clave", "nombre", "campos_dinamicos");

            var clave  = LeerClave(doc, "clave", ruta, errores);
            var nombre = LeerTexto(doc, "nombre", ruta, errores, 100);
            var campos = LeerCamposDinamicos(doc, ruta, errores);

            if (clave is null || nombre is null || campos is null) continue;

            claves.Add((clave, $"{ruta}.clave"));
            resultado.Add(new BsonDocument
            {
                { "clave", clave },
                { "nombre", nombre },
                { "campos_dinamicos", campos }
            });
        }

        ValidarUnicos(claves, "La clave de categoría", errores);
        return resultado;
    }

    private static BsonArray? LeerCamposDinamicos(BsonDocument categoria, string rutaCategoria, List<ErrorDetalle> errores)
    {
        var rutaCampos = $"{rutaCategoria}.campos_dinamicos";

        if (!categoria.Contains("campos_dinamicos"))
        {
            Error(errores, rutaCampos, "Es obligatorio (puede ser un arreglo vacío).");
            return null;
        }
        if (!categoria["campos_dinamicos"].IsBsonArray)
        {
            Error(errores, rutaCampos, "Debe ser un arreglo.");
            return null;
        }

        var arreglo   = categoria["campos_dinamicos"].AsBsonArray;
        var resultado = new BsonArray();
        var claves    = new List<(string Valor, string Campo)>();
        var huboError = false;

        for (var j = 0; j < arreglo.Count; j++)
        {
            var ruta = $"{rutaCampos}[{j}]";
            var doc  = LeerObjeto(arreglo[j], ruta, errores);
            if (doc is null) { huboError = true; continue; }

            ValidarCamposPermitidos(doc, ruta, errores, "clave", "etiqueta", "tipo_input", "requerido", "opciones");

            var clave     = LeerClave(doc, "clave", ruta, errores);
            var etiqueta  = LeerTexto(doc, "etiqueta", ruta, errores, 100);
            var tipoTexto = LeerTexto(doc, "tipo_input", ruta, errores, 20);
            var requerido = LeerBooleano(doc, "requerido", ruta, errores);

            var tipo = tipoTexto is null ? null : TextoNormalizer.Normalizar(tipoTexto);
            if (tipo is not null && !TiposInput.Contains(tipo))
            {
                Error(errores, $"{ruta}.tipo_input", $"'{tipoTexto}' no es válido. Tipos permitidos: {string.Join(", ", TiposInput)}.");
                tipo = null;
            }

            // "opciones" solo existe (y es obligatorio) cuando el tipo es select
            List<string>? opciones = null;
            if (tipo == "select")
                opciones = LeerListaTexto(doc, "opciones", ruta, errores, permitirVacia: false);
            else if (tipo is not null && doc.Contains("opciones"))
                Error(errores, $"{ruta}.opciones", "Solo se permite cuando tipo_input es 'select'.");

            if (clave is null || etiqueta is null || tipo is null || requerido is null || (tipo == "select" && opciones is null))
            {
                huboError = true;
                continue;
            }

            claves.Add((clave, $"{ruta}.clave"));
            var campo = new BsonDocument
            {
                { "clave", clave },
                { "etiqueta", etiqueta },
                { "tipo_input", tipo },
                { "requerido", requerido.Value }
            };
            if (opciones is not null)
                campo.Add("opciones", new BsonArray(opciones.Select(x => (BsonValue)x)));

            resultado.Add(campo);
        }

        ValidarUnicos(claves, "La clave de campo", errores);
        return huboError ? null : resultado;
    }
}
