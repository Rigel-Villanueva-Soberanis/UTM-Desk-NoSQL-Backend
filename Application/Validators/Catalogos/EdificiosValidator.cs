using Application.Common.ErrorCatalog;
using MongoDB.Bson;

namespace Application.Validators.Catalogos;

/// <summary>
/// Catálogo <c>edificios</c> (áreas de atención).
/// Estructura: [{ clave, edificio, salon }]
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

        var resultado   = new BsonArray();
        var claves      = new List<(string Valor, string Campo)>();
        var ubicaciones = new List<(string Valor, string Campo)>();

        for (var i = 0; i < arreglo.Count; i++)
        {
            var ruta = $"contenido[{i}]";
            var doc  = LeerObjeto(arreglo[i], ruta, errores);
            if (doc is null) continue;

            ValidarCamposPermitidos(doc, ruta, errores, "clave", "edificio", "salon");

            var clave    = LeerClave(doc, "clave", ruta, errores);
            var edificio = LeerTexto(doc, "edificio", ruta, errores, 100);
            var salon    = LeerTexto(doc, "salon", ruta, errores, 100);

            if (clave is null || edificio is null || salon is null) continue;

            claves.Add((clave, $"{ruta}.clave"));
            ubicaciones.Add(($"{edificio} - {salon}", $"{ruta}.salon"));
            resultado.Add(new BsonDocument
            {
                { "clave", clave },
                { "edificio", edificio },
                { "salon", salon }
            });
        }

        ValidarUnicos(claves, "La clave", errores);
        ValidarUnicos(ubicaciones, "La ubicación", errores);
        return resultado;
    }
}
