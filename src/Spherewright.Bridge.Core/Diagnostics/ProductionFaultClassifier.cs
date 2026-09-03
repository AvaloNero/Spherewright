using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public sealed class ProductionFaultInput
{
    public int PlanetId { get; set; }

    public int ObjectId { get; set; }

    public int TargetItemId { get; set; }

    public string TargetItemName { get; set; } = string.Empty;

    public string WindowState { get; set; } = OverseerWindowStates.WarmingUp;

    public long WindowElapsedGameTicks { get; set; }

    public long ExpectedCycleGameTicks { get; set; }

    public double ActualProductionPerMinute { get; set; }

    public bool IsConfigured { get; set; }

    public bool IsWorking { get; set; }

    public int? PowerNetworkId { get; set; }

    public double? PowerServeRatio { get; set; }

    public bool IsResourceExtractor { get; set; }

    public bool ResourceStateKnown { get; set; }

    public long RemainingResourceAmount { get; set; }

    public IReadOnlyList<ProductionMaterialInput> Inputs { get; set; } = Array.Empty<ProductionMaterialInput>();

    public IReadOnlyList<ProductionOutputState> Outputs { get; set; } = Array.Empty<ProductionOutputState>();
}

public sealed class ProductionMaterialInput
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public long AvailableCount { get; set; }

    public long RequiredPerCycle { get; set; }

    public bool SourceResourceStateKnown { get; set; }

    public long SourceResourceRemaining { get; set; }

    public bool LogisticsExpected { get; set; }

    public bool LogisticsConfigured { get; set; }

    public bool LogisticsOrderOutstanding { get; set; }

    public bool LogisticsProgressObserved { get; set; }

    public bool SourceInventoryKnown { get; set; }

    public long SourceInventoryCount { get; set; }
}

public sealed class ProductionOutputState
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public long BufferedCount { get; set; }

    public long BufferCapacity { get; set; }
}

public static class ProductionFaultClassifier
{
    public const double DefaultMinimumPowerServeRatio = 0.999d;
    private const double NonZeroRateEpsilon = 0.000001d;

    public static OverseerFindingSnapshot? ClassifyPrimary(
        ProductionFaultInput input,
        double minimumPowerServeRatio = DefaultMinimumPowerServeRatio)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        Validate(input, minimumPowerServeRatio);

        if (!input.IsConfigured
            || !string.Equals(input.WindowState, OverseerWindowStates.Ready, StringComparison.Ordinal)
            || input.WindowElapsedGameTicks < input.ExpectedCycleGameTicks)
        {
            return null;
        }

        if (input.IsResourceExtractor && input.ResourceStateKnown && input.RemainingResourceAmount == 0)
        {
            return Finding(
                input,
                OverseerFindingKinds.VeinExhausted,
                OverseerFindingConfidences.Confirmed,
                "The bound resource extractor has no remaining resource amount.",
                Evidence("remaining_resource", input.RemainingResourceAmount, "items"));
        }

        if (!input.PowerNetworkId.HasValue
            || input.PowerNetworkId.Value <= 0
            || (input.PowerServeRatio.HasValue && input.PowerServeRatio.Value < minimumPowerServeRatio))
        {
            var evidence = new List<OverseerEvidenceSnapshot>
            {
                Evidence("power_network_id", input.PowerNetworkId ?? 0, null),
            };
            if (input.PowerServeRatio.HasValue)
            {
                evidence.Add(Evidence("power_serve_ratio", input.PowerServeRatio.Value, "ratio"));
            }

            return Finding(
                input,
                OverseerFindingKinds.InsufficientPower,
                OverseerFindingConfidences.Confirmed,
                "The production unit is disconnected from power or its network is undersupplying demand.",
                evidence.ToArray());
        }

        if (input.ActualProductionPerMinute > NonZeroRateEpsilon)
        {
            return null;
        }

        var blockedOutput = input.Outputs.FirstOrDefault(output =>
            output.BufferCapacity > 0 && output.BufferedCount >= output.BufferCapacity);
        if (blockedOutput is not null)
        {
            return Finding(
                input,
                OverseerFindingKinds.OutputBlocked,
                OverseerFindingConfidences.Confirmed,
                $"The output buffer for {DisplayName(blockedOutput.ItemName, blockedOutput.ItemId)} is full.",
                Evidence("output_buffer_count", blockedOutput.BufferedCount, "items"),
                Evidence("output_buffer_capacity", blockedOutput.BufferCapacity, "items"));
        }

        var missingInput = input.Inputs.FirstOrDefault(material =>
            material.RequiredPerCycle > 0 && material.AvailableCount < material.RequiredPerCycle);
        if (missingInput is null)
        {
            return null;
        }

        if (missingInput.SourceResourceStateKnown && missingInput.SourceResourceRemaining == 0)
        {
            return Finding(
                input,
                OverseerFindingKinds.VeinExhausted,
                OverseerFindingConfidences.Confirmed,
                $"The upstream resource for {DisplayName(missingInput.ItemName, missingInput.ItemId)} is exhausted.",
                Evidence("input_available", missingInput.AvailableCount, "items"),
                Evidence("upstream_resource_remaining", missingInput.SourceResourceRemaining, "items"));
        }

        if (missingInput.LogisticsExpected && !missingInput.LogisticsConfigured)
        {
            return Finding(
                input,
                OverseerFindingKinds.LogisticsBlocked,
                OverseerFindingConfidences.Confirmed,
                $"The expected logistics route for {DisplayName(missingInput.ItemName, missingInput.ItemId)} is not configured.",
                Evidence("input_available", missingInput.AvailableCount, "items"),
                TextEvidence("logistics_configured", "false"));
        }

        if (missingInput.LogisticsExpected
            && missingInput.LogisticsOrderOutstanding
            && !missingInput.LogisticsProgressObserved
            && missingInput.SourceInventoryKnown
            && missingInput.SourceInventoryCount > 0)
        {
            return Finding(
                input,
                OverseerFindingKinds.LogisticsBlocked,
                OverseerFindingConfidences.Suspected,
                $"A logistics order for {DisplayName(missingInput.ItemName, missingInput.ItemId)} made no progress while source inventory was available.",
                Evidence("source_inventory", missingInput.SourceInventoryCount, "items"),
                TextEvidence("logistics_order_outstanding", "true"),
                TextEvidence("logistics_progress_observed", "false"));
        }

        return Finding(
            input,
            OverseerFindingKinds.MaterialShortage,
            OverseerFindingConfidences.Confirmed,
            $"The production unit lacks one full cycle of {DisplayName(missingInput.ItemName, missingInput.ItemId)}.",
            Evidence("input_available", missingInput.AvailableCount, "items"),
            Evidence("input_required_per_cycle", missingInput.RequiredPerCycle, "items"));
    }

    private static void Validate(ProductionFaultInput input, double minimumPowerServeRatio)
    {
        if (input.PlanetId <= 0
            || input.ObjectId <= 0
            || input.TargetItemId <= 0
            || input.WindowElapsedGameTicks < 0
            || input.ExpectedCycleGameTicks <= 0
            || double.IsNaN(input.ActualProductionPerMinute)
            || double.IsInfinity(input.ActualProductionPerMinute)
            || input.ActualProductionPerMinute < 0d
            || double.IsNaN(minimumPowerServeRatio)
            || double.IsInfinity(minimumPowerServeRatio)
            || minimumPowerServeRatio <= 0d
            || minimumPowerServeRatio > 1d
            || (input.PowerServeRatio.HasValue
                && (double.IsNaN(input.PowerServeRatio.Value)
                    || double.IsInfinity(input.PowerServeRatio.Value)
                    || input.PowerServeRatio.Value < 0d)))
        {
            throw new ArgumentException("The production diagnostic input contains an invalid identity, window, rate, or power value.", nameof(input));
        }

        if (input.Inputs.Any(material =>
                material.ItemId <= 0
                || material.AvailableCount < 0
                || material.RequiredPerCycle < 0
                || material.SourceResourceRemaining < 0
                || material.SourceInventoryCount < 0)
            || input.Outputs.Any(output =>
                output.ItemId <= 0
                || output.BufferedCount < 0
                || output.BufferCapacity < 0))
        {
            throw new ArgumentException("Production material and output counts must be non-negative and use valid item identities.", nameof(input));
        }
    }

    private static OverseerFindingSnapshot Finding(
        ProductionFaultInput input,
        string kind,
        string confidence,
        string summary,
        params OverseerEvidenceSnapshot[] evidence)
    {
        return new OverseerFindingSnapshot
        {
            Kind = kind,
            Confidence = confidence,
            Severity = input.ActualProductionPerMinute <= NonZeroRateEpsilon
                ? OverseerFindingSeverities.Stopped
                : OverseerFindingSeverities.Warning,
            PlanetId = input.PlanetId,
            ObjectId = input.ObjectId,
            ItemId = input.TargetItemId,
            Summary = summary,
            Evidence = evidence.ToList(),
            UpstreamPath = new List<OverseerPathNodeSnapshot>
            {
                new OverseerPathNodeSnapshot
                {
                    PlanetId = input.PlanetId,
                    ObjectId = input.ObjectId,
                    ItemId = input.TargetItemId,
                    Kind = "production_unit",
                    Name = DisplayName(input.TargetItemName, input.TargetItemId),
                },
            },
        };
    }

    private static OverseerEvidenceSnapshot Evidence(string metric, double value, string? unit)
    {
        return new OverseerEvidenceSnapshot
        {
            Metric = metric,
            NumericValue = value,
            Unit = unit,
        };
    }

    private static OverseerEvidenceSnapshot TextEvidence(string metric, string value)
    {
        return new OverseerEvidenceSnapshot
        {
            Metric = metric,
            TextValue = value,
        };
    }

    private static string DisplayName(string name, int itemId)
    {
        return string.IsNullOrWhiteSpace(name) ? $"item {itemId}" : name;
    }
}
