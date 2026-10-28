namespace Application.DTOs.Responses;

/// <summary>Resultado de registrar tiempo: lo que se sumó y el nuevo total, en horas y minutos.</summary>
public class HorasInvertidasResponse
{
    /// <summary>Lo que se sumó en esta sesión.</summary>
    public DuracionResponse TiempoRegistrado { get; set; } = new();

    /// <summary>Total acumulado de la incidencia.</summary>
    public DuracionResponse TiempoTotal { get; set; } = new();

    public SeguimientoResponse Seguimiento { get; set; } = new();
}
