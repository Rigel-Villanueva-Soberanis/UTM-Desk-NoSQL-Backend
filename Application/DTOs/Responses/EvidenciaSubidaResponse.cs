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

    /// <summary>Tipo de contenido del archivo guardado. Ej: image/webp.</summary>
    public string TipoMime { get; set; } = string.Empty;

    /// <summary>Peso en kilobytes (2 decimales).</summary>
    public double PesoKb { get; set; }

    /// <summary>Enlace firmado para previsualizar el archivo recién subido. Dura pocos minutos (ver <see cref="VenceEn"/>).</summary>
    public string UrlTemporal { get; set; } = string.Empty;

    /// <summary>Instante (UTC) en que deja de funcionar <see cref="UrlTemporal"/>.</summary>
    public DateTime VenceEn { get; set; }
}
