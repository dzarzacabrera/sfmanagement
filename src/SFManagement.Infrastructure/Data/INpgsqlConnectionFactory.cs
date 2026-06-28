using Npgsql;

namespace SFManagement.Infrastructure.Data;

public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> GetOpenConnectionAsync();
}
