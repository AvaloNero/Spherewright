namespace Spherewright.Bridge.Core.Safety;

public static class BeltConnectionProof
{
    public static bool InputMatches(int expectedObjectId, bool actualIsOutput, int actualObjectId)
    {
        return expectedObjectId > 0
            ? !actualIsOutput && actualObjectId == expectedObjectId
            : actualObjectId == 0;
    }

    public static bool OutputMatches(int expectedObjectId, bool actualIsOutput, int actualObjectId)
    {
        if (expectedObjectId > 0)
        {
            return actualIsOutput && actualObjectId == expectedObjectId;
        }

        return actualObjectId == 0;
    }
}
