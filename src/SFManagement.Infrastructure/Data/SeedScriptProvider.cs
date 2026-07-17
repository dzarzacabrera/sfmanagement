using System.Reflection;

namespace SFManagement.Infrastructure.Data;

internal static class SeedScriptProvider
{
    internal static string GetInitSql()
    {
        const string resourceName = "SFManagement.Infrastructure.Seed.init.sql";
        var assembly = typeof(SeedScriptProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded seed script 'init.sql' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
