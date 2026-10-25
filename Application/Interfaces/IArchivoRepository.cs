namespace Application.Interfaces;

/// <summary>
/// Almacenamiento de archivos en la nube (implementado con Cloudinary).
/// Por ahora las URLs son permanentes (sin expiración).
/// </summary>
public interface IArchivoRepository
{
    /// <summary>
    /// Sube el archivo a la <paramref name="carpeta"/> indicada.
    /// Con <paramref name="optimizarImagen"/> = true la imagen se convierte a WebP y se comprime antes de guardarse.
    /// </summary>
    Task<ArchivoSubido> SubirAsync(Stream contenido, string nombreArchivo, string carpeta, bool optimizarImagen);

    /// <summary>
    /// Borra físicamente el archivo de la nube. Si ya no existía también cuenta como borrado.
    /// </summary>
    Task<ArchivoEliminado> EliminarAsync(string publicId);

    /// <summary>URL pública del archivo a partir de su publicId y formato.</summary>
    string ConstruirUrl(string publicId, string formato);
}

/// <summary>Resultado de subir un archivo.</summary>
public record ArchivoSubido(string PublicId, string Url, string Formato, long Bytes);

/// <summary>Resultado de borrar un archivo. <see cref="Motivo"/> explica la falla cuando <see cref="Eliminado"/> es false.</summary>
public record ArchivoEliminado(string PublicId, bool Eliminado, string? Motivo);
