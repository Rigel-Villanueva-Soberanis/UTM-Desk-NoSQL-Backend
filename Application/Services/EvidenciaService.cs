using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Evidencias;

namespace Application.Services;

/// <summary>
/// Casos de uso de evidencias — Fase 2: subida de archivos a Cloudinary (RF-17).
/// Las reglas de los archivos están en <see cref="EvidenciaValidator"/>.
/// </summary>
public class EvidenciaService
{
    private readonly IArchivoRepository _archivoRepository;

    public EvidenciaService(IArchivoRepository archivoRepository)
    {
        _archivoRepository = archivoRepository;
    }

    /// <summary>
    /// Valida de 1 a 5 archivos y los sube a la carpeta del usuario: utmdesk/evidencias/{idUsuario}.
    /// Primero se validan TODOS; si alguno está mal no se sube ninguno.
    /// Las imágenes se convierten a WebP comprimido; los PDF se guardan tal cual.
    /// </summary>
    public async Task<List<EvidenciaSubidaResponse>> SubirAsync(IReadOnlyList<ArchivoEntrada> archivos, UsuarioActual usuario)
    {
        var validados = EvidenciaValidator.ValidarArchivos(archivos);
        var carpeta   = EvidenciaValidator.CarpetaDe(usuario.Id);
        var subidos   = new List<EvidenciaSubidaResponse>();

        foreach (var validado in validados)
        {
            var subido = await _archivoRepository.SubirAsync(
                validado.Archivo.Contenido, validado.Nombre, carpeta, optimizarImagen: validado.EsImagen);

            // El nombre refleja el formato real guardado (ej. foto.jpg → foto.webp)
            var nombre = validado.EsImagen ? Path.ChangeExtension(validado.Nombre, subido.Formato) : validado.Nombre;

            var enlace = _archivoRepository.GenerarEnlaceTemporal(subido.PublicId, subido.Formato, subido.Url);

            subidos.Add(new EvidenciaSubidaResponse
            {
                PublicId      = subido.PublicId,
                NombreArchivo = nombre,
                Formato       = subido.Formato,
                Bytes         = subido.Bytes,
                TipoMime      = EvidenciaValidator.TipoMimeDe(subido.Formato),
                PesoKb        = EvidenciaValidator.PesoEnKb(subido.Bytes),
                UrlTemporal   = enlace.Url,
                VenceEn       = enlace.VenceEn
            });
        }

        return subidos;
    }
}
