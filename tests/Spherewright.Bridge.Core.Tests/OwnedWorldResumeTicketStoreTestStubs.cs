using Spherewright.Contracts.Journals;

namespace BepInEx.Logging
{
    // TEST-ONLY minimal logging surface for linked store tests.
    public sealed class ManualLogSource
    {
        public ManualLogSource(string sourceName = "owned-world-ticket-store-tests") { }

        public void LogInfo(object? message) { }
        public void LogWarning(object? message) { }
        public void LogError(object? message) { }
    }
}

namespace Spherewright.Plugin.RuntimeDescriptor
{
    // TEST-ONLY resolver: the tests always pass a synthetic absolute temp directory.
    internal static class RuntimeDescriptorPublisher
    {
        internal static string ResolveRuntimeDirectory(string configuredDirectory) =>
            Path.GetFullPath(configuredDirectory);
    }
}

namespace Spherewright.Plugin.Security
{
    // TEST-ONLY I/O substitute. It deliberately does not apply or prove Windows ACL behavior.
    internal static class WindowsCurrentUserSecurity
    {
        public static void EnsureSecureDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A directory is required.", nameof(path));
            Directory.CreateDirectory(path);
        }

        public static void WriteSecureNewFile(string path, byte[] content)
        {
            var directory = Path.GetDirectoryName(path)
                ?? throw new ArgumentException("A parent directory is required.", nameof(path));
            if (File.Exists(Path.Combine(directory, ".test-only-fail-secure-write")))
                throw new IOException("Test-only secure-write failure injection.");
            var secondWriteMarker = Path.Combine(directory, ".test-only-fail-second-secure-write");
            if (File.Exists(secondWriteMarker))
            {
                var countPath = Path.Combine(directory, ".test-only-secure-write-count");
                var count = File.Exists(countPath) && int.TryParse(File.ReadAllText(countPath), out var previous)
                    ? previous + 1
                    : 1;
                File.WriteAllText(countPath, count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (count >= 2) throw new IOException("Test-only second secure-write failure injection.");
            }

            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(content, 0, content.Length);
            stream.Flush(true);
        }
    }
}

namespace Spherewright.Plugin.Game
{
    // TEST-ONLY journal shape used by the linked ticket store.
    internal sealed class GameplayJournalDocument
    {
        public int Version { get; set; }
        public string JournalId { get; set; } = string.Empty;
        public string OwnedSaveIdentityHash { get; set; } = string.Empty;
        public string GameVersion { get; set; } = string.Empty;
        public List<GameplayJournalVersionTransition> VersionTransitions { get; set; } = new();
        public string TrackingMode { get; set; } = string.Empty;
        public bool HistoricalCoverageComplete { get; set; }
        public string CreatedAtActualTime { get; set; } = string.Empty;
        public long TrackingStartedAtGameTick { get; set; }
        public List<GameplayJournalEntry> Entries { get; set; } = new();
    }

}
