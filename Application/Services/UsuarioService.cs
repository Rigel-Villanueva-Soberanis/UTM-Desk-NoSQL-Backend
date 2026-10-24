using Application.Common;
using Application.Common.ErrorCatalog;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain;
using MongoDB.Bson;
using System.Globalization;
using System.Text;

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
    private readonly IMapper _mapper;

    public UsuarioService(IUsuarioRepository repository, ICatalogoRepository catalogoRepository, IMapper mapper)
    {
        _repository = repository;
        _catalogoRepository = catalogoRepository;
        _mapper = mapper;
    }

    // ─── CRUD (solo Soporte TI) ───────────────────────────────────────────────

    public async Task<IEnumerable<UsuarioResponse>> GetAllAsync()
    {
        var usuarios = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<UsuarioResponse>>(usuarios);
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
            Nombres          = request.Nombres.Trim(),
            ApellidoPaterno  = request.ApellidoPaterno.Trim(),
            ApellidoMaterno  = request.ApellidoMaterno.Trim(),
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

        usuario.Nombres          = request.Nombres.Trim();
        usuario.ApellidoPaterno  = request.ApellidoPaterno.Trim();
        usuario.ApellidoMaterno  = request.ApellidoMaterno.Trim();
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

        usuario.Nombres         = request.Nombres.Trim();
        usuario.ApellidoPaterno = request.ApellidoPaterno.Trim();
        usuario.ApellidoMaterno = request.ApellidoMaterno.Trim();
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
    /// Verifica que el rol y la clasificación enviados por el frontend existan en el catálogo
    /// <c>roles_sistema</c> y que la clasificación esté dentro de <c>clasificaciones_permitidas</c> de ese rol.
    /// La comparación ignora mayúsculas, espacios y acentos tanto en lo que manda el frontend
    /// como en lo que está guardado en la BD ("SOPORTE" == "Soporte", "tecnico" == "Técnico").
    /// Devuelve ambos valores normalizados en minúsculas (ej. "reportante", "estudiante").
    /// </summary>
    private async Task<(string Rol, string Clasificacion)> ValidarRolYClasificacionAsync(
        string rolRecibido, string clasificacionRecibida)
    {
        var rol           = (rolRecibido ?? string.Empty).Trim().ToLowerInvariant();
        var clasificacion = (clasificacionRecibida ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(rol))
            throw UsuarioErrors.RolNoExiste(rolRecibido ?? string.Empty);

        var catalogo = await _catalogoRepository.GetByClaveAsync(ClaveCatalogoRoles)
            ?? throw CatalogoErrors.ClaveNotFound(ClaveCatalogoRoles);

        foreach (var item in catalogo.Contenido)
        {
            // Cada elemento debe ser un objeto { rol: "Soporte", clasificaciones_permitidas: [...] }
            if (item is not BsonDocument doc || !doc.Contains("rol") || !doc["rol"].IsString)
                continue;

            var rolCatalogo = doc["rol"].AsString.Trim().ToLowerInvariant();
            if (QuitarAcentos(rolCatalogo) != QuitarAcentos(rol))
                continue;

            // Rol encontrado → validar la clasificación
            var permitidas = doc.Contains("clasificaciones_permitidas") && doc["clasificaciones_permitidas"].IsBsonArray
                ? doc["clasificaciones_permitidas"].AsBsonArray
                    .Where(c => c.IsString)
                    .Select(c => c.AsString.Trim().ToLowerInvariant())
                    .ToList()
                : new List<string>();

            // Si el rol no define clasificaciones permitidas, se acepta la enviada tal cual (en minúsculas)
            if (permitidas.Count == 0)
                return (rolCatalogo, clasificacion);

            if (string.IsNullOrWhiteSpace(clasificacion))
                throw UsuarioErrors.ClasificacionNoValida(clasificacionRecibida ?? string.Empty, rolCatalogo, permitidas);

            var clasificacionCatalogo = permitidas
                .FirstOrDefault(c => QuitarAcentos(c) == QuitarAcentos(clasificacion));

            if (clasificacionCatalogo is null)
                throw UsuarioErrors.ClasificacionNoValida(clasificacionRecibida!.Trim(), rolCatalogo, permitidas);

            return (rolCatalogo, clasificacionCatalogo);
        }

        throw UsuarioErrors.RolNoExiste(rolRecibido!.Trim());
    }

    /// <summary>Elimina acentos/diacríticos para comparar ("técnico" == "tecnico").</summary>
    private static string QuitarAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // (protección NoSQL centralizada en Application.Common.NoSqlSanitizer)
}
