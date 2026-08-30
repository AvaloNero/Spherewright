using BepInEx;

namespace Spherewright.Plugin.Game;

internal sealed class GameVersionSnapshotProvider
{
    private string _lastKnownVersion = "unknown";

    public string CaptureOnMainThread()
    {
        var version = GameConfig.gameVersion;
        var build = GameConfig.build;
        if (build > 0)
        {
            _lastKnownVersion = $"{version.Major}.{version.Minor}.{version.Release}.{build}";
            return _lastKnownVersion;
        }

        if (version.Build > 0)
        {
            _lastKnownVersion = version.ToFullString();
            return _lastKnownVersion;
        }

        var versionFromFile = TryReadLatestVersionFile(Paths.GameRootPath);
        if (!string.IsNullOrWhiteSpace(versionFromFile))
        {
            _lastKnownVersion = versionFromFile!;
        }
        else
        {
            _lastKnownVersion = version.ToString();
        }

        return _lastKnownVersion;
    }

    private static string? TryReadLatestVersionFile(string gameRoot)
    {
        try
        {
            var path = Path.Combine(gameRoot, "Updates", "Versions.txt");
            if (!File.Exists(path))
            {
                return null;
            }

            var last = File.ReadLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .LastOrDefault();
            return last?.Split(',')[0].Trim();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
