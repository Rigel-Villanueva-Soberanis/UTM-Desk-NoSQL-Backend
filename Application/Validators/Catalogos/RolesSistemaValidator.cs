using Application.Common;
using MongoDB.Bson;

namespace Application.Validators.Catalogos;

/// <summary>
/// Catálogo <c>roles_sistema</c>.
/// Estructura: [{ rol, requiere_clasificacion, clasificaciones_permitidas: [string] }]
/// Pueden editar: Root, Administrativos y Soporte.
/// Los roles son fijos (cada uno tiene sus pantallas): no se pueden agregar, quitar ni renombrar.
/// Lo que sí se puede editar son las clasificaciones (subroles) y si son requeridas.
/// </summary>
public class RolesSistemaValidator : CatalogoValidatorBase
{
    public override string ClaveCatalogo => "roles_sistema";

    public override IReadOnlyCollection<string> RolesEditores { get; } = ["root", "administrativos", "soporte"];

    /// <summary>Cada rol se identifica por su nombre. Ej: PATCH /api/catalogos/roles_sistema/elementos/reportante</summary>
    public override string CampoIdentificador => "rol";

    /// <summary>Los roles son fijos: no se pueden agregar con POST.</summary>
    public override bool PermiteAgregarElementos => false;

    protected override BsonValue ValidarContenido(BsonValue contenidoNuevo, BsonValue contenidoActual, List<ErrorDetalle> errores)
    {
        var arreglo = LeerArregloRaiz(contenidoNuevo, errores);
        if (arreglo is null) return contenidoNuevo;

        var resultado = new BsonArray();
        var roles     = new List<(string Valor, string Campo)>();

        for (var i = 0; i < arreglo.Count; i++)
        {
            var ruta = $"contenido[{i}]";
            var doc  = LeerObjeto(arreglo[i], ruta, errores);
            if (doc is null) continue;

            ValidarCamposPermitidos(doc, ruta, errores, "rol", "requiere_clasificacion", "clasificaciones_permitidas");

            var rol             = LeerTexto(doc, "rol", ruta, errores, 50);
            var requiere        = LeerBooleano(doc, "requiere_clasificacion", ruta, errores);
            var clasificaciones = LeerListaTexto(doc, "clasificaciones_permitidas", ruta, errores, permitirVacia: true, maxLongitud: 50);

            if (requiere == true && clasificaciones is { Count: 0 })
                Error(errores, $"{ruta}.clasificaciones_permitidas", "Si 'requiere_clasificacion' es true, debe tener al menos una clasificación.");

            if (rol is null || requiere is null || clasificaciones is null) continue;

            roles.Add((rol, $"{ruta}.rol"));
            resultado.Add(new BsonDocument
            {
                { "rol", rol },
                { "requiere_clasificacion", requiere.Value },
                { "clasificaciones_permitidas", new BsonArray(clasificaciones.Select(x => (BsonValue)x)) }
            });
        }

        ValidarUnicos(roles, "El rol", errores);
        ValidarRolesFijos(roles, contenidoActual, errores);

        return resultado;
    }

    /// <summary>Los roles enviados deben ser exactamente los mismos que ya existen en la BD.</summary>
    private static void ValidarRolesFijos(List<(string Valor, string Campo)> rolesNuevos, BsonValue contenidoActual, List<ErrorDetalle> errores)
    {
        if (!contenidoActual.IsBsonArray) return;

        var rolesActuales = contenidoActual.AsBsonArray
            .OfType<BsonDocument>()
            .Where(d => d.Contains("rol") && d["rol"].IsString)
            .Select(d => d["rol"].AsString.Trim())
            .ToList();

        // Rol que existía y ya no viene → error general sobre "contenido"
        foreach (var actual in rolesActuales.Where(a => !rolesNuevos.Any(n => TextoNormalizer.Iguales(n.Valor, a))))
            Error(errores, "contenido", $"Falta el rol '{actual}'. Los roles del sistema son fijos: no se pueden eliminar ni renombrar.");

        // Rol nuevo → error en el campo exacto
        foreach (var nuevo in rolesNuevos.Where(n => !rolesActuales.Any(a => TextoNormalizer.Iguales(a, n.Valor))))
            Error(errores, nuevo.Campo, $"El rol '{nuevo.Valor}' no existe. Los roles son fijos; solo se editan sus clasificaciones.");
    }
}
