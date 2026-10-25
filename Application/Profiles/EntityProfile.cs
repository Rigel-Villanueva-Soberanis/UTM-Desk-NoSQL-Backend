using Application.DTOs.Responses;
using Domain;
using AutoMapper;

namespace Application.Profiles;

/// <summary>
/// Perfil de AutoMapper para la entidad de ejemplo.
/// Crea un perfil similar por cada entidad del dominio.
/// </summary>
public class EntityProfile : Profile
{
    public EntityProfile()
    {
        CreateMap<Entity, EntityResponse>();
    }
}
