using Application.DTOs.Responses;
using Domain;
using AutoMapper;
using MongoDB.Bson;

namespace Application.Profiles;

public class CatalogoProfile : Profile
{
    public CatalogoProfile()
    {
        // Catalogo (entidad) → CatalogoResponse (DTO de salida)
        // Contenido (array u object de BSON) se convierte a su representación .NET (List / Dictionary)
        CreateMap<Catalogo, CatalogoResponse>()
            .ForMember(dest => dest.Contenido,
                opt => opt.MapFrom(src => BsonTypeMapper.MapToDotNetValue(src.Contenido)));
    }
}
