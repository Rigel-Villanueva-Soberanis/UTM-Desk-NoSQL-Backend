using Application.Common.Utilidades;
using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación MongoDB del repositorio de usuarios.
/// Usa la colección "usuarios" de la base de datos configurada en appsettings.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly IMongoCollection<Usuario> _collection;

    public UsuarioRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Usuario>("usuarios");
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
        => await _collection.Find(_ => true).ToListAsync();

    /// <summary>
    /// Listado paginado con filtros, ordenado por nombres y apellido paterno.
    /// Los textos se buscan como "contiene" sin importar mayúsculas ni acentos.
    /// </summary>
    public async Task<(IReadOnlyList<Usuario> Items, long Total)> GetPaginadoAsync(UsuarioConsulta consulta)
    {
        var filtro = ConstruirFiltro(consulta);

        var total = await _collection.CountDocumentsAsync(filtro);

        var items = await _collection.Find(filtro)
            .SortBy(u => u.Nombres).ThenBy(u => u.ApellidoPaterno)
            .Skip((consulta.Pagina - 1) * consulta.TamanoPagina)
            .Limit(consulta.TamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Técnicos con cuenta activa (para el selector de asignación). Ignora el rol y estado de la consulta.
    /// </summary>
    public async Task<IReadOnlyList<Usuario>> GetTecnicosActivosAsync(UsuarioConsulta consulta)
    {
        var filtro = ConstruirFiltro(consulta with { Rol = "tecnico", EstadoCuenta = "activo" });

        return await _collection.Find(filtro)
            .SortBy(u => u.Nombres).ThenBy(u => u.ApellidoPaterno)
            .ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(string id)
        => await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByUsernameAsync(string username)
        => await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByCorreoAsync(string correo)
        => await _collection.Find(u => u.Correo == correo).FirstOrDefaultAsync();

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        await _collection.InsertOneAsync(usuario);
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario)
        => await _collection.ReplaceOneAsync(u => u.Id == usuario.Id, usuario);

    public async Task DeleteAsync(string id)
        => await _collection.DeleteOneAsync(u => u.Id == id);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Arma el filtro de MongoDB a partir de la consulta (los valores ya vienen sanitizados).</summary>
    private static FilterDefinition<Usuario> ConstruirFiltro(UsuarioConsulta c)
    {
        var f      = Builders<Usuario>.Filter;
        var filtro = f.Empty;

        if (c.Busqueda is not null)
        {
            var r = Contiene(c.Busqueda);
            filtro &= f.Or(
                f.Regex(u => u.Nombres, r),
                f.Regex(u => u.ApellidoPaterno, r),
                f.Regex(u => u.ApellidoMaterno, r),
                f.Regex(u => u.Username, r),
                f.Regex(u => u.Correo, r));
        }

        if (c.Nombres is not null)
            filtro &= f.Regex(u => u.Nombres, Contiene(c.Nombres));

        if (c.Apellidos is not null)
        {
            var r = Contiene(c.Apellidos);
            filtro &= f.Or(f.Regex(u => u.ApellidoPaterno, r), f.Regex(u => u.ApellidoMaterno, r));
        }

        if (c.Rol           is not null) filtro &= f.Regex(u => u.RolSistema, Exacto(c.Rol));
        if (c.Clasificacion is not null) filtro &= f.Regex(u => u.ClasificacionRol, Exacto(c.Clasificacion));
        if (c.EstadoCuenta  is not null) filtro &= f.Eq(u => u.EstadoCuenta, c.EstadoCuenta);

        return filtro;
    }

    /// <summary>"Contiene", sin mayúsculas ni acentos. El texto se escapa: no se aceptan expresiones del usuario.</summary>
    private static BsonRegularExpression Contiene(string texto)
        => new(TextoNormalizer.PatronSinAcentos(texto), "i");

    /// <summary>Valor completo ("técnico" = "tecnico"), sin mayúsculas ni acentos.</summary>
    private static BsonRegularExpression Exacto(string texto)
        => new("^" + TextoNormalizer.PatronSinAcentos(texto) + "$", "i");
}
