using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Corrección de la captura inicial. Reemplaza título, descripción, ubicación y detalle específico.
/// La categoría, la prioridad, el estado y el folio NO se cambian aquí.
/// </summary>
public class UpdateIncidenciaRequest
{
    /// <summary>Título corto del problema.</summary>
    /// <example>Sin internet en el Laboratorio 3 (edificio C)</example>
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 150 caracteres.")]
    public string Titulo { get; set; } = null!;

    /// <summary>Descripción detallada de la falla.</summary>
    /// <example>Ningún equipo del laboratorio obtiene dirección IP desde la mañana.</example>
    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string Descripcion { get; set; } = null!;

    /// <summary>Clave del lugar en el catálogo edificios.</summary>
    /// <example>lab_c3</example>
    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    public string ClaveUbicacion { get; set; } = null!;

    /// <summary>Campos dinámicos de la categoría ACTUAL de la incidencia (ver campos_dinamicos en el catálogo).</summary>
    public JsonElement DetalleEspecifico { get; set; }
}
