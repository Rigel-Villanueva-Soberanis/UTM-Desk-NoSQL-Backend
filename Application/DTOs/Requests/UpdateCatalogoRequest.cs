using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para actualizar un catálogo existente.
/// </summary>
public class UpdateCatalogoRequest
{
    /// <summary>
    /// Nueva clave semántica del catálogo.
    /// </summary>
    public string ClaveCatalogo { get; set; } = string.Empty;

    /// <summary>
    /// Nuevo contenido dinámico. Reemplaza completamente el contenido anterior.
    /// </summary>
    public JsonElement Contenido { get; set; }
}
