using Spherewright.Contracts.Errors;

namespace Spherewright.Plugin.Game;

internal sealed class GameCallResult<T>
{
    private GameCallResult(T? value, BridgeError? error)
    {
        Value = value;
        Error = error;
    }

    public bool Success => Error is null;

    public T? Value { get; }

    public BridgeError? Error { get; }

    public static GameCallResult<T> Succeeded(T value) => new GameCallResult<T>(value, null);

    public static GameCallResult<T> Failed(BridgeError error) => new GameCallResult<T>(default, error);
}
