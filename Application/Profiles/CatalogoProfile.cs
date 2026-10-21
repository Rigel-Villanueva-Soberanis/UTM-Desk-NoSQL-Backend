using AutoMapper;
using Application.DTOs.Responses;
using Domain;
using MongoDB.Bson;

namespace Application.Profiles;

public class CatalogoProfile : Profile
{
    public CatalogoProfile()
    {
        // Catalogo (entidad) → CatalogoResponse (DTO de salida)
        // Contenido se convierte de BsonArray a su representación JSON nativa
        CreateMap<Catalogo, CatalogoResponse>()
            .ForMember(dest => dest.Contenido,
                opt => opt.MapFrom(src => BsonTypeMapper.MapToDotNetValue(src.Contenido)));
    }
}
