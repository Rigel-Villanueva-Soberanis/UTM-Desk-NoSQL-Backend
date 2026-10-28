using Application.Common.ErrorsCatalogos;
using Application.Common.ErrorsUsuarios;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Usuarios;
using Domain;
using AutoMapper;
using MongoDB.Bson;

namespace Application.Services;

/// <summary>
/// Casos de uso para la gestión de usuarios del sistema.
/// </summary>
public class UsuarioService
{
    /// <summary>Clave del catálogo que contiene los roles válidos del sistema.</summary>
    private const string ClaveCatalogoRoles = "roles_sistema";

    private readonly IUsuarioRepository _repository;
    private readonly ICatalogoRepository _catalogoRepository;
    private readonly IIncidenciaRepository _incidenciaRepository;
    private readonly IMapper _mapper;

    public UsuarioService(
        IUsuarioRepository repository,
        ICatalogoRepository catalogoRepository,
        IIncidenciaRepository incidenciaRepository,
        IMapper mapper)
    {
        _repository = repository;
        _catalogoRepository = catalogoRepository;
        _incidenciaRepository = incidenciaRepository;
        _mapper = mapper;
    }

    // ─── CRUD (Root y Soporte TI) ───────────────────────────────────────────────

    /// <summary>
    /// Listado paginado de cuentas con filtros (nombres, apellidos, rol, clasificación, estado).
    /// </summary>
    public async Task<PaginadoResponse<UsuarioResponse>> GetAllAsync(UsuarioFiltroRequest filtro)
    {
        var consulta = UsuarioValidator.ValidarFiltros(filtro);

        var (items, total) = await _repository.GetPaginadoAsync(consulta);

        return new PaginadoResponse<UsuarioResponse>
        {
            Items        = _mapper.Map<List<UsuarioResponse>>(items),
            Pagina       = consulta.Pagina,
            TamanoPagina = consulta.TamanoPagina,
            Total        = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)consulta.TamanoPagina)
        };
    }

    // ─── Selector de técnicos (Root, Soporte, Administrativos) ───────────────

    /// <summary>
    /// Técnicos activos con su carga de trabajo (incidencias no resueltas, cerradas ni canceladas).
    /// Ordenados de menor a mayor carga para facilitar la asignación.
    /// </summary>
    public async Task<List<TecnicoDisponibleResponse>> GetTecnicosAsync(TecnicoFiltroRequest filtro)
    {
        var consulta = UsuarioValidator.ValidarFiltrosTecnicos(filtro);

        var tecnicos = await _repository.GetTecnicosActivosAsync(consulta);
        var carga    = await _incidenciaRepository.ContarActivasPorTecnicoAsync(tecnicos.Select(t => t.Id));

        return tecnicos
            .Select(t => new TecnicoDisponibleResponse
            {
                Id                 = t.Id,
                NombreCompleto     = t.NombreCompleto,
                Clasificacion      = string.IsNullOrWhiteSpace(t.ClasificacionRol) ? null : t.ClasificacionRol,
                IncidenciasActivas = carga.GetValueOrDefault(t.Id)
            })
            .OrderBy(t => t.IncidenciasActivas)
            .ThenBy(t => t.NombreCompleto)
            .ToList();
    }

    public async Task<UsuarioResponse> GetByIdAsync(string id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw UsuarioErrors.NotFound();
        return _mapper.Map<UsuarioResponse>(usuario);
    }

    /// <summary>
    /// Crea un nuevo usuario. Valida correo único, username único y encripta la contraseña.
    /// Protegido contra inyección NoSQL: todos los campos son literales, nunca operadores.
    /// </summary>
    public async Task<UsuarioResponse> CreateAsync(CreateUsuarioRequest request)
    {
        // Segunda línea de defensa anti-inyección NoSQL (la primera es el Driver LINQ)
        NoSqlSanitizer.ValidarTodos(
            (request.Correo,           "correo"),
            (request.Usuario,          "usuario"),
            (request.Nombres,          "nombres"),
            (request.ApellidoPaterno,  "apellidoPaterno"),
            (request.ApellidoMaterno,  "apellidoMaterno"),
            (request.RolSistema,       "rolSistema"),
            (request.ClasificacionRol, "clasificacionRol")
        );

        // Validar correo único
        var existeCorreo = await _repository.GetByCorreoAsync(request.Correo.ToLowerInvariant());
        if (existeCorreo is not null)
            throw UsuarioErrors.CorreoRepetido(request.Correo);

        // Validar username único
        var existeUsername = await _repository.GetByUsernameAsync(request.Usuario.ToLowerInvariant());
        if (existeUsername is not null)
            throw UsuarioErrors.UsernameRepetido(request.Usuario);

        // Validar rol y clasificación contra el catálogo roles_sistema (se guardan en minúsculas)
        var (rolValido, clasificacionValida) = await ValidarRolYClasificacionAsync(request.RolSistema, request.ClasificacionRol);

        var usuario = new Usuario
        {
            Nombres          = TextoSeguro.Limpiar(request.Nombres),
            ApellidoPaterno  = TextoSeguro.Limpiar(request.ApellidoPaterno),
            ApellidoMaterno  = TextoSeguro.Limpiar(request.ApellidoMaterno),
            Username         = request.Usuario.Trim().ToLowerInvariant(),
            Correo           = request.Correo.Trim().ToLowerInvariant(),
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            EstadoCuenta     = "activo",
            RolSistema       = rolValido,
            ClasificacionRol = clasificacionValida,
            FechaCreacion    = DateTime.UtcNow
        };

        var creado = await _repository.CreateAsync(usuario);
        return _mapper.Map<UsuarioResponse>(creado);
    }

    /// <summary>
    /// Actualización completa por Soporte TI. No cambia usuario ni contraseña.
    /// </summary>
    public async Task UpdateAsync(string id, UpdateUsuarioRequest request)
    {
        NoSqlSanitizer.ValidarTodos(
            (request.Correo,           "correo"),
            (request.Nombres,          "nombres"),
            (request.ApellidoPaterno,  "apellidoPaterno"),
            (request.ApellidoMaterno,  "apellidoMaterno"),
            (request.RolSistema,       "rolSistema"),
            (request.ClasificacionRol, "clasificacionRol")
        );

        var usuario = await _repository.GetByIdAsync(id)
            ?? throw UsuarioErrors.NotFound();

        // Validar correo único si cambió
        if (!usuario.Correo.Equals(request.Correo.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            var existeCorreo = await _repository.GetByCorreoAsync(request.Correo.ToLowerInvariant());
            if (existeCorreo is not null)
                throw UsuarioErrors.CorreoRepetido(request.Correo);
        }

        // Validar rol y clasificación contra el catálogo roles_sistema (se guardan en minúsculas)
        var (rolValido, clasificacionValida) = await ValidarRolYClasificacionAsync(request.RolSistema, request.ClasificacionRol);

        usuario.Nombres          = TextoSeguro.Limpiar(request.Nombres);
        usuario.ApellidoPaterno  = TextoSeguro.Limpiar(request.ApellidoPaterno);
        usuario.ApellidoMaterno  = TextoSeguro.Limpiar(request.ApellidoMaterno);
        usuario.Correo           = request.Correo.Trim().ToLowerInvariant();
        usuario.EstadoCuenta     = request.EstadoCuenta;
        usuario.RolSistema       = rolValido;
        usuario.ClasificacionRol = clasificacionValida;

        await _repository.UpdateAsync(usuario);
    }

    /// <summary>
    /// Cambia la contraseña de cualquier usuario. Solo Soporte TI.
    /// </summary>
    public async Task AdminCambiarPasswordAsync(string id, AdminCambiarPasswordRequest request)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw UsuarioErrors.NotFound();

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword, 12);
        await _repository.UpdateAsync(usuario);
    }

    /// <summary>
    /// Elimina permanentemente un usuario. Solo Soporte TI.
    /// </summary>
    public async Task DeleteAsync(string id)
    {
        _ = await _repository.GetByIdAsync(id)
            ?? throw UsuarioErrors.NotFound();
        await _repository.DeleteAsync(id);
    }

    // ─── Endpoints de perfil propio ───────────────────────────────────────────

    /// <summary>
    /// Datos de la cuenta del usuario autenticado (el id sale del token, nunca de la URL).
    /// </summary>
    public async Task<UsuarioResponse> GetMiPerfilAsync(string idUsuarioActual)
    {
        var usuario = await _repository.GetByIdAsync(idUsuarioActual)
            ?? throw UsuarioErrors.NotFound();
        return _mapper.Map<UsuarioResponse>(usuario);
    }

    /// <summary>
    /// Permite que un usuario edite su propio nombre y correo.
    /// No puede cambiar: usuario, contraseña, rol ni estado de cuenta.
    /// </summary>
    public async Task<UsuarioResponse> UpdateMiPerfilAsync(string idUsuarioActual, UpdateMiPerfilRequest request)
    {
        NoSqlSanitizer.ValidarTodos(
            (request.Correo,          "correo"),
            (request.Nombres,         "nombres"),
            (request.ApellidoPaterno, "apellidoPaterno"),
            (request.ApellidoMaterno, "apellidoMaterno")
        );

        var usuario = await _repository.GetByIdAsync(idUsuarioActual)
            ?? throw UsuarioErrors.NotFound();

        // Validar correo único si cambió
        if (!usuario.Correo.Equals(request.Correo.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            var existeCorreo = await _repository.GetByCorreoAsync(request.Correo.ToLowerInvariant());
            if (existeCorreo is not null)
                throw UsuarioErrors.CorreoRepetido(request.Correo);
        }

        usuario.Nombres         = TextoSeguro.Limpiar(request.Nombres);
        usuario.ApellidoPaterno = TextoSeguro.Limpiar(request.ApellidoPaterno);
        usuario.ApellidoMaterno = TextoSeguro.Limpiar(request.ApellidoMaterno);
        usuario.Correo          = request.Correo.Trim().ToLowerInvariant();

        await _repository.UpdateAsync(usuario);
        return _mapper.Map<UsuarioResponse>(usuario);
    }

    /// <summary>
    /// Permite que un usuario cambie su propia contraseña.
    /// Requiere la contraseña actual para confirmar identidad.
    /// </summary>
    public async Task CambiarMiPasswordAsync(string idUsuarioActual, CambiarMiPasswordRequest request)
    {
        var usuario = await _repository.GetByIdAsync(idUsuarioActual)
            ?? throw UsuarioErrors.NotFound();

        // Verificar contraseña actual
        if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.PasswordHash))
            throw UsuarioErrors.PasswordIncorrecta();

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword, 12);
        await _repository.UpdateAsync(usuario);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica el rol y la clasificación (subrol) contra el catálogo <c>roles_sistema</c>.
    /// La comparación ignora mayúsculas, espacios y acentos (en lo del frontend y en lo de la BD).
    /// Reglas por rol según el catálogo:
    ///   • requiere_clasificacion = true  → la clasificación es obligatoria y debe estar en clasificaciones_permitidas.
    ///   • requiere_clasificacion = false → es opcional; si se envía, debe estar en clasificaciones_permitidas.
    ///   • clasificaciones_permitidas vacía → el rol no maneja clasificación (se guarda vacía).
    /// Devuelve ambos valores en minúsculas (ej. "reportante", "docente").
    /// </summary>
    private async Task<(string Rol, string Clasificacion)> ValidarRolYClasificacionAsync(
        string rolRecibido, string? clasificacionRecibida)
    {
        var rol           = TextoNormalizer.Normalizar(rolRecibido);
        var clasificacion = TextoNormalizer.Normalizar(clasificacionRecibida);

        if (string.IsNullOrWhiteSpace(rol))
            throw UsuarioErrors.RolNoExiste(rolRecibido ?? string.Empty);

        var catalogo = await _catalogoRepository.GetByClaveAsync(ClaveCatalogoRoles)
            ?? throw CatalogoErrors.ClaveNotFound(ClaveCatalogoRoles);

        if (!catalogo.Contenido.IsBsonArray)
            throw CatalogoErrors.EstructuraInvalida(ClaveCatalogoRoles);

        foreach (var item in catalogo.Contenido.AsBsonArray)
        {
            // Cada elemento: { rol, requiere_clasificacion, clasificaciones_permitidas: [...] }
            if (item is not BsonDocument doc || !doc.Contains("rol") || !doc["rol"].IsString)
                continue;

            var rolCatalogo = TextoNormalizer.Normalizar(doc["rol"].AsString);
            if (!TextoNormalizer.Iguales(rolCatalogo, rol))
                continue;

            // Rol encontrado → validar la clasificación
            var requiere = doc.Contains("requiere_clasificacion")
                && doc["requiere_clasificacion"].IsBoolean
                && doc["requiere_clasificacion"].AsBoolean;

            var permitidas = doc.Contains("clasificaciones_permitidas") && doc["clasificaciones_permitidas"].IsBsonArray
                ? doc["clasificaciones_permitidas"].AsBsonArray
                    .Where(c => c.IsString)
                    .Select(c => TextoNormalizer.Normalizar(c.AsString))
                    .ToList()
                : new List<string>();

            if (string.IsNullOrWhiteSpace(clasificacion))
            {
                if (requiere)
                    throw UsuarioErrors.ClasificacionRequerida(rolCatalogo, permitidas);
                return (rolCatalogo, string.Empty);
            }

            if (permitidas.Count == 0)
                throw UsuarioErrors.ClasificacionNoAplica(rolCatalogo);

            var clasificacionCatalogo = permitidas.FirstOrDefault(c => TextoNormalizer.Iguales(c, clasificacion));
            if (clasificacionCatalogo is null)
                throw UsuarioErrors.ClasificacionNoValida(clasificacionRecibida!.Trim(), rolCatalogo, permitidas);

            return (rolCatalogo, clasificacionCatalogo);
        }

        throw UsuarioErrors.RolNoExiste(rolRecibido!.Trim());
    }

    // (protección NoSQL centralizada en Application.Common.NoSqlSanitizer)
}
