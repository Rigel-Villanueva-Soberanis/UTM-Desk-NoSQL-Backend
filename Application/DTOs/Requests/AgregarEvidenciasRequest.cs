using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Evidencias a ligar a una incidencia existente (PATCH /api/incidencias/{id}/evidencias).</summary>
public class AgregarEvidenciasRequest
{
    /// <summary>De 1 a 3 archivos ya subidos con POST /api/evidencias/upload.</summary>
    [Required(ErrorMessage = "Debes enviar al menos una evidencia.")]
    [MinLength(1, ErrorMessage = "Debes enviar al menos una evidencia.")]
    [MaxLength(3, ErrorMessage = "Puedes enviar máximo 3 evidencias por petición.")]
    public List<EvidenciaReferenciaRequest> Evidencias { get; set; } = [];
}
