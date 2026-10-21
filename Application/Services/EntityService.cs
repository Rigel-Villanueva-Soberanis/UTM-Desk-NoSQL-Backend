using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain;

namespace Application.Services;

/// <summary>
/// Servicio de ejemplo que implementa los casos de uso para la entidad genérica.
/// Crea un servicio similar por cada entidad del dominio.
/// </summary>
public class EntityService
{
    private readonly IEntityRepository _repository;
    private readonly IMapper _mapper;

    public EntityService(IEntityRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EntityResponse>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<EntityResponse>>(entities);
    }

    public async Task<EntityResponse?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<EntityResponse>(entity);
    }

    public async Task<int> CreateAsync(CreateEntityRequest request)
    {
        var entity = new Entity { Name = request.Name };
        return await _repository.CreateAsync(entity);
    }
}
