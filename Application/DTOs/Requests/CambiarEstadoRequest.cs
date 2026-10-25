using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Nuevo estado de la incidencia y el motivo del cambio (queda en la bitácora).</summary>
public class CambiarEstadoRequest
{
    /// <summary>abierta, en proceso, por autorizar, resuelta o cancelada.</summary>
    /// <example>resuelta</example>
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Justificación del cambio.</summary>
    /// <example>Se reemplazó el switch dañado y todos los equipos ya tienen red.</example>
    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    public string Motivo { get; set; } = null!;   // null si no se envía: así solo sale "obligatorio"
}
