using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class ImportSeedDataCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<ImportSeedDataCommand>
{
    public async Task HandleAsync(ImportSeedDataCommand command)
    {
        var initSql = SeedScriptProvider.GetInitSql();

        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var seedCmd = new NpgsqlCommand(initSql, connection);
        await seedCmd.ExecuteNonQueryAsync();
    }
}
