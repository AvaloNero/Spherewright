using System.Security.Cryptography;

namespace Spherewright.Bridge.Core.Snapshots;

public static class OpaqueToken
{
    public static string Create(int byteCount = 32)
    {
        if (byteCount < 32)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), "Opaque tokens must contain at least 256 bits of entropy.");
        }

        var bytes = new byte[byteCount];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
