using Application.Common.ErrorCatalog;
using Application.Common.ErrorsCatalogos;
using MongoDB.Bson;

namespace Application.Validators.Catalogos;

/// <summary>
/// Catálogo <c>edificios</c>.
/// Estructura: [{ clave, edificio, salones: ["Aula 101", "Aula 102", ...] }]
///   • Los salones son solo nombres (sin clave): se identifican por su nombre dentro del edificio.
///   • "salones" es opcional; sin él (o vacío) el edificio no tiene salones y se reporta el edificio completo.
///   • Clave y nombre del edificio son únicos; el nombre de un salón es único dentro de su edificio
///     (todo sin distinguir mayúsculas ni acentos).
/// Pueden editar: Root y Soporte de TI.
/// </summary>
public class EdificiosValidator : CatalogoValidatorBase
{
    public override string ClaveCatalogo => "edificios";

    public override IReadOnlyCollection<string> RolesEditores { get; } = ["root", "soporte"];

    protected override BsonValue ValidarContenido(BsonValue contenidoNuevo, BsonValue contenidoActual, List<ErrorDetalle> errores)
    {
        var arreglo = LeerArregloRaiz(contenidoNuevo, errores);
        if (arreglo is null) return contenidoNuevo;

        var resultado = new BsonArray();
        var claves    = new List<(string Valor, string Campo)>();
        var nombres   = new List<(string Valor, string Campo)>();

        for (var i = 0; i < arreglo.Count; i++)
        {
            var ruta = $"contenido[{i}]";
            var doc  = LeerObjeto(arreglo[i], ruta, errores);
            if (doc is null) continue;

            ValidarCamposPermitidos(doc, ruta, errores, "clave", "edificio", "salones");

            var clave    = LeerClave(doc, "clave", ruta, errores);
            var edificio = LeerTexto(doc, "edificio", ruta, errores, 100);
            var salones  = LeerSalones(doc, ruta, errores);

            if (clave is null || edificio is null || salones is null) continue;

            claves.Add((clave, $"{ruta}.clave"));
            nombres.Add((edificio, $"{ruta}.edificio"));
            resultado.Add(new BsonDocument
            {
                { "clave", clave },
                { "edificio", edificio },
                { "salones", new BsonArray(salones) }
            });
        }

        ValidarUnicos(claves,  EdificioErrors.DescripcionClave,    errores);
        ValidarUnicos(nombres, EdificioErrors.DescripcionEdificio, errores);
        return resultado;
    }

    /// <summary>Lista de nombres de salón: opcional (si falta queda vacía), sin repetidos y con máximo.</summary>
    private static List<string>? LeerSalones(BsonDocument doc, string ruta, List<ErrorDetalle> errores)
    {
        if (!doc.Contains("salones")) return [];

        var salones = LeerListaTexto(doc, "salones", ruta, errores, permitirVacia: true, maxLongitud: 100);
        if (salones is not null && salones.Count > EdificioErrors.MaxSalones)
        {
            errores.Add(EdificioErrors.MuchosSalones($"{ruta}.salones"));
            return null;
        }
        return salones;
    }
}
