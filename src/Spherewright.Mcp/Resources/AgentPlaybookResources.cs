using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Spherewright.Mcp.Resources;

[McpServerResourceType]
public static class AgentPlaybookResources
{
    public const string OpeningMovementUri = "spherewright://agent/playbooks/opening-movement-v1";

    private const string EmbeddedResourceName = "Spherewright.Mcp.agent-playbook.md";

    [McpServerResource(
        UriTemplate = OpeningMovementUri,
        Name = "spherewright-agent-opening-movement",
        Title = "Spherewright opening and core operation playbook",
        MimeType = "text/markdown")]
    [Description("Required opening-session guidance for ownership, commit outcomes, energy, harvesting, bounded movement recovery, production-line proof, saves, and flight recovery.")]
    public static TextResourceContents GetOpeningMovementPlaybook()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException("The embedded Spherewright Agent playbook is unavailable.");
        using var reader = new StreamReader(stream);
        return new TextResourceContents
        {
            Uri = OpeningMovementUri,
            MimeType = "text/markdown",
            Text = reader.ReadToEnd(),
        };
    }
}
