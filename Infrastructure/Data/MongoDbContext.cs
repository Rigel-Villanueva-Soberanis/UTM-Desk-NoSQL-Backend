using MongoDB.Driver;

namespace Infrastructure.Data;

/// <summary>
/// Contexto de conexión a MongoDB.
/// Centraliza la creación del cliente y la referencia a la base de datos,
/// para ser inyectado como Singleton en los repositorios.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Obtiene la colección tipada de MongoDB equivalente a una tabla en SQL.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad del dominio.</typeparam>
    /// <param name="collectionName">Nombre de la colección en MongoDB.</param>
    public IMongoCollection<T> GetCollection<T>(string collectionName)
        => _database.GetCollection<T>(collectionName);
}
