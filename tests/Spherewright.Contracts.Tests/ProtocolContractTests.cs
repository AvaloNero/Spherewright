using System.Text.Json;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class ProtocolContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void BridgeStatus_UsesStableCamelCaseContract()
    {
        var status = new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = "0.1.0",
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            GameVersion = "0.10.34.28529",
            GameLoaded = false,
            WritesConfigured = false,
            WriteHealth = WriteHealthStates.Healthy,
        };

        var json = JsonSerializer.Serialize(status, JsonOptions);

        Assert.Contains("\"bridgeConnected\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"protocolVersion\":1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("authToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pipeName", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ErrorCodes_HaveStableWireValues()
    {
        Assert.Equal("BRIDGE_NOT_READY", BridgeErrorCodes.BridgeNotReady);
        Assert.Equal("AUTH_FAILED", BridgeErrorCodes.AuthFailed);
        Assert.Equal("STALE_REVISION", BridgeErrorCodes.StaleRevision);
        Assert.Equal("ACTION_OUTCOME_UNKNOWN", BridgeErrorCodes.ActionOutcomeUnknown);
        Assert.Equal("SANDBOX_MODE_ACTIVE", BridgeErrorCodes.SandboxModeActive);
        Assert.Equal("confirmed_disabled", SandboxModeStates.ConfirmedDisabled);
    }

    [Fact]
    public void RequestEnvelope_DefaultsToCurrentProtocol()
    {
        var request = new BridgeRequestEnvelope<EmptyPayload>();

        Assert.Equal(ProtocolConstants.CurrentVersion, request.ProtocolVersion);
        Assert.Equal(BridgeMessageTypes.Request, request.MessageType);
    }

    [Fact]
    public void GameplayJournal_ExposesTimesWithoutRawSaveIdentity()
    {
        var journal = new GameplayJournalSnapshot
        {
            SessionId = "session",
            JournalId = "opaque-hash",
            CreatedAtActualTime = "2026-09-01T00:00:00+08:00",
            TrackingStartedAtGameTick = 120,
            TrackingStartedAtGameTime = "000d 00:00:02",
            DurableThroughSequence = 0,
            PersistencePending = true,
            PersistenceError = "IOException",
            Entries = new List<GameplayJournalEntry>
            {
                new GameplayJournalEntry
                {
                    Sequence = 1,
                    Kind = GameplayJournalEventKinds.ManualItemFirst,
                    ItemId = 1101,
                    ActualTime = "2026-09-01T00:00:01+08:00",
                    GameTick = 180,
                    GameTime = "000d 00:00:03",
                },
            },
        };

        var json = JsonSerializer.Serialize(journal, JsonOptions);
        using var parsed = JsonDocument.Parse(json);
        var entry = parsed.RootElement.GetProperty("entries")[0];

        Assert.Equal("2026-09-01T00:00:01+08:00", entry.GetProperty("actualTime").GetString());
        Assert.Equal(180, entry.GetProperty("gameTick").GetInt64());
        Assert.Equal(0, parsed.RootElement.GetProperty("durableThroughSequence").GetInt64());
        Assert.True(parsed.RootElement.GetProperty("persistencePending").GetBoolean());
        Assert.Equal("IOException", parsed.RootElement.GetProperty("persistenceError").GetString());
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
    }
}
