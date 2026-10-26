using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsCatalogos;

/// <summary>
/// Errores del catálogo <c>edificios</c> (edificio con su lista de salones).
///   • Métodos que devuelven <see cref="ErrorDetalle"/>: error de un campo, se juntan en la lista del elemento.
///   • Métodos que devuelven <see cref="DomainException"/>: se lanzan con throw (respuesta completa).
/// </summary>
public static class EdificioErrors
{
    /// <summary>Máximo de salones que puede tener un edificio.</summary>
    public const int MaxSalones = 100;

    // Textos que usa ValidarUnicos: "{descripción} '{valor}' ya existe en {campo}."
    public const string DescripcionClave    = "La clave";
    public const string DescripcionEdificio = "El edificio";

    public static ErrorDetalle MuchosSalones(string campo) =>
        new(campo, $"Un edificio no puede tener más de {MaxSalones} salones.");

    public static ErrorDetalle SalonRepetido(string salon) =>
        new("salon", $"El salón '{salon}' ya existe en el edificio.");

    public static DomainException SalonesVacios() =>
        new(ApiErrors.ValidationFailed, "Debes enviar al menos un salón.");

    public static DomainException SalonNoEncontrado(string edificio, string salon) =>
        new(ApiErrors.NotFound, $"El edificio '{edificio}' no tiene el salón '{salon}'.");
}
