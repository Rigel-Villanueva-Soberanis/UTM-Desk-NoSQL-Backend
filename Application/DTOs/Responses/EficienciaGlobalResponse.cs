namespace Application.DTOs.Responses;

/// <summary>KPIs generales del departamento de TI.</summary>
public class EficienciaGlobalResponse
{
    /// <summary>Incidencias que entran en los filtros.</summary>
    public int TotalIncidencias { get; set; }

    /// <summary>Suma de horas_invertidas de todas las incidencias (horas hombre).</summary>
    public double HorasHombreTotales { get; set; }

    /// <summary>Incidencias con al menos una hora registrada.</summary>
    public int IncidenciasConHoras { get; set; }

    /// <summary>Horas promedio en las incidencias que sí tienen horas registradas.</summary>
    public double PromedioHorasPorIncidencia { get; set; }

    /// <summary>Las 10 etiquetas más frecuentes (problemáticas más comunes).</summary>
    public List<TagFrecuenciaResponse> TopTags { get; set; } = new();

    public ProveedoresExternosResponse ProveedoresExternos { get; set; } = new();
}

public class TagFrecuenciaResponse
{
    public string Tag { get; set; } = string.Empty;
    public int Total { get; set; }
}

/// <summary>Impacto de la subcontratación: casos derivados a proveedores externos.</summary>
public class ProveedoresExternosResponse
{
    public int TotalCasos { get; set; }

    /// <summary>Porcentaje del total de incidencias que se derivaron (0 a 100).</summary>
    public double Porcentaje { get; set; }

    /// <summary>Casos por empresa, de más a menos.</summary>
    public List<CasosEmpresaResponse> PorEmpresa { get; set; } = new();
}

public class CasosEmpresaResponse
{
    public string Empresa { get; set; } = string.Empty;
    public int Total { get; set; }
}
