using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para registrar una incidencia. El folio, el reportante, el estado y las fechas
/// los pone el backend; el cliente no los manda.
/// </summary>
public class CreateIncidenciaRequest
{
    /// <summary>Título corto del problema.</summary>
    /// <example>Sin internet en el Laboratorio 3</example>
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Descripción detallada de la falla.</summary>
    /// <example>Ningún equipo del laboratorio obtiene dirección IP desde la mañana.</example>
    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Clave de la categoría en el catálogo categorias_incidencia.</summary>
    /// <example>computo</example>
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    /// <summary>
    /// Objeto con los campos dinámicos de la categoría (ver campos_dinamicos en el catálogo).
    /// Ej. para "computo": { "direccion_ip": "192.168.10.1", "conteo_equipos": 25 }
    /// </summary>
    public JsonElement DetalleEspecifico { get; set; }

    /// <summary>Clave del edificio en el catálogo edificios.</summary>
    /// <example>edificio_a</example>
    [Required(ErrorMessage = "El edificio es obligatorio.")]
    public string ClaveEdificio { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del salón del edificio (tal como aparece en el catálogo; no distingue mayúsculas ni acentos).
    /// Obligatorio si el edificio tiene salones; se omite si el edificio no tiene.
    /// </summary>
    /// <example>Aula 102</example>
    public string? Salon { get; set; }

    /// <summary>Prioridad: baja, media, alta o critica. Si no se envía se usa "media".</summary>
    /// <example>alta</example>
    public string? Prioridad { get; set; }
}
