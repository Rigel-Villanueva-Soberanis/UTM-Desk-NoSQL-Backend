using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para crear un catálogo.
/// El cliente manda la clave y el contenido.
/// El ID lo genera MongoDB automáticamente.
/// </summary>
public class CreateCatalogoRequest
{
    /// <summary>
    /// Identificador semántico del catálogo. Ej: "estatus_incidencia", "roles_sistema".
    /// </summary>
    public string ClaveCatalogo { get; set; } = string.Empty;

    /// <summary>
    /// Contenido dinámico: puede ser lista de strings u objetos.
    /// Se recibe como JsonElement para no restringir la estructura.
    /// </summary>
    public JsonElement Contenido { get; set; }
}
