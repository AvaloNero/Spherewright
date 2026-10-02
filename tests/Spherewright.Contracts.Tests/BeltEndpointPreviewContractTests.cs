using System.Text.Json;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class BeltEndpointPreviewContractTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void PreviewRequestRoundTripsExactBindingsWithoutChangingLegacyDefaults()
    {
        var request = new PrepareBuildRequest
        {
            PlanetId = 104,
            BuildingItemId = 2001,
            ExpectedPlayerStateHash = "player-state-hash",
            PreferredPosition = Point(1, 199.9925f, 2),
            PathEnd = Point(11, 199.686f, 2),
            BeltPathMode = BeltPathModes.NativeGrid,
            BeltEndpointPreview = new BeltEndpointPreviewRequest
            {
                SorterItemId = 2011,
                FilterItemId = 1109,
                Source = Binding(441, 3, 0, 2),
                Destination = Binding(884, -1, 0, 1),
            },
        };

        var json = JsonSerializer.Serialize(request, CamelCase);
        var restored = JsonSerializer.Deserialize<PrepareBuildRequest>(json, CamelCase)!;

        Assert.Equal(2011, restored.BeltEndpointPreview!.SorterItemId);
        Assert.Equal(1109, restored.BeltEndpointPreview.FilterItemId);
        Assert.Equal("player-state-hash", restored.ExpectedPlayerStateHash);
        AssertBinding(request.BeltEndpointPreview.Source!, restored.BeltEndpointPreview.Source!);
        AssertBinding(request.BeltEndpointPreview.Destination!, restored.BeltEndpointPreview.Destination!);
        Assert.Equal(BeltPathModes.NativeGrid, JsonSerializer.Deserialize<PrepareBuildRequest>("{}", CamelCase)!.BeltPathMode);
        Assert.Null(JsonSerializer.Deserialize<PrepareBuildRequest>("{}", CamelCase)!.BeltEndpointPreview);
    }

    [Fact]
    public void PreviewResultRoundTripsExplicitReadOnlyStatusAndNativeEvidence()
    {
        var result = new PreparedNormalAction
        {
            Prepared = false,
            ActionKind = NormalActionKinds.Build,
            PlanToken = string.Empty,
            ExpectedStateHash = "player-state-hash",
            BuildKind = "belt_endpoint_preview",
            BeltEndpointPreview = new BeltEndpointPreviewSnapshot
            {
                Phase = "read_only_belt_endpoint_preview",
                SessionId = "session-104",
                PlanetId = 104,
                Revision = 27,
                CapturedAtGameTick = 8123,
                NativeCheckPerformed = true,
                NativeCheckPassed = true,
                AssessmentHash = "assessment-hash",
                Attachments = new()
                {
                    Attachment("source", 441, "source-hash", 0, 3, -1),
                    Attachment("destination", 884, "destination-hash", 2, -1, -1),
                },
            },
        };

        var json = JsonSerializer.Serialize(result, CamelCase);
        var restored = JsonSerializer.Deserialize<PreparedNormalAction>(json, CamelCase)!;
        var preview = restored.BeltEndpointPreview!;

        Assert.False(restored.Prepared);
        Assert.Equal(string.Empty, restored.PlanToken);
        Assert.Equal("player-state-hash", restored.ExpectedStateHash);
        Assert.Equal("belt_endpoint_preview", restored.BuildKind);
        Assert.Equal("read_only_belt_endpoint_preview", preview.Phase);
        Assert.False(preview.Executable);
        Assert.Equal("session-104", preview.SessionId);
        Assert.Equal(104, preview.PlanetId);
        Assert.Equal(27, preview.Revision);
        Assert.Equal(8123, preview.CapturedAtGameTick);
        Assert.True(preview.NativeCheckPerformed);
        Assert.True(preview.NativeCheckPassed);
        Assert.Equal("assessment-hash", preview.AssessmentHash);
        Assert.Collection(preview.Attachments,
            source => Assert.Equal(("source", 441, "source-hash", 2101, 0, 3, -1),
                (source.Role, source.ExistingObjectId, source.EndpointStateHash, source.ExistingItemId, source.NativeSpan,
                    source.Attachment.SourceSlot, source.Attachment.DestinationSlot)),
            destination => Assert.Equal(("destination", 884, "destination-hash", 2001, 2, -1, -1),
                (destination.Role, destination.ExistingObjectId, destination.EndpointStateHash, destination.ExistingItemId, destination.NativeSpan,
                    destination.Attachment.SourceSlot, destination.Attachment.DestinationSlot)));
    }

    [Fact]
    public void LegacyPreparedActionWithoutPreviewRemainsDeserializable()
    {
        var restored = JsonSerializer.Deserialize<PreparedNormalAction>("{}", CamelCase)!;

        Assert.Null(restored.BeltEndpointPreview);
        Assert.False(restored.Prepared);
        Assert.Equal(string.Empty, restored.PlanToken);
    }

    private static PlannedBeltEndpointBinding Binding(int id, int slot, int existingTurns, int plannedTurns) => new()
    {
        ExistingObjectId = id,
        ExpectedEndpointStateHash = $"endpoint-{id}",
        ExistingSlot = slot,
        ExistingBeltQuarterTurns = existingTurns,
        PlannedBeltQuarterTurns = plannedTurns,
    };

    private static PlannedBeltEndpointAttachment Attachment(string role, int id, string hash, int span,
        int sourceSlot, int destinationSlot) => new()
    {
        Role = role,
        ExistingObjectId = id,
        EndpointStateHash = hash,
        ExistingItemId = (role == "source" ? sourceSlot : destinationSlot) == -1 ? 2001 : 2101,
        ObservedRecipeId = 58,
        PlannedBeltIndex = role == "source" ? 0 : 3,
        SorterItemId = 2011,
        FilterItemId = 1109,
        ExistingBeltQuarterTurns = 0,
        PlannedBeltQuarterTurns = role == "source" ? 2 : 1,
        NativeCondition = "Ok",
        NativeSpan = span,
        Attachment = new InserterAttachmentPlanSnapshot
        {
            Mode = "read_only_planned_belt_exact_slots",
            SourceSlot = sourceSlot,
            DestinationSlot = destinationSlot,
            InputOffset = 0,
            OutputOffset = 0,
            SourcePosition = Point(1, 199.9925f, 2),
            DestinationPosition = Point(11, 199.686f, 2),
        },
    };

    private static Vector3Snapshot Point(float x, float y, float z) => new() { X = x, Y = y, Z = z };

    private static void AssertBinding(PlannedBeltEndpointBinding expected, PlannedBeltEndpointBinding actual)
    {
        Assert.Equal(expected.ExistingObjectId, actual.ExistingObjectId);
        Assert.Equal(expected.ExpectedEndpointStateHash, actual.ExpectedEndpointStateHash);
        Assert.Equal(expected.ExistingSlot, actual.ExistingSlot);
        Assert.Equal(expected.ExistingBeltQuarterTurns, actual.ExistingBeltQuarterTurns);
        Assert.Equal(expected.PlannedBeltQuarterTurns, actual.PlannedBeltQuarterTurns);
    }
}
