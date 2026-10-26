using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Nuevo nombre para un salón de un edificio.</summary>
public class ActualizarSalonRequest
{
    /// <summary>Nombre nuevo del salón. No puede repetirse dentro del edificio.</summary>
    /// <example>Aula 104 Planta Alta</example>
    [Required(ErrorMessage = "El nombre del salón es obligatorio.")]
    public string Salon { get; set; } = string.Empty;
}
