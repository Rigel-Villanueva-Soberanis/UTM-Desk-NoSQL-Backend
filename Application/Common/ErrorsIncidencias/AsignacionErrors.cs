using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de PATCH /api/incidencias/{id}/asignar-tecnico.</summary>
public static class AsignacionErrors
{
    public static DomainException IdTecnicoInvalido() =>
        new(ApiErrors.ValidationFailed, "Los datos de la asignación no son válidos. Revisa los errores.",
            [new ErrorDetalle("idTecnico", "El id del técnico no tiene un formato válido.")]);

    public static DomainException TecnicoNoExiste() =>
        new(ApiErrors.NotFound, "El usuario a asignar no existe.",
            [new ErrorDetalle("idTecnico", "No existe un usuario con ese id.")]);

    public static DomainException NoEsTecnico(string rol) =>
        new(ApiErrors.ValidationFailed, "Solo se pueden asignar usuarios con rol Técnico.",
            [new ErrorDetalle("idTecnico", $"El usuario tiene el rol '{rol}', no el de técnico.")]);

    public static DomainException TecnicoInactivo(string estadoCuenta) =>
        new(ApiErrors.ValidationFailed, "No se puede asignar a un técnico con la cuenta inactiva.",
            [new ErrorDetalle("idTecnico", $"La cuenta del técnico está '{estadoCuenta}'.")]);

    public static DomainException MismoTecnico(string nombre) =>
        new(ApiErrors.Conflict, $"El técnico {nombre} ya está asignado a esta incidencia.");

    public static DomainException IncidenciaCerrada(string estado) =>
        new(ApiErrors.Conflict, $"No se puede asignar técnico a una incidencia en estado '{estado}'. Reábrela primero.");
}
