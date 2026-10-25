using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de las acciones del técnico: notas de seguimiento y horas invertidas.</summary>
public static class SeguimientoErrors
{
    public static DomainException NoAsignado() =>
        new(ApiErrors.Forbidden, "Solo el técnico asignado a esta incidencia puede realizar esta acción.");

    public static DomainException IncidenciaCerrada(string estado) =>
        new(ApiErrors.Conflict, $"No se pueden registrar cambios en una incidencia en estado '{estado}'.");
}
