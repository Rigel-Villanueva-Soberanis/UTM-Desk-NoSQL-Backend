using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de las acciones del técnico: notas de seguimiento y horas invertidas.</summary>
public static class SeguimientoErrors
{
    public static DomainException NoAsignado() =>
        new(ApiErrors.Forbidden, "Solo el técnico asignado a esta incidencia puede realizar esta acción.");

    public static DomainException IncidenciaCerrada(string estado) =>
        new(ApiErrors.Conflict, $"No se pueden registrar cambios en una incidencia en estado '{estado}'.");

    public static DomainException HorasSoloTecnico() =>
        new(ApiErrors.Forbidden, "Solo el técnico asignado (o Root) puede registrar horas de trabajo.",
            [new ErrorDetalle("horas", "Solo el técnico asignado (o Root) puede registrar horas de trabajo.")]);

    public static DomainException TiempoFueraDeRango()
    {
        const string mensaje = "El tiempo registrado debe ser de al menos 1 minuto y como máximo 24 horas.";
        return new(ApiErrors.ValidationFailed, mensaje, [new ErrorDetalle("horas", mensaje)]);
    }
}
