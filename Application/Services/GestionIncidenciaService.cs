using Application.Common.Bitacora;
using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Incidencias;
using AutoMapper;
using Domain;
using MongoDB.Bson;

namespace Application.Services;

/// <summary>
/// Casos de uso de la Fase 3: gestión del ticket (asignar técnico, cambiar estado,
/// notas de seguimiento y horas invertidas).
/// Todo cambio se guarda con <see cref="IIncidenciaRepository.AplicarCambiosAsync"/>, que SIEMPRE
/// agrega su entrada de bitácora (armada con <see cref="BitacoraIncidencia"/>) en la misma operación.
/// </summary>
public class GestionIncidenciaService
{
    private readonly IIncidenciaRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IMapper _mapper;

    public GestionIncidenciaService(IIncidenciaRepository repository, IUsuarioRepository usuarioRepository, IMapper mapper)
    {
        _repository        = repository;
        _usuarioRepository = usuarioRepository;
        _mapper            = mapper;
    }

    // ─── PATCH /api/incidencias/{id}/asignar-tecnico ────────────────────────

    /// <summary>
    /// Asigna o reasigna el técnico responsable (Root, Soporte, Administrativos).
    ///   • Verifica en la BD que el usuario exista, tenga rol Técnico y su cuenta esté activa.
    ///   • Si la incidencia estaba "abierta", pasa automáticamente a "en proceso".
    ///   • Bitácora: asignación inicial o reasignación (+ el cambio automático de estado).
    /// </summary>
    public async Task<IncidenciaResponse> AsignarTecnicoAsync(string id, AsignarTecnicoRequest request, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (IncidenciaValidator.EstaCerrada(incidencia.Estado))
            throw AsignacionErrors.IncidenciaCerrada(incidencia.Estado);

        // Validación de rol en la base de datos
        if (!ObjectId.TryParse(request.IdTecnico, out _))
            throw AsignacionErrors.IdTecnicoInvalido();

        var tecnico = await _usuarioRepository.GetByIdAsync(request.IdTecnico)
            ?? throw AsignacionErrors.TecnicoNoExiste();

        if (!RolesSistema.Es(tecnico.RolSistema, RolesSistema.Tecnico))
            throw AsignacionErrors.NoEsTecnico(tecnico.RolSistema);

        if (tecnico.EstadoCuenta != "activo")
            throw AsignacionErrors.TecnicoInactivo(tecnico.EstadoCuenta);

        if (incidencia.TecnicoAsignado?.IdUsuario == tecnico.Id)
            throw AsignacionErrors.MismoTecnico(tecnico.NombreCompleto);

        // Transición automática: abierta → en proceso
        var estadoFinal = incidencia.Estado == EstadoIncidenciaValidator.Abierta
            ? EstadoIncidenciaValidator.EnProceso
            : incidencia.Estado;

        var bitacora = new List<SeguimientoIncidencia>
        {
            incidencia.TecnicoAsignado is null
                ? BitacoraIncidencia.Asignacion(usuario, tecnico.NombreCompleto, estadoFinal)
                : BitacoraIncidencia.Reasignacion(usuario, incidencia.TecnicoAsignado.Nombre, tecnico.NombreCompleto, estadoFinal)
        };
        if (estadoFinal != incidencia.Estado)
            bitacora.Add(BitacoraIncidencia.CambioEstado(usuario, incidencia.Estado, estadoFinal, "Cambio automático al asignar técnico."));

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            EstadoEsperado = incidencia.Estado,
            NuevoTecnico   = new UsuarioReferencia { IdUsuario = tecnico.Id, Nombre = tecnico.NombreCompleto, Rol = tecnico.RolSistema },
            NuevoEstado    = estadoFinal != incidencia.Estado ? estadoFinal : null,
            Bitacora       = bitacora
        });

        return _mapper.Map<IncidenciaResponse>(actualizada);
    }

    // ─── PATCH /api/incidencias/{id}/cambiar-estado ─────────────────────────

    /// <summary>
    /// Cambia el estado con su motivo.
    ///   • "resuelta": fechas.resolucion = ahora (inicia el conteo del índice TTL de 60 días).
    ///   • Reapertura desde "resuelta": fechas.resolucion = null (se detiene el TTL).
    ///   • Técnico: solo en sus incidencias asignadas y solo a "en proceso" o "resuelta".
    /// </summary>
    public async Task<IncidenciaResponse> CambiarEstadoAsync(string id, CambiarEstadoRequest request, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (usuario.EsRol(RolesSistema.Tecnico) && !IncidenciaValidator.EsTecnicoAsignado(incidencia, usuario))
            throw SeguimientoErrors.NoAsignado();

        var nuevo = EstadoIncidenciaValidator.ValidarEstado(request.Estado);
        EstadoIncidenciaValidator.ValidarTransicion(incidencia.Estado, nuevo, usuario);

        var resuelve = nuevo == EstadoIncidenciaValidator.Resuelta;
        var reabre   = incidencia.Estado == EstadoIncidenciaValidator.Resuelta;

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            EstadoEsperado    = incidencia.Estado,
            NuevoEstado       = nuevo,
            CambiarResolucion = resuelve || reabre,
            FechaResolucion   = resuelve ? DateTime.UtcNow : null,   // null = se desactiva el TTL
            Bitacora          = [BitacoraIncidencia.CambioEstado(usuario, incidencia.Estado, nuevo, request.Motivo.Trim())]
        });

        return _mapper.Map<IncidenciaResponse>(actualizada);
    }

    // ─── PATCH /api/incidencias/{id}/seguimiento ────────────────────────────

    /// <summary>Nota de texto libre del técnico asignado (o Root). No cambia el estado ni los datos del ticket.</summary>
    public async Task<SeguimientoResponse> AgregarNotaAsync(string id, NotaSeguimientoRequest request, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaParaTecnicoAsync(id, usuario);

        var nota = BitacoraIncidencia.Nota(usuario, request.Nota.Trim(), incidencia.Estado);
        await GuardarAsync(id, new CambiosIncidencia { Bitacora = [nota] });

        return _mapper.Map<SeguimientoResponse>(nota);
    }

    // ─── PATCH /api/incidencias/{id}/horas-invertidas ───────────────────────

    /// <summary>Suma horas con $inc (ej. 1.5 + 2.0 = 3.5) y lo registra en la bitácora.</summary>
    public async Task<HorasInvertidasResponse> RegistrarHorasAsync(string id, RegistrarHorasRequest request, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaParaTecnicoAsync(id, usuario);

        var entrada = BitacoraIncidencia.Horas(usuario, request.Horas, request.Comentario, incidencia.Estado);

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            HorasASumar      = request.Horas,
            InicializarHoras = incidencia.HorasInvertidas is null,
            Bitacora         = [entrada]
        });

        return new HorasInvertidasResponse
        {
            HorasRegistradas = request.Horas,
            HorasTotales     = actualizada.HorasInvertidas ?? request.Horas,
            Seguimiento      = _mapper.Map<SeguimientoResponse>(entrada)
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Incidencia> ObtenerIncidenciaAsync(string id)
    {
        IncidenciaValidator.ValidarId(id);
        return await _repository.GetByIdAsync(id)
            ?? throw IncidenciaErrors.NotFound();
    }

    /// <summary>Para notas y horas: Root o el técnico asignado, y la incidencia no debe estar cerrada.</summary>
    private async Task<Incidencia> ObtenerIncidenciaParaTecnicoAsync(string id, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (usuario.EsRol(RolesSistema.Tecnico) && !IncidenciaValidator.EsTecnicoAsignado(incidencia, usuario))
            throw SeguimientoErrors.NoAsignado();

        if (IncidenciaValidator.EstaCerrada(incidencia.Estado))
            throw SeguimientoErrors.IncidenciaCerrada(incidencia.Estado);

        return incidencia;
    }

    /// <summary>Aplica los cambios (con su bitácora). Si otra persona la cambió al mismo tiempo, avisa.</summary>
    private async Task<Incidencia> GuardarAsync(string id, CambiosIncidencia cambios)
        => await _repository.AplicarCambiosAsync(id, cambios)
           ?? throw IncidenciaErrors.CambioSimultaneo();
}
