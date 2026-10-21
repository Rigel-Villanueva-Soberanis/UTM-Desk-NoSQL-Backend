using Application.Common;

namespace Application.Common.ErrorCatalog;

/// <summary>
/// Catálogo de errores específicos para la entidad User.
/// Agrega un archivo similar por cada entidad del dominio.
/// </summary>
public static class UserErrors
{
    public static DomainException NotFound(int id) =>
        new(ApiErrors.NotFound, $"El usuario con ID {id} no fue encontrado.");

    public static DomainException AlreadyExists(string email) =>
        new(ApiErrors.Conflict, $"Ya existe un usuario con el email '{email}'.");
}
