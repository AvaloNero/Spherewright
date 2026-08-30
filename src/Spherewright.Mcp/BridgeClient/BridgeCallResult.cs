using Spherewright.Contracts.Errors;

namespace Spherewright.Mcp.BridgeClient;

public sealed class BridgeCallResult<T>
{
    private BridgeCallResult(bool success, T? value, BridgeError? error)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public bool Success { get; }

    public T? Value { get; }

    public BridgeError? Error { get; }

    public static BridgeCallResult<T> Succeeded(T value)
    {
        return new BridgeCallResult<T>(true, value, null);
    }

    public static BridgeCallResult<T> Failed(BridgeError error)
    {
        return new BridgeCallResult<T>(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    }
}

