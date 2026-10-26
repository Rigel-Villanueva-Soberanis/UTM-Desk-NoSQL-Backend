using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Salones nuevos para un edificio. Se agregan a los que ya tiene (no reemplazan la lista).</summary>
public class AgregarSalonesRequest
{
    /// <summary>Nombres de los salones a agregar. No pueden repetirse entre sí ni con los que ya tiene el edificio.</summary>
    /// <example>["Aula 104", "Aula 105"]</example>
    [Required(ErrorMessage = "Los salones son obligatorios.")]
    public List<string> Salones { get; set; } = [];
}
