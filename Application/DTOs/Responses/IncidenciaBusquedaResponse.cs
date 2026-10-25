namespace Application.DTOs.Responses;

/// <summary>Fila del resultado de la búsqueda full-text.</summary>
public class IncidenciaBusquedaResponse : IncidenciaResumenResponse
{
    /// <summary>Puntuación de coincidencia (textScore) de MongoDB: más alta = más relevante.</summary>
    public double Relevancia { get; set; }
}
