using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de PATCH /api/incidencias/{id}/cambiar-estado.</summary>
public static class EstadoErrors
{
    public static DomainException NoValido(string? valor, IEnumerable<string> opciones) =>
        new(ApiErrors.ValidationFailed, "El estado no es válido. Revisa los errores.",
            [new ErrorDetalle("estado", $"'{valor}' no es válido. Opciones: {string.Join(", ", opciones)}.")]);

    public static DomainException MismoEstado(string estado) =>
        new(ApiErrors.ValidationFailed, $"La incidencia ya está en estado '{estado}'.",
            [new ErrorDetalle("estado", $"La incidencia ya está en estado '{estado}'.")]);

    public static DomainException TransicionNoPermitida(string actual, string nuevo, IEnumerable<string> permitidos) =>
        new(ApiErrors.Conflict, permitidos.Any()
            ? $"No se puede pasar de '{actual}' a '{nuevo}'. Desde '{actual}' solo puedes pasar a: {string.Join(", ", permitidos)}."
            : $"No se puede pasar de '{actual}' a '{nuevo}'. Desde '{actual}' no puedes cambiar el estado.");

    public static DomainException TecnicoSinPermiso(string nuevo) =>
        new(ApiErrors.Forbidden, $"Como técnico solo puedes pasar incidencias a 'en proceso' o 'resuelta' (intentaste '{nuevo}').");
}
