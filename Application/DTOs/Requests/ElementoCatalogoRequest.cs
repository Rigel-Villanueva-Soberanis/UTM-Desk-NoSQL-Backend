using System.Text.Json.Serialization;
using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Un elemento de catálogo (para POST y PATCH de /api/catalogos/{clave}/elementos).
/// Se manda "plano", con los campos propios del catálogo. Ej. para edificios:
/// { "clave": "aula_a1", "edificio": "Edificio A", "salon": "Aula 1" }
/// Los campos se capturan dinámicamente porque cada catálogo tiene su estructura;
/// el validador del catálogo se encarga de revisarlos.
/// </summary>
public class ElementoCatalogoRequest
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Campos { get; set; } = new();
}
