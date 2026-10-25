using Application.DTOs.Responses;
using Domain;
using AutoMapper;
using MongoDB.Bson;

namespace Application.Profiles;

public class IncidenciaProfile : Profile
{
    public IncidenciaProfile()
    {
        // Expediente completo
        CreateMap<Incidencia, IncidenciaResponse>()
            .ForMember(dest => dest.DetalleEspecifico,
                opt => opt.MapFrom(src => BsonTypeMapper.MapToDotNetValue(src.DetalleEspecifico)));

        CreateMap<UbicacionIncidencia, UbicacionResponse>();
        CreateMap<DatosReportante, DatosReportanteResponse>();
        CreateMap<UsuarioReferencia, UsuarioReferenciaResponse>();
        CreateMap<TecnicoExterno, TecnicoExternoResponse>();
        CreateMap<SeguimientoIncidencia, SeguimientoResponse>();
        CreateMap<EvidenciaIncidencia, EvidenciaResponse>();
        CreateMap<FechasIncidencia, FechasResponse>();

        // Fila del listado
        CreateMap<Incidencia, IncidenciaResumenResponse>()
            .ForMember(dest => dest.Edificio,         opt => opt.MapFrom(src => src.Ubicacion.Edificio))
            .ForMember(dest => dest.Aula,             opt => opt.MapFrom(src => src.Ubicacion.Aula))
            .ForMember(dest => dest.NombreReportante, opt => opt.MapFrom(src => src.DatosReportante.Nombre))
            .ForMember(dest => dest.Asignada,         opt => opt.MapFrom(src => src.TecnicoAsignado != null))
            .ForMember(dest => dest.IdTecnico,        opt => opt.MapFrom(src => src.TecnicoAsignado != null ? src.TecnicoAsignado.IdUsuario : null))
            .ForMember(dest => dest.NombreTecnico,    opt => opt.MapFrom(src => src.TecnicoAsignado != null ? src.TecnicoAsignado.Nombre : null))
            .ForMember(dest => dest.FechaCreacion,    opt => opt.MapFrom(src => src.Fechas.Creacion));

        // Resultado de la búsqueda full-text: igual que el resumen; Relevancia la pone el servicio
        CreateMap<Incidencia, IncidenciaBusquedaResponse>()
            .IncludeBase<Incidencia, IncidenciaResumenResponse>()
            .ForMember(dest => dest.Relevancia, opt => opt.Ignore());
    }
}
