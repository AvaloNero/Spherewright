using System.Text.Json;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class TechCompletionRewardSerializationTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"completionItemRewards\":null}")]
    public void LegacyOrUnknownMetadataIsNotAnEmptyRewardList(string json)
    {
        Assert.Null(JsonSerializer.Deserialize<TechStateSnapshot>(json, Options)!.CompletionItemRewards);
    }

    [Fact]
    public void ObservedEmptyListSurvivesRoundtrip()
    {
        var state = new TechStateSnapshot { CompletionItemRewards = new() };
        var json = JsonSerializer.Serialize(state, Options);
        Assert.Contains("\"completionItemRewards\":[]", json);
        Assert.Empty(JsonSerializer.Deserialize<TechStateSnapshot>(json, Options)!.CompletionItemRewards!);
    }

    [Fact]
    public void ExactRewardMetadataSurvivesProgressionRoundtripWithoutClaimingDelivery()
    {
        var state = new ProgressionStateSnapshot { Technologies = new()
        {
            new TechStateSnapshot { TechId = 1501, Unlocked = false, CompletionItemRewards = new()
            {
                new TechCompletionItemReward { ItemId = 2205, Name = "Solar panel", Count = 1 },
            } },
        } };
        var json = JsonSerializer.Serialize(state, Options);
        var tech = Assert.Single(JsonSerializer.Deserialize<ProgressionStateSnapshot>(json, Options)!.Technologies);
        var reward = Assert.Single(tech.CompletionItemRewards!);
        Assert.False(tech.Unlocked);
        Assert.Equal(2205, reward.ItemId);
        Assert.Equal("Solar panel", reward.Name);
        Assert.Equal(1, reward.Count);
        Assert.DoesNotContain("delivered", json, StringComparison.OrdinalIgnoreCase);
    }
}
