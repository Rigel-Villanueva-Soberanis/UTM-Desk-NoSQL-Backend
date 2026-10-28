namespace Application.Common.Utilidades;

/// <summary>
/// Conversión entre el tiempo en horas y minutos (lo que ve y manda el usuario)
/// y las horas decimales que se guardan en la base de datos (ej. 1 h 30 min = 1.5).
/// </summary>
public static class DuracionHoras
{
    /// <summary>Mínimo que se puede registrar en una sesión de trabajo.</summary>
    public const int MinutosMinimosPorSesion = 1;

    /// <summary>Máximo que se puede registrar en una sesión de trabajo (24 horas).</summary>
    public const int MinutosMaximosPorSesion = 24 * 60;

    /// <summary>Horas y minutos → minutos totales. Ej: (1, 30) → 90.</summary>
    public static int AMinutos(int horas, int minutos) => horas * 60 + minutos;

    /// <summary>Horas decimales (como se guardan) → minutos totales, redondeado al minuto más cercano.</summary>
    public static int AMinutos(double horasDecimales)
        => (int)Math.Round(horasDecimales * 60, MidpointRounding.AwayFromZero);

    /// <summary>Minutos totales → horas decimales para guardar. Ej: 90 → 1.5.</summary>
    public static double AHoras(int minutosTotales) => minutosTotales / 60.0;

    /// <summary>Texto legible de una duración. Ej: 90 → "1 h 30 min", 45 → "45 min", 120 → "2 h", 0 → "0 min".</summary>
    public static string Texto(int minutosTotales)
    {
        var horas   = minutosTotales / 60;
        var minutos = minutosTotales % 60;

        if (horas == 0) return $"{minutos} min";
        return minutos == 0 ? $"{horas} h" : $"{horas} h {minutos} min";
    }
}
