using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.Interfaces;

namespace Application.Validators.Incidencias;

/// <summary>
/// Validación de los filtros de los reportes de analítica.
/// Reutiliza las piezas de <see cref="IncidenciaValidator"/> (prioridad, categoría, fechas).
/// Los mensajes de error están en <see cref="AnaliticaErrors"/> e <see cref="IncidenciaErrors"/>.
/// </summary>
public static class AnaliticaValidator
{
    public const string OrdenReportes = "reportes";
    public const string OrdenTiempo   = "tiempo";
    public static readonly string[] Ordenes = [OrdenReportes, OrdenTiempo];

    public static TiemposEdificioConsulta ValidarTiempos(TiemposEdificioFiltroRequest filtro)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Categoria,  "categoria"),
            (filtro.Prioridad,  "prioridad"),
            (filtro.OrdenarPor, "ordenarPor")
        );

        var errores   = new List<ErrorDetalle>();
        var prioridad = IncidenciaValidator.PrioridadDeFiltro(filtro.Prioridad, errores);
        var (desde, hasta) = IncidenciaValidator.RangoFechas(filtro.FechaDesde, filtro.FechaHasta, errores);

        var orden = string.IsNullOrWhiteSpace(filtro.OrdenarPor)
            ? OrdenReportes
            : Ordenes.FirstOrDefault(o => TextoNormalizer.Iguales(o, filtro.OrdenarPor));
        if (orden is null)
            errores.Add(AnaliticaErrors.OrdenNoValido(filtro.OrdenarPor, Ordenes));

        if (errores.Count > 0)
            throw AnaliticaErrors.FiltrosInvalidos(errores);

        return new TiemposEdificioConsulta(
            desde, hasta, IncidenciaValidator.CategoriaDeFiltro(filtro.Categoria), prioridad,
            OrdenarPorTiempo: orden == OrdenTiempo);
    }

    public static EficienciaGlobalConsulta ValidarEficiencia(EficienciaGlobalFiltroRequest filtro)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Categoria, "categoria"),
            (filtro.Edificio,  "edificio")
        );
        NoSqlSanitizer.ValidarTodos((filtro.Estado ?? []).Select(e => (e, "estado")).ToArray());

        var errores = new List<ErrorDetalle>();
        var (desde, hasta) = IncidenciaValidator.RangoFechas(filtro.FechaDesde, filtro.FechaHasta, errores);
        var estados = IncidenciaValidator.EstadosDeFiltro(filtro.Estado, errores);

        if (errores.Count > 0)
            throw AnaliticaErrors.FiltrosInvalidos(errores);

        return new EficienciaGlobalConsulta(
            desde, hasta, IncidenciaValidator.CategoriaDeFiltro(filtro.Categoria),
            string.IsNullOrWhiteSpace(filtro.Edificio) ? null : filtro.Edificio.Trim(),
            estados);
    }
}
