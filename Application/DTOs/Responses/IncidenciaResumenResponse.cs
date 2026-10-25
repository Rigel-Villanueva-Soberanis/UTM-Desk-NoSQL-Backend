namespace Application.DTOs.Responses;

/// <summary>Fila del listado de incidencias (versión ligera para tablas).</summary>
public class IncidenciaResumenResponse
{
    public string Id { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Edificio { get; set; } = string.Empty;
    public string? Aula { get; set; }
    public string NombreReportante { get; set; } = string.Empty;

    /// <summary>true si ya tiene un técnico asignado.</summary>
    public bool Asignada { get; set; }
    public string? IdTecnico { get; set; }
    public string? NombreTecnico { get; set; }
    public DateTime FechaCreacion { get; set; }
}
