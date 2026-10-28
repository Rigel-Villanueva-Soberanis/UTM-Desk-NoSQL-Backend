namespace Application.Interfaces;

/// <summary>
/// Almacenamiento de archivos en la nube (implementado con Cloudinary).
/// Los archivos se guardan con acceso restringido: para verlos se pide un enlace temporal firmado
/// (<see cref="GenerarEnlaceTemporal"/>) que deja de funcionar pasados unos minutos.
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

    /// <summary>
    /// Referencia interna del archivo (la que se guarda en url_storage) a partir de su publicId y formato.
    /// No sirve para verlo: para eso está <see cref="GenerarEnlaceTemporal"/>.
    /// </summary>
    string ConstruirUrl(string publicId, string formato);

    /// <summary>
    /// Consulta en la nube si el archivo existe y cuánto pesa (para guardar su peso real en la evidencia).
    /// No descarga el archivo.
    /// </summary>
    Task<ConsultaArchivo> ConsultarAsync(string publicId);

    /// <summary>
    /// Enlace firmado para ver o descargar el archivo, válido solo unos minutos (la firma incluye la hora de
    /// vencimiento, así que no se puede alargar). Se calcula localmente, sin llamar a la nube.
    /// <paramref name="urlGuardada"/> es la referencia guardada en MongoDB; sirve para reconocer archivos antiguos.
    /// </summary>
    EnlaceTemporal GenerarEnlaceTemporal(string publicId, string? formato, string? urlGuardada);
}

/// <summary>Resultado de <see cref="IArchivoRepository.ConsultarAsync"/>.</summary>
public enum EstadoConsulta { Encontrado, NoExiste, NoDisponible }

/// <summary>Datos de un archivo guardado en la nube. <see cref="Bytes"/> solo es válido si <see cref="Estado"/> es Encontrado.</summary>
public record ConsultaArchivo(EstadoConsulta Estado, long Bytes);

/// <summary>Enlace firmado de un archivo y el instante (UTC) en que deja de funcionar.</summary>
public record EnlaceTemporal(string Url, DateTime VenceEn);

/// <summary>Resultado de subir un archivo.</summary>
public record ArchivoSubido(string PublicId, string Url, string Formato, long Bytes);

/// <summary>Resultado de borrar un archivo. <see cref="Motivo"/> explica la falla cuando <see cref="Eliminado"/> es false.</summary>
public record ArchivoEliminado(string PublicId, bool Eliminado, string? Motivo);
