using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data;

/// <summary>
/// Fábrica de conexiones a la base de datos.
/// Centraliza la creación de IDbConnection para ser inyectada en los repositorios.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
