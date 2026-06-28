using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Infrastructure;
using Testcontainers.PostgreSql;

namespace SFManagement.E2ETests;

public sealed class SfManagementE2eFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("sfmanagement_e2e")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private WebApplication _app = null!;
    private string _connectionString = string.Empty;

    public string ServerUrl { get; private set; } = "http://localhost:5000";
    public HttpClient AnonymousClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();
        _connectionString = _pgContainer.GetConnectionString();
        await RunInitSqlAsync();

        var port = GetFreePort();

        var webProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SFManagement.Web"));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = webProjectPath,
            EnvironmentName = "Test"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString
        });

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services.AddControllersWithViews()
            .AddApplicationPart(typeof(Program).Assembly);
        builder.Services.AddInfrastructure(_connectionString);

        _app = builder.Build();

        _app.UseExceptionHandler("/Home/Error");
        _app.UseStaticFiles();
        _app.UseRouting();
        _app.UseAuthorization();
        _app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await _app.StartAsync();

        ServerUrl = $"http://127.0.0.1:{port}";
        AnonymousClient = new HttpClient { BaseAddress = new Uri(ServerUrl) };
    }

    public async Task DisposeAsync()
    {
        AnonymousClient.Dispose();
        await _app.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }

    public async Task<string> GetAsync(string path)
    {
        var response = await AnonymousClient.GetAsync(path);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> PostAsync(string path, Dictionary<string, string> formData)
    {
        var content = new FormUrlEncodedContent(formData);
        return await AnonymousClient.PostAsync(path, content);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async Task ResetDatabaseAsync()
    {
        var initSql = await File.ReadAllTextAsync(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "init.sql"));

        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var resetCmd = new Npgsql.NpgsqlCommand(@"
            TRUNCATE TABLE
                performance_evaluations,
                task_assignments,
                tasks,
                project_workers,
                projects,
                workers,
                skills_catalogue
            RESTART IDENTITY CASCADE;
        ", connection);
        await resetCmd.ExecuteNonQueryAsync();

        await using var cmd = new Npgsql.NpgsqlCommand(initSql, connection);
        await cmd.ExecuteNonQueryAsync();
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

[CollectionDefinition("E2E")]
public sealed class E2eCollection : ICollectionFixture<SfManagementE2eFixture>
{
}
