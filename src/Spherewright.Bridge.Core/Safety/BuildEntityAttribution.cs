namespace Spherewright.Bridge.Core.Safety;

public readonly struct BuildEntityCandidate
{
    public BuildEntityCandidate(int entityId, float squaredDistance)
    {
        EntityId = entityId;
        SquaredDistance = squaredDistance;
    }

    public int EntityId { get; }

    public float SquaredDistance { get; }
}

public readonly struct DirectedBuildEntityCandidate
{
    public DirectedBuildEntityCandidate(
        int entityId,
        int inputObjectId,
        int outputObjectId,
        float squaredDistance)
    {
        EntityId = entityId;
        InputObjectId = inputObjectId;
        OutputObjectId = outputObjectId;
        SquaredDistance = squaredDistance;
    }

    public int EntityId { get; }

    public int InputObjectId { get; }

    public int OutputObjectId { get; }

    public float SquaredDistance { get; }
}

public static class BuildEntityAttribution
{
    public static int SelectNearestNewCandidate(
        IEnumerable<BuildEntityCandidate> candidates,
        IReadOnlyCollection<int> preexistingEntityIds,
        IReadOnlyCollection<int> alreadySelectedEntityIds,
        float maximumSquaredDistance)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (preexistingEntityIds is null)
        {
            throw new ArgumentNullException(nameof(preexistingEntityIds));
        }

        if (alreadySelectedEntityIds is null)
        {
            throw new ArgumentNullException(nameof(alreadySelectedEntityIds));
        }

        if (maximumSquaredDistance <= 0f
            || float.IsNaN(maximumSquaredDistance)
            || float.IsInfinity(maximumSquaredDistance))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSquaredDistance));
        }

        var bestId = 0;
        var bestDistance = maximumSquaredDistance;
        foreach (var candidate in candidates)
        {
            if (candidate.EntityId <= 0
                || candidate.SquaredDistance < 0f
                || float.IsNaN(candidate.SquaredDistance)
                || float.IsInfinity(candidate.SquaredDistance)
                || preexistingEntityIds.Contains(candidate.EntityId)
                || alreadySelectedEntityIds.Contains(candidate.EntityId))
            {
                continue;
            }

            if (candidate.SquaredDistance < bestDistance)
            {
                bestDistance = candidate.SquaredDistance;
                bestId = candidate.EntityId;
            }
        }

        return bestId;
    }

    public static bool TrySelectUniqueDirectedPath(
        IReadOnlyList<IReadOnlyList<DirectedBuildEntityCandidate>> candidatesByStep,
        IReadOnlyCollection<int> excludedEntityIds,
        int sourceObjectId,
        int destinationObjectId,
        float maximumSquaredDistance,
        out IReadOnlyList<int> selectedEntityIds)
    {
        if (candidatesByStep is null)
        {
            throw new ArgumentNullException(nameof(candidatesByStep));
        }

        if (excludedEntityIds is null)
        {
            throw new ArgumentNullException(nameof(excludedEntityIds));
        }

        if (maximumSquaredDistance <= 0f
            || float.IsNaN(maximumSquaredDistance)
            || float.IsInfinity(maximumSquaredDistance))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSquaredDistance));
        }

        selectedEntityIds = Array.Empty<int>();
        if (candidatesByStep.Count == 0 || candidatesByStep.Any(step => step is null))
        {
            return false;
        }

        var current = new int[candidatesByStep.Count];
        int[]? unique = null;
        var solutionCount = 0;

        void Search(int stepIndex, int expectedInputObjectId)
        {
            if (solutionCount > 1)
            {
                return;
            }

            if (stepIndex == candidatesByStep.Count)
            {
                if (destinationObjectId > 0
                    && candidatesByStep.Count > 0
                    && !candidatesByStep[candidatesByStep.Count - 1]
                        .Any(candidate => candidate.EntityId == current[current.Length - 1]
                            && candidate.OutputObjectId == destinationObjectId))
                {
                    return;
                }

                solutionCount++;
                unique = current.ToArray();
                return;
            }

            foreach (var candidate in candidatesByStep[stepIndex])
            {
                if (candidate.EntityId <= 0
                    || candidate.SquaredDistance < 0f
                    || candidate.SquaredDistance >= maximumSquaredDistance
                    || float.IsNaN(candidate.SquaredDistance)
                    || float.IsInfinity(candidate.SquaredDistance)
                    || excludedEntityIds.Contains(candidate.EntityId)
                    || current.Take(stepIndex).Contains(candidate.EntityId)
                    || (expectedInputObjectId > 0 && candidate.InputObjectId != expectedInputObjectId))
                {
                    continue;
                }

                if (stepIndex + 1 < candidatesByStep.Count)
                {
                    if (candidate.OutputObjectId <= 0)
                    {
                        continue;
                    }
                }
                else if (destinationObjectId > 0 && candidate.OutputObjectId != destinationObjectId)
                {
                    continue;
                }

                current[stepIndex] = candidate.EntityId;
                Search(stepIndex + 1, candidate.EntityId);
                current[stepIndex] = 0;
            }
        }

        Search(0, sourceObjectId);
        if (solutionCount != 1 || unique is null)
        {
            return false;
        }

        selectedEntityIds = unique;
        return true;
    }
}
