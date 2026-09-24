using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Journals;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldVersionCompatibilityPolicyTests
{
    private const long EntryCount = 91;
    private const long AdoptedTick = 73573789;
    private const string RecordedAt = "2026-09-24T00:48:28.0000000+00:00";

    [Fact]
    public void OnlyTheResearchedSourceToEitherExactTargetIsASupportedMigration()
    {
        Assert.True(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(Source, Target));
        Assert.True(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(Source, TargetPatch));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(Target, Source));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(TargetPatch, Source));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(Target, TargetPatch));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(TargetPatch, Target));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.IsSupportedMigration(Source, "0.10.35.29058"));
    }

    [Theory]
    [InlineData("0.10.34.28529", "0.10.34.28529", true)]
    [InlineData("0.10.35.29057", "0.10.35.29057", true)]
    [InlineData("0.10.35.29088", "0.10.35.29088", true)]
    [InlineData("0.10.34.28529", "0.10.35.29057", true)]
    [InlineData("0.10.34.28529", "0.10.35.29088", true)]
    [InlineData("0.10.35.29057", "0.10.34.28529", false)]
    [InlineData("0.10.35.29088", "0.10.34.28529", false)]
    [InlineData("0.10.35.29057", "0.10.35.29088", false)]
    [InlineData("0.10.35.29088", "0.10.35.29057", false)]
    [InlineData("0.10.33.00000", "0.10.35.29057", false)]
    [InlineData("0.10.34.28529", "0.10.36.00000", false)]
    [InlineData("", "0.10.35.29057", false)]
    public void ReauthorizationAllowsOnlySameVersionOrTheExactSupportedPair(
        string source, string target, bool allowed) =>
        Assert.Equal(allowed, OwnedWorldVersionCompatibilityPolicy.AllowsReauthorization(source, target));

    [Fact]
    public void NullVersionsFailClosed()
    {
        Assert.False(OwnedWorldVersionCompatibilityPolicy.AllowsReauthorization(null!, Target));
        Assert.False(OwnedWorldVersionCompatibilityPolicy.AllowsReauthorization(Source, null!));
    }

    [Fact]
    public void SameVersionJournalNeedsNoTransition()
    {
        Assert.True(OwnedWorldVersionCompatibilityPolicy.JournalMatches(
            Source, Array.Empty<GameplayJournalVersionTransition>(), EntryCount, Source));
    }

    [Fact]
    public void ExactDurableTransitionAllowsFutureNormalResumeOnTheTarget()
    {
        Assert.True(OwnedWorldVersionCompatibilityPolicy.JournalMatches(
            Source, new[] { ValidTransition() }, EntryCount, Target));
    }

    [Fact]
    public void ExactDurableTransitionAllowsFutureNormalResumeOnThePatchTarget()
    {
        Assert.True(OwnedWorldVersionCompatibilityPolicy.JournalMatches(
            Source, new[] { ValidTransition(to: TargetPatch) }, EntryCount, TargetPatch));
    }

    [Theory]
    [InlineData("null-transitions")]
    [InlineData("duplicate-transitions")]
    [InlineData("malformed-recorded-at")]
    [InlineData("future-sequence")]
    [InlineData("negative-adopted-tick")]
    [InlineData("wrong-source")]
    [InlineData("wrong-target")]
    [InlineData("reverse")]
    [InlineData("cross-researched-targets")]
    [InlineData("unexpected-expected-version")]
    public void InvalidOrNonDirectionalJournalTransitionsFailClosed(string change)
    {
        IReadOnlyList<GameplayJournalVersionTransition>? transitions = new[] { ValidTransition() };
        var expected = Target;

        switch (change)
        {
            case "null-transitions": transitions = null; break;
            case "duplicate-transitions": transitions = new[] { ValidTransition(), ValidTransition() }; break;
            case "malformed-recorded-at": transitions = new[] { ValidTransition(recordedAt: "not-a-date") }; break;
            case "future-sequence": transitions = new[] { ValidTransition(sequence: EntryCount + 1) }; break;
            case "negative-adopted-tick": transitions = new[] { ValidTransition(adoptedAt: -1) }; break;
            case "wrong-source": transitions = new[] { ValidTransition(from: "0.10.33.00000") }; break;
            case "wrong-target": transitions = new[] { ValidTransition(to: "0.10.35.29058") }; break;
            case "reverse": transitions = new[] { ValidTransition(from: Target, to: Source) }; break;
            case "cross-researched-targets":
                transitions = new[] { ValidTransition(from: Target, to: TargetPatch) };
                expected = TargetPatch;
                break;
            case "unexpected-expected-version": expected = Source; break;
        }

        Assert.False(OwnedWorldVersionCompatibilityPolicy.JournalMatches(Source, transitions, EntryCount, expected));
    }

    [Fact]
    public void NullTransitionEntryFailsClosed()
    {
        IReadOnlyList<GameplayJournalVersionTransition> transitions = new GameplayJournalVersionTransition[] { null! };

        Assert.False(OwnedWorldVersionCompatibilityPolicy.JournalMatches(Source, transitions, EntryCount, Target));
    }

    private const string Source = OwnedWorldVersionCompatibilityPolicy.SourceVersion;
    private const string Target = OwnedWorldVersionCompatibilityPolicy.TargetVersion;
    private const string TargetPatch = OwnedWorldVersionCompatibilityPolicy.TargetPatchVersion;

    private static GameplayJournalVersionTransition ValidTransition(
        string from = Source,
        string to = Target,
        long adoptedAt = AdoptedTick,
        long sequence = EntryCount,
        string recordedAt = RecordedAt) => new()
    {
        FromGameVersion = from,
        ToGameVersion = to,
        AdoptedAtGameTick = adoptedAt,
        DurableThroughSequence = sequence,
        RecordedAtUtc = recordedAt,
    };
}
