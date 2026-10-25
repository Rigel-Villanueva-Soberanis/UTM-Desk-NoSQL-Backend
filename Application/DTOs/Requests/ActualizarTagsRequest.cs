namespace Application.DTOs.Requests;

/// <summary>
/// Etiquetas a agregar y/o quitar de una incidencia. Se puede mandar solo una de las dos listas.
/// El backend las convierte a minúsculas y sin acentos ("Proyector Eléctrico" → "proyector electrico").
/// </summary>
public class ActualizarTagsRequest
{
    /// <summary>Etiquetas a agregar ($addToSet: si ya existe en el ticket no se duplica).</summary>
    /// <example>["proyector", "Cable HDMI"]</example>
    public List<string>? Agregar { get; set; }

    /// <summary>Etiquetas a quitar ($pull).</summary>
    /// <example>["clima"]</example>
    public List<string>? Quitar { get; set; }
}
