using System.Security.Cryptography;
using System.Text;

namespace Spherewright.Plugin.Game;

internal static class GameplayJournalIdentity
{
    public static string HashOwnedSaveIdentity(string ownedSaveName)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"spherewright-gameplay-journal-v1\n{ownedSaveName}"));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
