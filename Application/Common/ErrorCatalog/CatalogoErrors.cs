using Application.Common;

namespace Application.Common.ErrorCatalog;

public static class CatalogoErrors
{
    public static DomainException NotFound(string id) =>
        new(ApiErrors.NotFound, $"El catálogo con ID '{id}' no fue encontrado.");

    public static DomainException ClaveNotFound(string clave) =>
        new(ApiErrors.NotFound, $"No existe un catálogo con la clave '{clave}'.");

    public static DomainException ClaveRepetida(string clave) =>
        new(ApiErrors.Conflict, $"Ya existe un catálogo con la clave '{clave}'.");
}
