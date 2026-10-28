using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>
/// Todos los mensajes de error de evidencias.
///   • Métodos que devuelven <see cref="DomainException"/>: se lanzan con throw (respuesta completa).
///   • Métodos que devuelven <see cref="ErrorDetalle"/>: un error de un campo, se juntan en una lista.
/// </summary>
public static class EvidenciaErrors
{
    // ─── Errores generales (throw) ───────────────────────────────────────────

    public static DomainException DatosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los datos de la evidencia no son válidos. Revisa los errores.", errores);

    public static DomainException ArchivosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los archivos no son válidos. Revisa los errores.", errores);

    public static DomainException SinPermiso() =>
        new(ApiErrors.Forbidden, "No tienes permiso para agregar evidencias a esta incidencia.");

    public static DomainException EstadoNoPermite(string estado) =>
        new(ApiErrors.Conflict, $"No se pueden agregar evidencias a una incidencia en estado '{estado}'.");

    public static DomainException SinArchivos() =>
        new(ApiErrors.ValidationFailed, "Debes adjuntar al menos un archivo.",
            [new ErrorDetalle("archivos", "Debes adjuntar al menos un archivo.")]);

    public static DomainException DemasiadosArchivos(int maximo) =>
        new(ApiErrors.ValidationFailed, $"Puedes subir máximo {maximo} archivos por petición.",
            [new ErrorDetalle("archivos", $"Puedes subir máximo {maximo} archivos por petición.")]);

    public static DomainException LimitePorRol(string rol, int maximo, int actuales) =>
        new(ApiErrors.Conflict, $"Como {rol} puedes tener máximo {maximo} evidencias en esta incidencia y ya hay {actuales}.",
            [new ErrorDetalle("evidencias", $"Solo puedes agregar {Math.Max(0, maximo - actuales)} evidencia(s) más.")]);

    public static DomainException AlmacenamientoNoDisponible() =>
        new(ApiErrors.ServiceUnavailable, "No se pudo verificar el archivo en el almacenamiento. Intenta de nuevo en unos minutos.");

    // ─── Errores por archivo del upload (campo "archivos[i]") ────────────────

    public static ErrorDetalle ArchivoVacio(string campo) =>
        new(campo, "El archivo está vacío.");

    public static ErrorDetalle ArchivoMuyGrande(string campo, long maximoMb) =>
        new(campo, $"El archivo no puede pesar más de {maximoMb} MB.");

    public static ErrorDetalle TipoArchivoNoPermitido(string campo, IEnumerable<string> extensionesValidas) =>
        new(campo, $"Tipo de archivo no permitido. Formatos válidos: {string.Join(", ", extensionesValidas)}.");

    public static ErrorDetalle ContenidoNoCoincide(string campo) =>
        new(campo, "El contenido del archivo no coincide con su extensión.");

    // ─── Errores por evidencia ligada (campo "evidencias[i].xxx") ────────────

    public static ErrorDetalle DemasiadasEvidencias(string campo, int maximo) =>
        new(campo, $"Puedes enviar máximo {maximo} evidencias por petición.");

    public static ErrorDetalle ArchivoAjeno(string campo) =>
        new(campo, "El archivo no pertenece a tus cargas. Súbelo primero con POST /api/evidencias/upload.");

    public static ErrorDetalle FormatoNoPermitido(string campo, IEnumerable<string> formatosValidos) =>
        new(campo, $"Formato no permitido. Formatos válidos: {string.Join(", ", formatosValidos)}.");

    public static ErrorDetalle ArchivoNoExiste(string campo) =>
        new(campo, "El archivo no existe en el almacenamiento. Súbelo primero con POST /api/evidencias/upload.");

    public static ErrorDetalle EvidenciaRepetida(string campo) =>
        new(campo, "Este archivo está repetido en la petición.");

    public static ErrorDetalle EvidenciaYaLigada(string campo) =>
        new(campo, "Este archivo ya está ligado a la incidencia.");
}
