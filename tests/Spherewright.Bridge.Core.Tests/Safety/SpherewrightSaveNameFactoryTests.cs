using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests.Safety;

public sealed class SpherewrightSaveNameFactoryTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new DateTimeOffset(2026, 9, 4, 12, 34, 56, TimeSpan.Zero);

    private static readonly Guid Uniqueness =
        Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

    [Fact]
    public void CreateNewWorldName_UsesNewPrefixAndNeverLegacyM0Prefix()
    {
        var saveName = SpherewrightSaveNameFactory.CreateNewWorldName(CreatedAtUtc, Uniqueness);

        Assert.Equal(
            "Spherewright_New_20260904_123456_0123456789abcdef0123456789abcdef",
            saveName);
        Assert.False(saveName.StartsWith("Spherewright_M0_", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateImportedWorldName_KeepsImportedPrefix()
    {
        var saveName = SpherewrightSaveNameFactory.CreateImportedWorldName(CreatedAtUtc, Uniqueness);

        Assert.Equal(
            "Spherewright_Imported_20260904_123456_0123456789abcdef0123456789abcdef",
            saveName);
        Assert.StartsWith(SpherewrightSaveNameFactory.ImportedWorldPrefix, saveName);
    }
}
