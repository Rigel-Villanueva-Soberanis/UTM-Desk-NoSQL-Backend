namespace Application.DTOs.Requests;

/// <summary>
/// Un archivo recibido en el upload, sin depender de ASP.NET (IFormFile se queda en el controller).
/// </summary>
/// <param name="Contenido">Contenido del archivo.</param>
/// <param name="Nombre">Nombre original (ej. "foto.jpg").</param>
/// <param name="TipoContenido">Content-Type (ej. "image/jpeg").</param>
/// <param name="Tamano">Tamaño en bytes.</param>
public record ArchivoEntrada(Stream Contenido, string? Nombre, string? TipoContenido, long Tamano);
