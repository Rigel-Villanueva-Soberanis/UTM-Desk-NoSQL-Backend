using Api.Common;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Incidencias — Fase 5: búsqueda full-text y estadísticas.
/// En Swagger aparece como su propio apartado, justo debajo de "Incidencia".
/// </summary>
[ApiController]
[Route("api")]
[Tags("Incidencia · Analítica")]
[Produces("application/json")]
[Authorize]
public class IncidenciaAnaliticaController : ControllerBase
{
    private readonly AnaliticaService _service;

    public IncidenciaAnaliticaController(AnaliticaService service)
    {
        _service = service;
    }

    /// <remarks>
    /// Búsqueda full-text en `titulo` y `descripcion`, ordenada de la más relevante a la menos relevante.
    ///
    /// **Roles:** Root, Soporte de TI, Directivos.
    ///
    /// - Usa `$text` con el índice de texto `ix_texto_incidencias` (idioma español): ignora mayúsculas, acentos
    ///   y palabras comunes ("el", "de", "la"), y reconoce variantes ("proyectores" encuentra "proyector").
    /// - Cada fila trae `relevancia` (textScore de MongoDB). El título pesa el doble que la descripción.
    /// - `q` con varias palabras busca cualquiera de ellas; `"pantalla azul"` entre comillas busca la frase exacta;
    ///   `-impresora` excluye esa palabra.
    /// - Acepta los mismos filtros del listado: `estado`, `prioridad`, `categoria`, `edificio`, `tag`,
    ///   `asignada`, `conProveedorExterno`, `fechaDesde`, `fechaHasta`.
    ///
    /// Ejemplos: `?q=pantalla+azul` · `?q=proyector+lab&amp;estado=abierta`
    /// </remarks>
    /// <response code="200">Página de resultados ordenada por relevancia.</response>
    /// <response code="400">Falta `q`, es muy corto o algún filtro es inválido.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede usar la búsqueda.</response>
    [HttpGet("incidencias/buscar")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Soporte + "," + RolesSistema.Directivos)]
    public async Task<IActionResult> Buscar([FromQuery] BusquedaIncidenciaRequest request)
    {
        var resultado = await _service.BuscarAsync(request);
        return Ok(ApiResponse<PaginadoResponse<IncidenciaBusquedaResponse>>.Ok(resultado));
    }

    /// <remarks>
    /// Tiempo de resolución por edificio.
    ///
    /// **Roles:** Root, Directivos.
    ///
    /// Pipeline de agregación en MongoDB (no se cargan los documentos al backend):
    ///
    /// 1. `$match`: solo incidencias `resuelta` o `cerrada` con `fechas.creacion` y `fechas.resolucion` registradas.
    /// 2. `$project`: horas de atención = `fechas.resolucion` − `fechas.creacion`.
    /// 3. `$group` por `ubicacion.edificio`: `totalReportes` (`$sum: 1`), el promedio (`$avg`), el caso más rápido y el más lento, y la suma (`$sum`) de las horas de trabajo registradas (`horas_invertidas`). Los tiempos se devuelven en horas y minutos (`tiempoPromedio`, `tiempoMinimo`, `tiempoMaximo`, `tiempoTrabajado`). Los primeros tres son tiempo de reloj (creación → resolución); `tiempoTrabajado` es el trabajo que los técnicos anotaron.
    /// 4. `$sort`: `ordenarPor=reportes` (por defecto) o `ordenarPor=tiempo`, de mayor a menor.
    ///
    /// Filtros: `fechaDesde` / `fechaHasta` (fecha de resolución, hora de México), `categoria`, `prioridad`.
    ///
    /// Nota: las resueltas se borran solas a los 60 días (índice TTL), así que el reporte cubre los últimos 60 días.
    /// </remarks>
    /// <response code="200">Lista de edificios con sus tiempos.</response>
    /// <response code="400">Filtros inválidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede ver este reporte.</response>
    [HttpGet("analitica/tiempos-edificio")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Directivos)]
    public async Task<IActionResult> TiemposPorEdificio([FromQuery] TiemposEdificioFiltroRequest filtro)
    {
        var resultado = await _service.TiemposPorEdificioAsync(filtro);
        return Ok(ApiResponse<List<TiempoEdificioResponse>>.Ok(resultado));
    }

    /// <remarks>
    /// KPIs generales del departamento de TI.
    ///
    /// **Roles:** Root, Administrativos.
    ///
    /// Un solo pipeline (`$match` + `$facet`) que calcula tres indicadores sobre los mismos documentos:
    ///
    /// 1. **Horas hombre totales:** `$sum` de `horas_invertidas` (y el promedio por incidencia con tiempo). Se devuelve en horas y minutos (`tiempoHombreTotal`, `promedioPorIncidencia`).
    /// 2. **Top 10 de fallas recurrentes:** `$unwind` de `tags` → `$group` por etiqueta → `$sort` → `$limit: 10`.
    /// 3. **Proveedores externos:** incidencias con `tecnico_externo` registrado: total de casos, porcentaje y casos por empresa.
    ///
    /// Filtros: `fechaDesde` / `fechaHasta` (fecha de creación, hora de México), `categoria`, `edificio` y `estado`.
    ///
    /// **Filtro `estado`:** si no se manda, se cuentan incidencias de **todos** los estados. Si se manda, solo las de esos estados
    /// (se repite el parámetro: `?estado=resuelta&amp;estado=cancelada`, o con coma: `?estado=resuelta,cancelada`).
    /// Todos los indicadores (`totalIncidencias`, horas hombre, promedio, etiquetas y proveedores externos) se calculan
    /// solo sobre las incidencias que cumplen el filtro; el porcentaje de externos se saca sobre ese mismo total.
    /// </remarks>
    /// <response code="200">Indicadores calculados.</response>
    /// <response code="400">Filtros inválidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede ver este reporte.</response>
    [HttpGet("analitica/eficiencia-global")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Administrativos)]
    public async Task<IActionResult> EficienciaGlobal([FromQuery] EficienciaGlobalFiltroRequest filtro)
    {
        var resultado = await _service.EficienciaGlobalAsync(filtro);
        return Ok(ApiResponse<EficienciaGlobalResponse>.Ok(resultado));
    }
}
