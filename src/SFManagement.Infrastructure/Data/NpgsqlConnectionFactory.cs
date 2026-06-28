using Npgsql;

namespace SFManagement.Infrastructure.Data;

internal sealed class NpgsqlConnectionFactory(NpgsqlDataSource dataSource) : INpgsqlConnectionFactory
{
    public async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        var connection = dataSource.CreateConnection();
        await connection.OpenAsync();
        return connection;
    }
}
