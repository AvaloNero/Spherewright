namespace Spherewright.Bridge.Core.Safety;

public sealed class PreparedPlan<T>
{
    internal PreparedPlan(string token, DateTimeOffset expiresAtUtc, string fingerprint, T payload)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Fingerprint = fingerprint;
        Payload = payload;
    }

    public string Token { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public string Fingerprint { get; }

    public T Payload { get; }
}
