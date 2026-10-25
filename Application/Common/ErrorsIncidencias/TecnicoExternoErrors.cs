using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores al canalizar una incidencia con un proveedor externo.</summary>
public static class TecnicoExternoErrors
{
    public static DomainException IncidenciaCerrada(string estado) =>
        new(ApiErrors.Conflict, $"No se puede canalizar con un proveedor externo una incidencia en estado '{estado}'.");

    public static DomainException SinCambios() =>
        new(ApiErrors.Conflict, "La incidencia ya tiene registrado este mismo proveedor externo.");
}
