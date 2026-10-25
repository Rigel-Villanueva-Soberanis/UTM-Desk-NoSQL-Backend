using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>
/// Errores generales de incidencias (crear, listar, ver detalle).
///   • Métodos que devuelven <see cref="DomainException"/>: se lanzan con throw (respuesta completa).
///   • Métodos que devuelven <see cref="ErrorDetalle"/>: un error de un campo, se juntan en una lista.
/// </summary>
public static class IncidenciaErrors
{
    // ─── Errores generales (throw) ───────────────────────────────────────────

    public static DomainException NotFound() =>
        new(ApiErrors.NotFound, "La incidencia no fue encontrada.");

    public static DomainException IdInvalido() =>
        new(ApiErrors.ValidationFailed, "El id de la incidencia no tiene un formato válido.");

    public static DomainException SinAcceso() =>
        new(ApiErrors.Forbidden, "No tienes permiso para ver esta incidencia.");

    public static DomainException DatosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los datos de la incidencia no son válidos. Revisa los errores.", errores);

    public static DomainException FiltrosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los filtros de búsqueda no son válidos. Revisa los errores.", errores);

    public static DomainException CambioSimultaneo() =>
        new(ApiErrors.Conflict, "La incidencia fue modificada por otra persona al mismo tiempo. Vuelve a consultarla e intenta de nuevo.");

    public static DomainException CatalogoNoDisponible(string clave) =>
        new(ApiErrors.InternalError, $"No se encontró el catálogo '{clave}' necesario para las incidencias.");

    // ─── Errores por campo (datos de la incidencia y filtros) ────────────────

    public static ErrorDetalle PrioridadNoValida(string? valor, IEnumerable<string> opciones) =>
        new("prioridad", $"'{valor}' no es válida.");

    public static ErrorDetalle EstadoNoValido(string? valor, IEnumerable<string> opciones) =>
        new("estado", $"'{valor}' no es válido.");

    public static ErrorDetalle CategoriaNoExiste(string? valor, IEnumerable<string> opciones) =>
        new("categoria", $"La categoría '{valor}' no existe. Opciones: {string.Join(", ", opciones)}.");

    public static ErrorDetalle UbicacionNoExiste(string? valor) =>
        new("claveUbicacion", $"La ubicación '{valor}' no existe en el catálogo de edificios.");

    public static ErrorDetalle RangoFechasInvalido() =>
        new("fechaDesde", "La fecha inicial no puede ser posterior a la fecha final.");

    public static ErrorDetalle BusquedaMuyCorta(int minimo) =>
        new("q", $"Escribe al menos {minimo} caracteres para buscar.");
}
