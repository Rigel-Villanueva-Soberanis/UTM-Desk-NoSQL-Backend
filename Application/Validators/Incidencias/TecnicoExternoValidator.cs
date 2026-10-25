using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Domain;

namespace Application.Validators.Incidencias;

/// <summary>
/// Reglas del proveedor externo. Los largos y obligatorios ya los valida el DTO;
/// aquí se protege contra inyección NoSQL y se arma el subdocumento tecnico_externo.
/// </summary>
public static class TecnicoExternoValidator
{
    public static TecnicoExterno Validar(TecnicoExternoRequest request)
    {
        NoSqlSanitizer.ValidarTodos(
            (request.Empresa,            "empresa"),
            (request.TecnicoResponsable, "tecnicoResponsable"),
            (request.Contacto,           "contacto"),
            (request.OrdenServicio,      "ordenServicio")
        );

        return new TecnicoExterno
        {
            RequiereServicioExterno = true,
            EmpresaProveedor        = request.Empresa.Trim(),
            NombreTecnico           = Opcional(request.TecnicoResponsable),
            Contacto                = Opcional(request.Contacto),
            OrdenServicioRef        = request.OrdenServicio.Trim().ToUpperInvariant()
        };
    }

    /// <summary>¿Los datos nuevos son iguales a los que ya tiene la incidencia?</summary>
    public static bool EsIgual(TecnicoExterno? actual, TecnicoExterno nuevo)
        => actual is not null
           && actual.RequiereServicioExterno
           && TextoNormalizer.Iguales(actual.EmpresaProveedor, nuevo.EmpresaProveedor)
           && TextoNormalizer.Iguales(actual.NombreTecnico,    nuevo.NombreTecnico)
           && TextoNormalizer.Iguales(actual.Contacto,         nuevo.Contacto)
           && TextoNormalizer.Iguales(actual.OrdenServicioRef, nuevo.OrdenServicioRef);

    private static string? Opcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
