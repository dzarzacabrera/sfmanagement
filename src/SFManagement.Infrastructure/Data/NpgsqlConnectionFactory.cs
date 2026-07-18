using Npgsql;

namespace SFManagement.Infrastructure.Data;

internal sealed class NpgsqlConnectionFactory(RecyclableNpgsqlDataSource dataSource) : INpgsqlConnectionFactory
{
    public async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        var connection = dataSource.Current.CreateConnection();
        await connection.OpenAsync();
        return connection;
    }
}
