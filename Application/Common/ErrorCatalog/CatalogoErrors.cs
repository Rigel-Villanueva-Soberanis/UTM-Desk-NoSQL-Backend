using Application.Common;

namespace Application.Common.ErrorCatalog;

public static class CatalogoErrors
{
    public static DomainException ClaveNotFound(string clave) =>
        new(ApiErrors.NotFound, $"No existe un catálogo con la clave '{clave}'.");

    public static DomainException NoEditable(string clave) =>
        new(ApiErrors.Forbidden, $"El catálogo no se puede modificar.");

    public static DomainException SinPermiso(string clave, string rol) =>
        new(ApiErrors.Forbidden, $"No tienes permiso para modificar el catálogo.");

    public static DomainException ContenidoObligatorio() =>
        new(ApiErrors.ValidationFailed, "El campo 'contenido' es obligatorio.");

    public static DomainException ContenidoTipoInvalido() =>
        new(ApiErrors.ValidationFailed, "El campo 'contenido' debe ser un arreglo (array) o un objeto (object).");

    public static DomainException FormatoJsonInvalido() =>
        new(ApiErrors.ValidationFailed, "El contenido del catálogo tiene un formato inválido. Verifica la estructura.");

    public static DomainException EstructuraInvalida(string clave) =>
        new(ApiErrors.InternalError, $"El catálogo no tiene la estructura esperada.");

    public static DomainException NoManejaElementos() =>
        new(ApiErrors.ValidationFailed, "Este catálogo no maneja elementos individuales.");

    public static DomainException NoPermiteAgregar() =>
        new(ApiErrors.Forbidden, "Este catálogo no permite agregar elementos nuevos. Solo se pueden consultar o modificar los existentes.");

    public static DomainException ElementoNoEncontrado(string id) =>
        new(ApiErrors.NotFound, $"No existe el elemento '{id}' en el catálogo.");

    public static DomainException ElementoVacio() =>
        new(ApiErrors.ValidationFailed, "Debes enviar al menos un campo.");

    public static DomainException ElementoFormatoInvalido() =>
        new(ApiErrors.ValidationFailed, "El elemento debe ser un objeto JSON con sus campos.");

    public static DomainException IdentificadorNoEditable(string campo) =>
        new(ApiErrors.ValidationFailed, "El elemento no es válido. Revisa los errores.",
            [new ErrorDetalle(campo, $"El campo '{campo}' identifica al elemento y no se puede cambiar.")]);

    public static DomainException ElementoInvalido(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "El elemento no es válido. Revisa los errores.", errores);

    public static DomainException ContenidoInvalido(string clave, IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, $"El contenido del catálogo '{clave}' no es válido. Revisa los errores.", errores);
}
