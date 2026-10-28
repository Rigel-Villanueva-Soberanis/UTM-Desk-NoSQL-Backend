namespace Application.DTOs.Responses;

/// <summary>
/// Técnico para el selector de asignación. Solo lo necesario: sin correo ni datos de la cuenta.
/// </summary>
public class TecnicoDisponibleResponse
{
    public string Id { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Especialidad (clasificación del rol). Ej: redes, sistemas, mantenimiento.</summary>
    public string? Clasificacion { get; set; }

    /// <summary>Incidencias asignadas que aún no están resueltas, cerradas ni canceladas (carga de trabajo).</summary>
    public int IncidenciasActivas { get; set; }
}
