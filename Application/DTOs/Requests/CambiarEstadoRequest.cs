using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Nuevo estado de la incidencia, el motivo del cambio (queda en la bitácora) y, opcionalmente,
/// el tiempo trabajado en la intervención (se suma a las horas de la incidencia en la misma operación).
/// </summary>
public class CambiarEstadoRequest
{
    /// <summary>abierta, en proceso, en espera, resuelta, cerrada o cancelada.</summary>
    /// <example>resuelta</example>
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Justificación del cambio.</summary>
    /// <example>Se reemplazó el switch dañado y todos los equipos ya tienen red.</example>
    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    public string Motivo { get; set; } = null!;   // null si no se envía: así solo sale "obligatorio"

    /// <summary>
    /// Horas completas trabajadas en esta intervención (0 a 24). Opcional: si no se manda, no se registra tiempo.
    /// Solo el técnico asignado (o Root) puede registrar horas.
    /// </summary>
    /// <example>1</example>
    [Range(0, 24, ErrorMessage = "Las horas deben estar entre 0 y 24.")]
    public int Horas { get; set; }

    /// <summary>Minutos trabajados en esta intervención (0 a 59). Opcional.</summary>
    /// <example>30</example>
    [Range(0, 59, ErrorMessage = "Los minutos deben estar entre 0 y 59.")]
    public int Minutos { get; set; }
}
