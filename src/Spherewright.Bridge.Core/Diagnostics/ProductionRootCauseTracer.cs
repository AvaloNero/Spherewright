using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public static class ProductionRootCauseTracer
{
    public const int DefaultMaximumDepth = 8;
    public const int DefaultMaximumVisitedProducers = 64;

    public static OverseerFindingSnapshot? TracePrimary(
        ProductionFaultInput input,
        Func<ProductionUpstreamReference, ProductionFaultInput?> resolveProducer,
        int maximumDepth = DefaultMaximumDepth,
        int maximumVisitedProducers = DefaultMaximumVisitedProducers)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (resolveProducer is null)
        {
            throw new ArgumentNullException(nameof(resolveProducer));
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        }

        if (maximumVisitedProducers <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisitedProducers));
        }

        var visited = new HashSet<string>(StringComparer.Ordinal)
        {
            ProducerKey(input.PlanetId, input.ObjectId, input.TargetItemId),
        };
        return Trace(
            input,
            resolveProducer,
            visited,
            0,
            maximumDepth,
            maximumVisitedProducers);
    }

    private static OverseerFindingSnapshot? Trace(
        ProductionFaultInput input,
        Func<ProductionUpstreamReference, ProductionFaultInput?> resolveProducer,
        ISet<string> visited,
        int depth,
        int maximumDepth,
        int maximumVisitedProducers)
    {
        var finding = ProductionFaultClassifier.ClassifyPrimary(input);
        if (finding is null)
        {
            return null;
        }

        if (!string.Equals(
                finding.Kind,
                OverseerFindingKinds.MaterialShortage,
                StringComparison.Ordinal))
        {
            return finding;
        }

        var missingMaterial = input.Inputs.FirstOrDefault(material =>
            material.RequiredPerCycle > 0
            && material.AvailableCount < material.RequiredPerCycle);
        if (missingMaterial is null || missingMaterial.UpstreamProducers.Count == 0)
        {
            return finding;
        }

        if (depth >= maximumDepth)
        {
            AddTraceStopReason(finding, "maximum_depth");
            return finding;
        }

        var cycleDetected = false;
        var unresolvedReference = false;
        foreach (var reference in missingMaterial.UpstreamProducers
                     .OrderBy(candidate => candidate.PlanetId)
                     .ThenBy(candidate => candidate.ObjectId)
                     .ThenBy(candidate => candidate.ItemId))
        {
            if (visited.Count >= maximumVisitedProducers)
            {
                AddTraceStopReason(finding, "maximum_visited_producers");
                break;
            }

            var key = ProducerKey(reference.PlanetId, reference.ObjectId, reference.ItemId);
            if (!visited.Add(key))
            {
                cycleDetected = true;
                continue;
            }

            var producer = resolveProducer(reference);
            if (producer is null
                || producer.PlanetId != reference.PlanetId
                || producer.ObjectId != reference.ObjectId
                || producer.TargetItemId != reference.ItemId)
            {
                unresolvedReference = true;
                continue;
            }

            var upstream = Trace(
                producer,
                resolveProducer,
                visited,
                depth + 1,
                maximumDepth,
                maximumVisitedProducers);
            if (upstream is null)
            {
                continue;
            }

            upstream.UpstreamPath.InsertRange(0, finding.UpstreamPath);
            return upstream;
        }

        if (!finding.Evidence.Any(evidence => string.Equals(
                evidence.Metric,
                "upstream_trace_stop_reason",
                StringComparison.Ordinal)))
        {
            if (cycleDetected)
            {
                AddTraceStopReason(finding, "cycle_detected");
            }
            else if (unresolvedReference)
            {
                AddTraceStopReason(finding, "unresolved_producer_reference");
            }
        }

        return finding;
    }

    private static string ProducerKey(int planetId, int objectId, int itemId) =>
        $"{planetId}:{objectId}:{itemId}";

    private static void AddTraceStopReason(OverseerFindingSnapshot finding, string reason)
    {
        if (finding.Evidence.Any(evidence => string.Equals(
                evidence.Metric,
                "upstream_trace_stop_reason",
                StringComparison.Ordinal)))
        {
            return;
        }

        finding.Evidence.Add(new OverseerEvidenceSnapshot
        {
            Metric = "upstream_trace_stop_reason",
            TextValue = reason,
        });
    }
}
