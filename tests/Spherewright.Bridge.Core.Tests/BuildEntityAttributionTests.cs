using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BuildEntityAttributionTests
{
    [Fact]
    public void SelectNearestNewCandidate_ExcludesOlderEntityAtSameSorterPose()
    {
        var candidates = new[]
        {
            new BuildEntityCandidate(211, 0f),
            new BuildEntityCandidate(213, 0f),
        };

        var selected = BuildEntityAttribution.SelectNearestNewCandidate(
            candidates,
            new[] { 211 },
            Array.Empty<int>(),
            0.09f);

        Assert.Equal(213, selected);
    }

    [Fact]
    public void SelectNearestNewCandidate_ExcludesSourceBeltAtSameFirstSegmentPose()
    {
        var candidates = new[]
        {
            new BuildEntityCandidate(92, 0f),
            new BuildEntityCandidate(128, 0f),
        };

        var selected = BuildEntityAttribution.SelectNearestNewCandidate(
            candidates,
            new[] { 92 },
            Array.Empty<int>(),
            0.09f);

        Assert.Equal(128, selected);
    }

    [Fact]
    public void SelectNearestNewCandidate_ExcludesEntitiesAlreadyMappedToAnotherStep()
    {
        var candidates = new[]
        {
            new BuildEntityCandidate(301, 0.01f),
            new BuildEntityCandidate(302, 0.02f),
        };

        var selected = BuildEntityAttribution.SelectNearestNewCandidate(
            candidates,
            Array.Empty<int>(),
            new[] { 301 },
            0.09f);

        Assert.Equal(302, selected);
    }

    [Fact]
    public void SelectNearestNewCandidate_RejectsCandidateAtOrBeyondBound()
    {
        var selected = BuildEntityAttribution.SelectNearestNewCandidate(
            new[] { new BuildEntityCandidate(401, 0.09f) },
            Array.Empty<int>(),
            Array.Empty<int>(),
            0.09f);

        Assert.Equal(0, selected);
    }

    [Fact]
    public void TrySelectUniqueDirectedPath_FollowsNewBeltFromExistingSource()
    {
        var candidates = new IReadOnlyList<DirectedBuildEntityCandidate>[]
        {
            new[]
            {
                new DirectedBuildEntityCandidate(92, 91, 128, 0f),
                new DirectedBuildEntityCandidate(128, 92, 127, 0f),
            },
            new[] { new DirectedBuildEntityCandidate(127, 128, 126, 0f) },
            new[] { new DirectedBuildEntityCandidate(126, 127, 0, 0f) },
        };

        var proved = BuildEntityAttribution.TrySelectUniqueDirectedPath(
            candidates,
            Array.Empty<int>(),
            92,
            0,
            0.09f,
            out var selected);

        Assert.True(proved);
        Assert.Equal(new[] { 128, 127, 126 }, selected);
    }

    [Fact]
    public void TrySelectUniqueDirectedPath_RejectsAmbiguousTopology()
    {
        var candidates = new IReadOnlyList<DirectedBuildEntityCandidate>[]
        {
            new[]
            {
                new DirectedBuildEntityCandidate(10, 5, 20, 0f),
                new DirectedBuildEntityCandidate(11, 5, 21, 0f),
            },
            new[]
            {
                new DirectedBuildEntityCandidate(20, 10, 0, 0f),
                new DirectedBuildEntityCandidate(21, 11, 0, 0f),
            },
        };

        var proved = BuildEntityAttribution.TrySelectUniqueDirectedPath(
            candidates,
            Array.Empty<int>(),
            5,
            0,
            0.09f,
            out var selected);

        Assert.False(proved);
        Assert.Empty(selected);
    }
}
