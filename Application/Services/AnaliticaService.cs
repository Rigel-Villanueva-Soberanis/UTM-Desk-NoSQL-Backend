using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Incidencias;
using AutoMapper;

namespace Application.Services;

/// <summary>
/// Fase 5: búsqueda full-text y reportes estadísticos de incidencias.
/// Solo orquesta: valida los filtros, llama al repositorio (los cálculos los hace MongoDB) y arma la respuesta.
/// </summary>
public class AnaliticaService
{
    private readonly IIncidenciaRepository _incidenciaRepository;
    private readonly IAnaliticaRepository _analiticaRepository;
    private readonly IMapper _mapper;

    public AnaliticaService(IIncidenciaRepository incidenciaRepository, IAnaliticaRepository analiticaRepository, IMapper mapper)
    {
        _incidenciaRepository = incidenciaRepository;
        _analiticaRepository  = analiticaRepository;
        _mapper               = mapper;
    }

    // ─── GET /api/incidencias/buscar ────────────────────────────────────────

    /// <summary>Búsqueda $text en título y descripción, de la más relevante a la menos relevante.</summary>
    public async Task<PaginadoResponse<IncidenciaBusquedaResponse>> BuscarAsync(BusquedaIncidenciaRequest request)
    {
        var (filtros, texto) = IncidenciaValidator.ValidarBusqueda(request);

        var consulta = new IncidenciaConsulta(
            Pagina:       request.Pagina,
            TamanoPagina: request.TamanoPagina,
            IdReportante: null,              // Root, Soporte y Directivos ven todas
            IdTecnico:    null,
            Estado:       filtros.Estado,
            Prioridad:    filtros.Prioridad,
            Categoria:    filtros.Categoria,
            Edificio:     filtros.Edificio,
            Asignada:     request.Asignada,
            Tag:          filtros.Tag,
            ConProveedorExterno: request.ConProveedorExterno,
            Desde:        filtros.Desde,
            Hasta:        filtros.Hasta);

        var (items, total) = await _incidenciaRepository.BuscarTextoAsync(consulta, texto);

        var filas = items.Select(r =>
        {
            var fila = _mapper.Map<IncidenciaBusquedaResponse>(r.Incidencia);
            fila.Relevancia = r.Relevancia;
            return fila;
        }).ToList();

        return new PaginadoResponse<IncidenciaBusquedaResponse>
        {
            Items        = filas,
            Pagina       = consulta.Pagina,
            TamanoPagina = consulta.TamanoPagina,
            Total        = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)consulta.TamanoPagina)
        };
    }

    // ─── GET /api/analitica/tiempos-edificio ────────────────────────────────

    public async Task<List<TiempoEdificioResponse>> TiemposPorEdificioAsync(TiemposEdificioFiltroRequest filtro)
    {
        var consulta = AnaliticaValidator.ValidarTiempos(filtro);
        var datos    = await _analiticaRepository.TiemposPorEdificioAsync(consulta);

        return datos.Select(d => new TiempoEdificioResponse
        {
            Edificio      = d.Edificio,
            TotalReportes = d.TotalReportes,
            PromedioHoras = d.PromedioHoras,
            MinimoHoras   = d.MinimoHoras,
            MaximoHoras   = d.MaximoHoras
        }).ToList();
    }

    // ─── GET /api/analitica/eficiencia-global ───────────────────────────────

    public async Task<EficienciaGlobalResponse> EficienciaGlobalAsync(EficienciaGlobalFiltroRequest filtro)
    {
        var consulta = AnaliticaValidator.ValidarEficiencia(filtro);
        var datos    = await _analiticaRepository.EficienciaGlobalAsync(consulta);

        return new EficienciaGlobalResponse
        {
            TotalIncidencias           = datos.TotalIncidencias,
            HorasHombreTotales         = datos.HorasHombreTotales,
            IncidenciasConHoras        = datos.IncidenciasConHoras,
            PromedioHorasPorIncidencia = datos.IncidenciasConHoras == 0
                ? 0
                : Math.Round(datos.HorasHombreTotales / datos.IncidenciasConHoras, 2),
            TopTags = datos.TopTags
                .Select(t => new TagFrecuenciaResponse { Tag = t.Tag, Total = t.Total })
                .ToList(),
            ProveedoresExternos = new ProveedoresExternosResponse
            {
                TotalCasos = datos.CasosExternos,
                Porcentaje = datos.TotalIncidencias == 0
                    ? 0
                    : Math.Round(datos.CasosExternos * 100.0 / datos.TotalIncidencias, 2),
                PorEmpresa = datos.CasosPorEmpresa
                    .Select(e => new CasosEmpresaResponse { Empresa = e.Empresa, Total = e.Total })
                    .ToList()
            }
        };
    }
}
