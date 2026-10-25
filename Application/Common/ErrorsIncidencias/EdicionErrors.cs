using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores al editar (corregir la captura) y al eliminar una incidencia.</summary>
public static class EdicionErrors
{
    // ─── PUT /api/incidencias/{id} ───────────────────────────────────────────

    public static DomainException NoEsTuIncidencia() =>
        new(ApiErrors.Forbidden, "Solo puedes editar las incidencias que tú registraste.");

    public static DomainException EstadoNoPermite(string estado) =>
        new(ApiErrors.Forbidden, $"Ya no puedes editar esta incidencia porque está en estado '{estado}'. Solo se puede editar mientras está 'abierta'.");

    // ─── DELETE /api/incidencias/{id} ────────────────────────────────────────

    public static DomainException ArchivosNoEliminados(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ServiceUnavailable,
            "No se pudieron borrar las evidencias en Cloudinary, por eso la incidencia NO se eliminó. Intenta de nuevo más tarde.",
            errores);

    public static ErrorDetalle ArchivoNoEliminado(string publicId, string motivo) =>
        new("evidencias", $"'{publicId}': {motivo}");
}
