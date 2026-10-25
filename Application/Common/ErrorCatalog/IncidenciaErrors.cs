using Application.Common;

namespace Application.Common.ErrorCatalog;

public static class IncidenciaErrors
{
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

    public static DomainException FolioNoAsignado() =>
        new(ApiErrors.Conflict, "No se pudo asignar un folio a la incidencia. Intenta de nuevo.");

    public static DomainException CatalogoNoDisponible(string clave) =>
        new(ApiErrors.InternalError, $"No se encontró el catálogo '{clave}' necesario para las incidencias.");
}
