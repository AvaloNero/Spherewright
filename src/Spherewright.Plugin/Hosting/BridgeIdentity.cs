using System.Diagnostics;
using System.Security.Cryptography;

namespace Spherewright.Plugin.Hosting;

internal sealed class BridgeIdentity
{
    private BridgeIdentity(string bridgeInstanceId, string pipeName, string authToken)
    {
        BridgeInstanceId = bridgeInstanceId;
        PipeName = pipeName;
        AuthToken = authToken;
    }

    public string BridgeInstanceId { get; }

    public string PipeName { get; }

    public string AuthToken { get; }

    public static BridgeIdentity Create(string pipeNamePrefix)
    {
        var processId = Process.GetCurrentProcess().Id;
        var pipeSuffix = Base64Url(RandomBytes(12));
        var token = Base64Url(RandomBytes(32));
        return new BridgeIdentity(
            Guid.NewGuid().ToString("N"),
            $"{pipeNamePrefix}-{processId}-{pipeSuffix}",
            token);
    }

    private static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return bytes;
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

