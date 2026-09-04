namespace Spherewright.Bridge.Core.Safety;

public static class GameplayModePolicy
{
    public static bool AllowsNormalActions(
        bool descriptorAvailable,
        bool isPeaceful,
        bool isSandboxMode,
        bool sandboxToolsEnabled,
        float resourceMultiplier)
    {
        // Sandbox and resource settings are evidence, not authorization gates.
        // Spherewright still uses only its bounded normal-game primitives and
        // never invokes sandbox tools or injects resources.
        _ = isSandboxMode;
        _ = sandboxToolsEnabled;
        _ = resourceMultiplier;
        return descriptorAvailable && isPeaceful;
    }
}
