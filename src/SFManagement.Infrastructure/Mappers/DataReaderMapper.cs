using Npgsql;

namespace SFManagement.Infrastructure.Mappers;

internal sealed class DataReaderMapper(NpgsqlDataReader reader)
{
    private readonly NpgsqlDataReader _reader = reader;

    public int GetInt32(string column) =>
        _reader.GetFieldValue<int>(_reader.GetOrdinal(column));

    public string GetString(string column) =>
        _reader.GetFieldValue<string>(_reader.GetOrdinal(column));

    public string? GetStringOrNull(string column)
    {
        var ordinal = _reader.GetOrdinal(column);
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<string>(ordinal);
    }

    public float[] GetVector(string column)
    {
        var vector = _reader.GetFieldValue<Pgvector.Vector>(_reader.GetOrdinal(column));
        return vector.ToArray();
    }

    public DateTime GetDateTime(string column) =>
        _reader.GetFieldValue<DateTime>(_reader.GetOrdinal(column));

    public int? GetInt32OrNull(string column)
    {
        var ordinal = _reader.GetOrdinal(column);
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<int>(ordinal);
    }

    public TEnum GetEnum<TEnum>(string column) where TEnum : struct, Enum
    {
        var raw = _reader.GetFieldValue<string>(_reader.GetOrdinal(column));
        return Enum.Parse<TEnum>(raw, ignoreCase: true);
    }

    public double GetDouble(string column) =>
        _reader.GetFieldValue<double>(_reader.GetOrdinal(column));
}
