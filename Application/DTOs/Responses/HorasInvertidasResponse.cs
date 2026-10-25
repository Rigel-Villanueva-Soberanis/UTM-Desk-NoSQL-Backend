namespace Application.DTOs.Responses;

/// <summary>Resultado de registrar horas: lo que se sumó y el nuevo total.</summary>
public class HorasInvertidasResponse
{
    public double HorasRegistradas { get; set; }
    public double HorasTotales { get; set; }
    public SeguimientoResponse Seguimiento { get; set; } = new();
}
