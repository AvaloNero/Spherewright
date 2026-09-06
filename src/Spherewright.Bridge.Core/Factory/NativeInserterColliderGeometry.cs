namespace Spherewright.Bridge.Core.Factory;

// Current DSP BuildTool_BlueprintPaste.CheckBuildConditions inserter box formula.
// Pure dimensions only; Plugin supplies verified native prefab/endpoint/pose values.
public static class NativeInserterColliderGeometry
{
    public static (float CentreOffsetZ, float HalfLength) Calculate(float span, float prefabHalfLength,
        bool inputIsBelt, bool outputIsBelt)
    {
        if (float.IsNaN(span) || float.IsInfinity(span) || span < 0 || span > 64
            || float.IsNaN(prefabHalfLength) || float.IsInfinity(prefabHalfLength) || prefabHalfLength < 0 || prefabHalfLength > 16)
            throw new ArgumentOutOfRangeException(nameof(span));
        var offset = (inputIsBelt ? -.35f : 0) + (outputIsBelt ? .35f : 0);
        var extent = span * .5f + prefabHalfLength - .5f + (inputIsBelt ? .35f : 0) + (outputIsBelt ? .35f : 0);
        return (offset, Math.Max(.1f, extent));
    }
}
