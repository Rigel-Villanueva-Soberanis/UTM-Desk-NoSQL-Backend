using Application.Interfaces;
using Infrastructure.Data;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;

namespace Infrastructure.Repositories;

/// <summary>
/// Almacenamiento de archivos en Cloudinary (RF-17).
/// Los archivos se suben con entrega pública ("upload"): su URL es permanente.
/// Las imágenes se guardan optimizadas: WebP, calidad automática y máximo 1920 px por lado.
/// </summary>
public class ArchivoRepository : IArchivoRepository
{
    private const string TipoEntrega = "upload";

    /// <summary>Formato en el que se guardan las imágenes (más ligero que JPG/PNG con buena calidad).</summary>
    private const string FormatoImagen = "webp";

    /// <summary>Lado máximo en píxeles; las fotos más grandes se reducen sin deformarse.</summary>
    private const int LadoMaximo = 1920;

    private readonly CloudinaryContext _cloudinary;

    public ArchivoRepository(CloudinaryContext cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<ArchivoSubido> SubirAsync(Stream contenido, string nombreArchivo, string carpeta, bool optimizarImagen)
    {
        // Imágenes y PDF se suben como recurso "image" (Cloudinary maneja los PDF como imagen)
        var parametros = new ImageUploadParams
        {
            File   = new FileDescription(nombreArchivo, contenido),
            Folder = carpeta,
            Type   = TipoEntrega
        };

        if (optimizarImagen)
        {
            // Cloudinary convierte y comprime ANTES de guardar: en la nube solo queda la versión ligera
            parametros.Format         = FormatoImagen;
            parametros.Transformation = new Transformation()
                .Width(LadoMaximo).Height(LadoMaximo).Crop("limit")   // reduce solo si es más grande
                .Quality("auto:good");                                // compresión con buena calidad visual
        }

        var resultado = await _cloudinary.Cliente.UploadAsync(parametros);

        if (resultado.Error is not null)
            throw new InvalidOperationException($"Cloudinary rechazó la subida: {resultado.Error.Message}");

        var url = resultado.SecureUrl?.ToString() ?? ConstruirUrl(resultado.PublicId, resultado.Format);
        return new ArchivoSubido(resultado.PublicId, url, resultado.Format, resultado.Bytes);
    }

    /// <summary>
    /// Borra el archivo con DestroyAsync e invalida la copia en caché del CDN.
    /// "ok" = borrado · "not found" = ya no existía (también cuenta como borrado).
    /// </summary>
    public async Task<ArchivoEliminado> EliminarAsync(string publicId)
    {
        try
        {
            var resultado = await _cloudinary.Cliente.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,   // imágenes y PDF se subieron como "image"
                Type         = TipoEntrega,
                Invalidate   = true
            });

            if (resultado.Error is not null)
                return new ArchivoEliminado(publicId, false, resultado.Error.Message);

            return resultado.Result is "ok" or "not found"
                ? new ArchivoEliminado(publicId, true, null)
                : new ArchivoEliminado(publicId, false, $"Respuesta inesperada de Cloudinary: '{resultado.Result}'.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ArchivoEliminado(publicId, false, "No se pudo conectar con Cloudinary.");
        }
    }

    public string ConstruirUrl(string publicId, string formato)
        => $"https://res.cloudinary.com/{_cloudinary.CloudName}/image/{TipoEntrega}/{publicId}.{formato}";
}
