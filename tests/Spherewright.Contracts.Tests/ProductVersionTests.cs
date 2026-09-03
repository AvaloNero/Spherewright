using Spherewright.Contracts.Versioning;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class ProductVersionTests
{
    [Fact]
    public void CurrentVersion_MatchesContractsAssemblyVersion()
    {
        var assemblyVersion = typeof(SpherewrightProduct).Assembly.GetName().Version;

        Assert.NotNull(assemblyVersion);
        Assert.Equal(
            SpherewrightProduct.CurrentVersion,
            $"{assemblyVersion!.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}");
    }
}
