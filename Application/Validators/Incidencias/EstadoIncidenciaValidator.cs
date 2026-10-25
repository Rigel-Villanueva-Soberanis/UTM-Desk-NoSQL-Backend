using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;

namespace Application.Validators.Incidencias;

/// <summary>
/// Reglas para cambiar el estado de una incidencia.
/// Flujo normal: abierta → en proceso → resuelta / cancelada.
///   • Root, Soporte y Administrativos: pueden hacer cualquier cambio (incluida la reapertura resuelta → en proceso).
///   • Técnico: solo abierta → en proceso y en proceso → resuelta (y solo en sus incidencias asignadas).
/// Los mensajes de error están en <see cref="EstadoErrors"/>.
/// </summary>
public static class EstadoIncidenciaValidator
{
    public const string Abierta   = "abierta";
    public const string EnProceso = "en proceso";
    public const string Resuelta  = "resuelta";
    public const string Cancelada = "cancelada";

    /// <summary>Cambios que puede hacer un técnico: estado actual → estados a los que puede pasar.</summary>
    private static readonly Dictionary<string, string[]> FlujoTecnico = new()
    {
        [Abierta]   = [EnProceso],
        [EnProceso] = [Resuelta]
    };

    /// <summary>Normaliza el estado recibido ("En Proceso" → "en proceso") o lanza error si no existe.</summary>
    public static string ValidarEstado(string? valor)
    {
        var estado = string.IsNullOrWhiteSpace(valor)
            ? null
            : IncidenciaValidator.Estados.FirstOrDefault(e => TextoNormalizer.Iguales(e, valor));

        return estado ?? throw EstadoErrors.NoValido(valor, IncidenciaValidator.Estados);
    }

    /// <summary>Lanza el error correspondiente si el usuario no puede pasar de <paramref name="actual"/> a <paramref name="nuevo"/>.</summary>
    public static void ValidarTransicion(string actual, string nuevo, UsuarioActual usuario)
    {
        if (actual == nuevo)
            throw EstadoErrors.MismoEstado(actual);

        if (!usuario.EsRol(RolesSistema.Tecnico))
            return; // Root, Soporte y Administrativos: cualquier cambio

        if (nuevo != EnProceso && nuevo != Resuelta)
            throw EstadoErrors.TecnicoSinPermiso(nuevo);

        var permitidos = FlujoTecnico.TryGetValue(actual, out var destinos) ? destinos : [];
        if (!permitidos.Contains(nuevo))
            throw EstadoErrors.TransicionNoPermitida(actual, nuevo, permitidos);
    }
}
