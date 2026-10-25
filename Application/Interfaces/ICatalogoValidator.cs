using Application.Common.ErrorCatalog;
using MongoDB.Bson;

namespace Application.Interfaces;

/// <summary>
/// Contrato que cada catálogo editable implementa con sus reglas propias:
/// quién puede editarlo, cómo se identifica cada elemento y cómo debe ser su contenido.
/// Para hacer editable un catálogo nuevo: crear una clase en Application/Validators/Catalogos
/// y registrarla en DependencyInjection.
/// </summary>
public interface ICatalogoValidator
{
    /// <summary>Clave del catálogo en MongoDB (ej. "roles_sistema").</summary>
    string ClaveCatalogo { get; }

    /// <summary>Roles (en minúsculas, sin acentos) que pueden modificar este catálogo.</summary>
    IReadOnlyCollection<string> RolesEditores { get; }

    /// <summary>Campo que identifica a cada elemento dentro del contenido (ej. "clave" o "rol").</summary>
    string CampoIdentificador { get; }

    /// <summary>Si se pueden agregar elementos nuevos con POST (roles_sistema no, porque sus roles son fijos).</summary>
    bool PermiteAgregarElementos { get; }

    /// <summary>
    /// Valida el contenido nuevo completo (comparándolo con el actual si hace falta)
    /// y devuelve el contenido limpio y normalizado que se guardará.
    /// Lanza DomainException con la lista de errores si no es válido.
    /// </summary>
    BsonValue Validar(BsonValue contenidoNuevo, BsonValue contenidoActual);
}
