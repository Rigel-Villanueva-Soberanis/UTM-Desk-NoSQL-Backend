using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>Errores de los reportes de analítica (estadísticas de incidencias).</summary>
public static class AnaliticaErrors
{
    public static DomainException FiltrosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los filtros del reporte no son válidos. Revisa los errores.", errores);

    public static ErrorDetalle OrdenNoValido(string? valor, IEnumerable<string> opciones) =>
        new("ordenarPor", $"'{valor}' no es válido. Opciones: {string.Join(", ", opciones)}.");
}
