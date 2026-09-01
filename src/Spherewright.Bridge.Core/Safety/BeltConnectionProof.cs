namespace Spherewright.Bridge.Core.Safety;

public static class BeltConnectionProof
{
    public static bool OutputMatches(int expectedObjectId, bool actualIsOutput, int actualObjectId)
    {
        if (expectedObjectId > 0)
        {
            return actualIsOutput && actualObjectId == expectedObjectId;
        }

        return actualObjectId == 0;
    }
}
