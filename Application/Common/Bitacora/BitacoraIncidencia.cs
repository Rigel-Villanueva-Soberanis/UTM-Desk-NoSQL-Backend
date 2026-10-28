using System.Globalization;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using Domain;

namespace Application.Common.Bitacora;

/// <summary>
/// Fábrica ÚNICA de entradas para la bitácora (historial_seguimiento).
/// Cualquier endpoint que modifique una incidencia arma aquí su entrada, así todas
/// tienen el mismo formato: tipo, autor (del token), acción, estado y fecha (UTC).
///   • Automáticas (las genera el sistema): creacion, evidencia, asignacion, reasignacion, cambio_estado, horas,
///     etiquetas, servicio_externo, edicion.
///   • Manual (la escribe el técnico): nota.
/// </summary>
public static class BitacoraIncidencia
{
    public const string TipoCreacion     = "creacion";
    public const string TipoEvidencia    = "evidencia";
    public const string TipoAsignacion   = "asignacion";
    public const string TipoReasignacion = "reasignacion";
    public const string TipoCambioEstado = "cambio_estado";
    public const string TipoNota         = "nota";
    public const string TipoHoras        = "horas";
    public const string TipoEtiquetas    = "etiquetas";
    public const string TipoExterno      = "servicio_externo";
    public const string TipoEdicion      = "edicion";

    /// <summary>Firma del usuario que hace la acción (datos del token).</summary>
    public static UsuarioReferencia Autor(UsuarioActual usuario) =>
        new() { IdUsuario = usuario.Id, Nombre = usuario.Nombre, Rol = usuario.Rol };

    public static SeguimientoIncidencia Creacion(UsuarioActual usuario, string estado) =>
        Entrada(TipoCreacion, usuario, "Incidencia registrada en el sistema.", estado);

    public static SeguimientoIncidencia Evidencia(UsuarioActual usuario, string nombreArchivo, string estado) =>
        Entrada(TipoEvidencia, usuario, $"Evidencia agregada: {nombreArchivo}", estado);

    public static SeguimientoIncidencia Asignacion(UsuarioActual usuario, string nombreTecnico, string estado) =>
        Entrada(TipoAsignacion, usuario, $"Asignación inicial: se asignó al técnico {nombreTecnico}.", estado);

    public static SeguimientoIncidencia Reasignacion(UsuarioActual usuario, string tecnicoAnterior, string tecnicoNuevo, string estado) =>
        Entrada(TipoReasignacion, usuario, $"Reasignación: de {tecnicoAnterior} a {tecnicoNuevo}.", estado);

    public static SeguimientoIncidencia CambioEstado(UsuarioActual usuario, string anterior, string nuevo, string motivo) =>
        Entrada(TipoCambioEstado, usuario, $"Cambio de estado: {anterior} → {nuevo}. Motivo: {motivo}", nuevo, anterior);

    public static SeguimientoIncidencia Nota(UsuarioActual usuario, string nota, string estado) =>
        Entrada(TipoNota, usuario, nota, estado);

    public static SeguimientoIncidencia Horas(UsuarioActual usuario, int minutosTotales, string? comentario, string estado)
    {
        var texto = $"Se registraron {DuracionHoras.Texto(minutosTotales)} de trabajo.";
        if (!string.IsNullOrWhiteSpace(comentario))
            texto += $" {comentario.Trim()}";
        return Entrada(TipoHoras, usuario, texto, estado);
    }

    public static SeguimientoIncidencia Etiquetas(UsuarioActual usuario, IReadOnlyList<string> agregadas, IReadOnlyList<string> quitadas, string estado)
    {
        var partes = new List<string>();
        if (agregadas.Count > 0) partes.Add($"Etiquetas agregadas: {string.Join(", ", agregadas)}.");
        if (quitadas.Count  > 0) partes.Add($"Etiquetas quitadas: {string.Join(", ", quitadas)}.");
        return Entrada(TipoEtiquetas, usuario, string.Join(" ", partes), estado);
    }

    public static SeguimientoIncidencia ServicioExterno(UsuarioActual usuario, string empresa, string ordenServicio, bool actualizacion, string estado)
    {
        var texto = actualizacion
            ? $"Datos del proveedor externo actualizados: {empresa}. Orden de servicio: {ordenServicio}."
            : $"Incidencia canalizada con proveedor externo: {empresa}. Orden de servicio: {ordenServicio}.";
        return Entrada(TipoExterno, usuario, texto, estado);
    }

    public static SeguimientoIncidencia Edicion(UsuarioActual usuario, IEnumerable<string> campos, string estado) =>
        Entrada(TipoEdicion, usuario, $"Datos generales corregidos: {string.Join(", ", campos)}.", estado);

    private static SeguimientoIncidencia Entrada(string tipo, UsuarioActual usuario, string accion, string estado, string? estadoAnterior = null) =>
        new()
        {
            Tipo            = tipo,
            Autor           = Autor(usuario),
            Accion          = accion,
            EstadoAnterior  = estadoAnterior,
            EstadoReportado = estado,
            Fecha           = DateTime.UtcNow
        };
}
