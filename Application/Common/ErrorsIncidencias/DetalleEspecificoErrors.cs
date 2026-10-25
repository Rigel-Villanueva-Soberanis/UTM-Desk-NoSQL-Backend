using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsIncidencias;

/// <summary>
/// Errores del <c>detalle_especifico</c> de una incidencia (campos dinámicos de la categoría).
/// Cada método devuelve el error de un campo; se juntan en la lista de errores de la incidencia.
/// </summary>
public static class DetalleEspecificoErrors
{
    public static ErrorDetalle FormatoInvalido(string campo) =>
        new(campo, "Tiene un formato inválido.");

    public static ErrorDetalle NoEsObjeto(string campo) =>
        new(campo, "Debe ser un objeto con los campos de la categoría.");

    public static ErrorDetalle SinCamposAdicionales(string campo) =>
        new(campo, "Esta categoría no tiene campos adicionales.");

    public static ErrorDetalle CampoNoDefinido(string campo, IEnumerable<string> camposValidos) =>
        new(campo, $"Campo no definido para la categoría. Campos válidos: {string.Join(", ", camposValidos)}.");

    public static ErrorDetalle Obligatorio(string campo) =>
        new(campo, "Es obligatorio.");

    public static ErrorDetalle DebeSerTexto(string campo) =>
        new(campo, "Debe ser texto.");

    public static ErrorDetalle MuyLargo(string campo, int maxLongitud) =>
        new(campo, $"No puede tener más de {maxLongitud} caracteres.");

    public static ErrorDetalle DebeSerNumero(string campo) =>
        new(campo, "Debe ser un número.");

    public static ErrorDetalle OpcionNoValida(string campo, IEnumerable<string> opciones) =>
        new(campo, $"Debe ser una de estas opciones: {string.Join(", ", opciones)}.");

    public static ErrorDetalle FechaInvalida(string campo) =>
        new(campo, "Debe ser una fecha válida (ej. 2026-10-24).");
}
