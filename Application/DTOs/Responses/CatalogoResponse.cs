using System.Text.Json;

namespace Application.DTOs.Responses;

/// <summary>
/// Respuesta del catálogo al cliente.
/// Contenido se serializa como JSON crudo para preservar su estructura dinámica.
/// </summary>
public class CatalogoResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClaveCatalogo { get; set; } = string.Empty;

    /// <summary>
    /// Contenido serializado como objeto JSON.
    /// El cliente recibe la estructura original sin conversiones.
    /// </summary>
    public object? Contenido { get; set; }
}
