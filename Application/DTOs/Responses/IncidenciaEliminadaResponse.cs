namespace Application.DTOs.Responses;

/// <summary>Resultado de eliminar físicamente una incidencia.</summary>
public class IncidenciaEliminadaResponse
{
    public string Id { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;

    /// <summary>Cuántas evidencias se borraron de Cloudinary antes de borrar el documento.</summary>
    public int ArchivosEliminados { get; set; }
}
