using System.Text;

namespace Spherewright.Plugin.Game;

internal static class AssemblerCursorCodec
{
    public static string Encode(string sessionId, long revision, int nextComponentId)
    {
        var value = $"{sessionId}|{revision}|{nextComponentId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static bool TryDecode(string? cursor, out string sessionId, out long revision, out int nextComponentId)
    {
        sessionId = string.Empty;
        revision = 0;
        nextComponentId = 1;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = value.Split('|');
            if (parts.Length != 3
                || string.IsNullOrWhiteSpace(parts[0])
                || !long.TryParse(parts[1], out revision)
                || !int.TryParse(parts[2], out nextComponentId)
                || nextComponentId <= 0)
            {
                return false;
            }

            sessionId = parts[0];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
