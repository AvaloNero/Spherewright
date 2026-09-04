namespace Spherewright.Bridge.Core.Safety;

public static class SpherewrightSaveNameFactory
{
    public const string NewWorldPrefix = "Spherewright_New_";

    public const string ImportedWorldPrefix = "Spherewright_Imported_";

    public static string CreateNewWorldName(DateTimeOffset createdAtUtc, Guid uniqueness) =>
        Create(NewWorldPrefix, createdAtUtc, uniqueness);

    public static string CreateImportedWorldName(DateTimeOffset createdAtUtc, Guid uniqueness) =>
        Create(ImportedWorldPrefix, createdAtUtc, uniqueness);

    private static string Create(string prefix, DateTimeOffset createdAtUtc, Guid uniqueness) =>
        $"{prefix}{createdAtUtc.UtcDateTime:yyyyMMdd_HHmmss}_{uniqueness:N}";
}
