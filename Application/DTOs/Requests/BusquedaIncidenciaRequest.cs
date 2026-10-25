using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Búsqueda full-text ($text) en título y descripción, con los mismos filtros del listado.
/// Ej: GET /api/incidencias/buscar?q=pantalla+azul&amp;estado=abierta
/// </summary>
public class BusquedaIncidenciaRequest : IncidenciaFiltroRequest
{
    /// <summary>Palabras a buscar. Usa "comillas" para una frase exacta y -palabra para excluirla.</summary>
    /// <example>proyector lab</example>
    [Required(ErrorMessage = "El término de búsqueda es obligatorio.")]
    [StringLength(100, ErrorMessage = "El término de búsqueda no puede tener más de 100 caracteres.")]
    public string Q { get; set; } = null!;
}
