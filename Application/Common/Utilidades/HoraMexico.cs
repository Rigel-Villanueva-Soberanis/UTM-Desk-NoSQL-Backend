namespace Application.Common.Utilidades;

/// <summary>
/// Conversión de fechas de la hora de México (America/Mexico_City) a UTC.
/// En MongoDB todo se guarda en UTC; los filtros de fecha que manda el frontend son días de México.
/// </summary>
public static class HoraMexico
{
    private static readonly TimeZoneInfo? Zona = BuscarZona();

    /// <summary>Instante UTC en que inicia ese día en México (00:00 hora local).</summary>
    public static DateTime InicioDelDiaUtc(DateOnly dia)
    {
        var local = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return Zona is null
            ? DateTime.SpecifyKind(local, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(local, Zona);
    }

    /// <summary>Año según la hora de México (para que el 31 de diciembre en la noche no salte de año).</summary>
    public static int Anio(DateTime fechaUtc)
        => Zona is null ? fechaUtc.Year : TimeZoneInfo.ConvertTimeFromUtc(fechaUtc, Zona).Year;

    /// <summary>Día según la hora de México, como AAAA-MM-DD (para llaves y reportes por día).</summary>
    public static string Dia(DateTime fechaUtc)
    {
        var local = Zona is null ? fechaUtc : TimeZoneInfo.ConvertTimeFromUtc(fechaUtc, Zona);
        return local.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo? BuscarZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }
}
