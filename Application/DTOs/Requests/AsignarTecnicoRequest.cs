using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Técnico a asignar (o reasignar) a una incidencia.</summary>
public class AsignarTecnicoRequest
{
    /// <summary>_id del usuario con rol Técnico.</summary>
    /// <example>6add0d854d289579f80b37ea</example>
    [Required(ErrorMessage = "El id del técnico es obligatorio.")]
    public string IdTecnico { get; set; } = string.Empty;
}
