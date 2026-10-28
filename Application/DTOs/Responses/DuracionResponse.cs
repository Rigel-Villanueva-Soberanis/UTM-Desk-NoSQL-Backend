using Application.Common.Utilidades;

namespace Application.DTOs.Responses;

/// <summary>
/// Un tiempo en horas y minutos. Ej: 1.5 h guardadas → { horas: 1, minutos: 30, texto: "1 h 30 min" }.
/// <see cref="Horas"/> puede pasar de 24 (es el total de horas, no días).
/// </summary>
public class DuracionResponse
{
    /// <summary>Horas completas.</summary>
    /// <example>1</example>
    public int Horas { get; set; }

    /// <summary>Minutos restantes (0 a 59).</summary>
    /// <example>30</example>
    public int Minutos { get; set; }

    /// <summary>Texto listo para mostrar.</summary>
    /// <example>1 h 30 min</example>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Convierte horas decimales (como están en la base de datos) a horas y minutos.</summary>
    public static DuracionResponse DeHoras(double horasDecimales)
    {
        var total = DuracionHoras.AMinutos(horasDecimales);
        return new DuracionResponse { Horas = total / 60, Minutos = total % 60, Texto = DuracionHoras.Texto(total) };
    }
}
