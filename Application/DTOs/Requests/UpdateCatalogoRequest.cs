using System.Text.Json;

namespace Application.DTOs.Requests;

/// <summary>
/// Datos para modificar el contenido de un catálogo existente.
/// La clave va en la URL (PUT /api/catalogos/{clave}) y no se puede cambiar.
/// </summary>
public class UpdateCatalogoRequest
{
    /// <summary>
    /// Nuevo contenido. Reemplaza completamente el anterior.
    /// Se recibe como JsonElement porque cada catálogo tiene su propia estructura;
    /// el validador del catálogo se encarga de revisarla.
    /// </summary>
    public JsonElement Contenido { get; set; }
}
