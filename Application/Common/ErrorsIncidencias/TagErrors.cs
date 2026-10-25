using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>
/// Errores de las etiquetas (tags) de una incidencia.
///   • Métodos que devuelven <see cref="DomainException"/>: se lanzan con throw (respuesta completa).
///   • Métodos que devuelven <see cref="ErrorDetalle"/>: un error de un campo, se juntan en una lista.
/// </summary>
public static class TagErrors
{
    // ─── Errores generales (throw) ───────────────────────────────────────────

    public static DomainException DatosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Las etiquetas no son válidas. Revisa los errores.", errores);

    public static DomainException SinCambios() =>
        new(ApiErrors.ValidationFailed, "Envía al menos una etiqueta para agregar o quitar.",
            [new ErrorDetalle("agregar", "Envía al menos una etiqueta en 'agregar' o en 'quitar'.")]);

    public static DomainException LimiteIncidencia(int maximo, int actuales) =>
        new(ApiErrors.Conflict, $"Una incidencia puede tener máximo {maximo} etiquetas y ya tiene {actuales}.",
            [new ErrorDetalle("agregar", $"Solo puedes agregar {Math.Max(0, maximo - actuales)} etiqueta(s) más.")]);

    // ─── Errores por etiqueta (campo "agregar[i]" / "quitar[i]") ─────────────

    public static ErrorDetalle DemasiadasEtiquetas(string campo, int maximo) =>
        new(campo, $"Puedes enviar máximo {maximo} etiquetas por petición.");

    public static ErrorDetalle EtiquetaVacia(string campo) =>
        new(campo, "La etiqueta no puede estar vacía.");

    public static ErrorDetalle LongitudNoValida(string campo, int minimo, int maximo) =>
        new(campo, $"La etiqueta debe tener entre {minimo} y {maximo} caracteres.");

    public static ErrorDetalle CaracteresNoValidos(string campo) =>
        new(campo, "La etiqueta solo puede tener letras, números, espacios, guiones y guiones bajos.");

    public static ErrorDetalle AgregarYQuitar(string campo, string etiqueta) =>
        new(campo, $"La etiqueta '{etiqueta}' no puede estar en 'agregar' y en 'quitar' a la vez.");
}
