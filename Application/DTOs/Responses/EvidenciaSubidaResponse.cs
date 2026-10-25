namespace Application.DTOs.Responses;

/// <summary>
/// Respuesta del upload. Para ligar el archivo a una incidencia se mandan
/// PublicId, Formato y NombreArchivo a PATCH /api/incidencias/{id}/evidencias.
/// </summary>
public class EvidenciaSubidaResponse
{
    public string PublicId { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string Formato { get; set; } = string.Empty;
    public long Bytes { get; set; }

    /// <summary>URL del archivo en Cloudinary.</summary>
    public string Url { get; set; } = string.Empty;
}
