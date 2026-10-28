namespace Application.Interfaces;

/// <summary>
/// Reportes estadísticos sobre la colección de incidencias.
/// Todo se calcula con pipelines de agregación en MongoDB: al backend solo llegan los totales.
/// </summary>
public interface IAnaliticaRepository
{
    /// <summary>Incidencias resueltas agrupadas por edificio: total y tiempo de resolución en horas.</summary>
    Task<List<TiempoEdificio>> TiemposPorEdificioAsync(TiemposEdificioConsulta consulta);

    /// <summary>KPIs del departamento: horas hombre, top 10 de etiquetas y casos con proveedor externo.</summary>
    Task<EficienciaGlobal> EficienciaGlobalAsync(EficienciaGlobalConsulta consulta);
}

/// <summary>Filtros ya validados. Desde/Hasta en UTC, Hasta exclusivo. Aplican sobre fechas.resolucion.</summary>
public record TiemposEdificioConsulta(DateTime? Desde, DateTime? Hasta, string? Categoria, string? Prioridad, bool OrdenarPorTiempo);

/// <summary>Filtros ya validados. Desde/Hasta en UTC, Hasta exclusivo. Aplican sobre fechas.creacion. Estados null = todos.</summary>
public record EficienciaGlobalConsulta(DateTime? Desde, DateTime? Hasta, string? Categoria, string? Edificio, IReadOnlyList<string>? Estados);

public record TiempoEdificio(string Edificio, int TotalReportes, double PromedioHoras, double MinimoHoras, double MaximoHoras, double HorasTrabajadas);

public record EficienciaGlobal(
    int TotalIncidencias,
    double HorasHombreTotales,
    int IncidenciasConHoras,
    IReadOnlyList<(string Tag, int Total)> TopTags,
    int CasosExternos,
    IReadOnlyList<(string Empresa, int Total)> CasosPorEmpresa);
