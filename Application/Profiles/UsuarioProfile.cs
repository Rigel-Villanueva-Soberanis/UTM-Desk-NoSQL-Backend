using Application.DTOs.Responses;
using Domain;
using AutoMapper;

namespace Application.Profiles;

public class UsuarioProfile : Profile
{
    public UsuarioProfile()
    {
        // Entidad → Response
        // AutoMapper mapea automáticamente campos con mismo nombre:
        //   Nombres, ApellidoPaterno, ApellidoMaterno, Correo, EstadoCuenta, RolSistema, ClasificacionRol, FechaCreacion
        // El único que necesita mapeo explícito es Username → Usuario
        CreateMap<Usuario, UsuarioResponse>()
            .ForMember(dest => dest.Usuario, opt => opt.MapFrom(src => src.Username));
        // NombreCompleto es una propiedad calculada, no se mapea
    }
}
