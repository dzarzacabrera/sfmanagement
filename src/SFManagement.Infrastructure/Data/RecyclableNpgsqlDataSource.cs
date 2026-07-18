using Npgsql;

namespace SFManagement.Infrastructure.Data;

internal sealed class RecyclableNpgsqlDataSource
{
    private volatile NpgsqlDataSource _current;

    public RecyclableNpgsqlDataSource(string connectionString)
    {
        ConnectionString = connectionString;
        _current = Build(connectionString);
    }

    public string ConnectionString { get; }
    public NpgsqlDataSource Current => _current;

    public void Recycle()
    {
        var old = _current;
        _current = Build(ConnectionString);
        old.Dispose();
    }

    private static NpgsqlDataSource Build(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        return builder.Build();
    }
}
