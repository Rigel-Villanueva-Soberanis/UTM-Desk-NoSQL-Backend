using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Referencia a un archivo ya subido con POST /api/evidencias/upload.
/// Se copian tal cual los datos de la respuesta del upload.
/// Se usa en PATCH /api/incidencias/{id}/evidencias.
/// </summary>
public class EvidenciaReferenciaRequest
{
    /// <summary>Identificador del archivo en Cloudinary (viene en la respuesta del upload).</summary>
    /// <example>utmdesk/evidencias/6add0d854d289579f80b37ea/ab12cd34ef</example>
    [Required(ErrorMessage = "El publicId es obligatorio.")]
    [StringLength(300, ErrorMessage = "El publicId es demasiado largo.")]
    public string PublicId { get; set; } = string.Empty;

    /// <summary>Formato del archivo (viene en la respuesta del upload).</summary>
    /// <example>webp</example>
    [Required(ErrorMessage = "El formato es obligatorio.")]
    public string Formato { get; set; } = string.Empty;

    /// <summary>Nombre original del archivo, para mostrarlo en el expediente.</summary>
    /// <example>rack_falla_led.webp</example>
    [Required(ErrorMessage = "El nombre del archivo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre del archivo no puede tener más de 150 caracteres.")]
    public string NombreArchivo { get; set; } = string.Empty;
}
