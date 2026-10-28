using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain;

namespace Application.Profiles;

/// <summary>
/// Después de mapear una evidencia, le pone el enlace temporal firmado (UrlTemporal y VenceEn).
/// Si la evidencia es muy antigua y no tiene public_id, se devuelve la URL guardada sin vencimiento.
/// </summary>
public class EnlaceTemporalEvidencia : IMappingAction<EvidenciaIncidencia, EvidenciaResponse>
{
    private readonly IArchivoRepository _archivoRepository;

    public EnlaceTemporalEvidencia(IArchivoRepository archivoRepository)
    {
        _archivoRepository = archivoRepository;
    }

    public void Process(EvidenciaIncidencia origen, EvidenciaResponse destino, ResolutionContext contexto)
    {
        if (string.IsNullOrWhiteSpace(origen.PublicId))
        {
            destino.UrlTemporal = origen.UrlStorage;
            destino.VenceEn     = null;
            return;
        }

        var enlace = _archivoRepository.GenerarEnlaceTemporal(origen.PublicId, origen.Formato, origen.UrlStorage);
        destino.UrlTemporal = enlace.Url;
        destino.VenceEn     = enlace.VenceEn;
    }
}
