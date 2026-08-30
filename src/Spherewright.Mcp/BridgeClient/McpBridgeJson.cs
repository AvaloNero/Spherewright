using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Spherewright.Mcp.BridgeClient;

internal static class McpBridgeJson
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 64,
    };

    public static string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, Formatting.None, Settings);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
}

