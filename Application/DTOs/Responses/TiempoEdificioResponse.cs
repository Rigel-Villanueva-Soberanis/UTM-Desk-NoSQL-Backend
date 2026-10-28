namespace Application.DTOs.Responses;

/// <summary>Tiempos de resolución de un edificio (solo incidencias resueltas).</summary>
public class TiempoEdificioResponse
{
    public string Edificio { get; set; } = string.Empty;

    /// <summary>Reportes resueltos en el edificio.</summary>
    public int TotalReportes { get; set; }

    /// <summary>Tiempo promedio entre la creación y la resolución, en horas y minutos.</summary>
    public DuracionResponse TiempoPromedio { get; set; } = new();

    /// <summary>El caso que se resolvió más rápido.</summary>
    public DuracionResponse TiempoMinimo { get; set; } = new();

    /// <summary>El caso que más tardó.</summary>
    public DuracionResponse TiempoMaximo { get; set; } = new();

    /// <summary>Total de horas de trabajo registradas por los técnicos en las incidencias resueltas o cerradas del edificio (suma de horas_invertidas).</summary>
    public DuracionResponse TiempoTrabajado { get; set; } = new();
}
