namespace Application.DTOs.Responses;

/// <summary>Tiempos de resolución de un edificio (solo incidencias resueltas).</summary>
public class TiempoEdificioResponse
{
    public string Edificio { get; set; } = string.Empty;

    /// <summary>Reportes resueltos en el edificio.</summary>
    public int TotalReportes { get; set; }

    /// <summary>Tiempo promedio entre la creación y la resolución, en horas.</summary>
    public double PromedioHoras { get; set; }

    /// <summary>El caso que se resolvió más rápido, en horas.</summary>
    public double MinimoHoras { get; set; }

    /// <summary>El caso que más tardó, en horas.</summary>
    public double MaximoHoras { get; set; }
}
