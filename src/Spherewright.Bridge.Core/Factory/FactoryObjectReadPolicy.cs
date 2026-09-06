using Spherewright.Contracts.Errors;

namespace Spherewright.Bridge.Core.Factory;

public static class FactoryObjectReadPolicy
{
    public static BridgeError? ValidateObjectId(int objectId) => objectId != 0 && objectId != int.MinValue
        ? null
        : BridgeError.Create(BridgeErrorCodes.InvalidRequest,
            "inspect_factory_entity requires objectId: a positive entity ID or negative prebuild ID; zero/missing and Int32.MinValue are invalid. The read payload field is objectId, not entityId.",
            false, "Use the exact inspect_factory_entity request schema and a fresh listed objectId. An invalid request does not prove that a building disappeared. Other action schemas may use entityId; do not rename those fields.");
}
