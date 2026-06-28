using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace SFManagement.IntegrationTests;

public sealed class SfManagementFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("sfmanagement_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private string _connectionString = string.Empty;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();
        _connectionString = _pgContainer.GetConnectionString();

        await RunInitSqlAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseConfiguration(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                })
                .Build());
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }

    private async Task RunInitSqlAsync()
    {
        var initSql = await File.ReadAllTextAsync(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "init.sql"));

        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new Npgsql.NpgsqlCommand(initSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }
}
