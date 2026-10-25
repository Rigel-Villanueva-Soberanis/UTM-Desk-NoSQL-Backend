using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de la generación de folios de incidencias (FolioService).</summary>
public static class FolioErrors
{
    public static DomainException NoAsignado() =>
        new(ApiErrors.Conflict, "No se pudo asignar un folio a la incidencia. Intenta de nuevo.");
}
