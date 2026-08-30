using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;

namespace Spherewright.Mcp.BridgeClient;

internal sealed class BridgeDescriptorLocator
{
    private readonly BridgeClientOptions _options;
    private readonly Func<int, bool> _processValidator;
    private readonly Func<string, bool> _aclValidator;

    public BridgeDescriptorLocator(BridgeClientOptions options)
        : this(options, IsLiveDspProcess, HasCurrentUserOnlyAcl)
    {
    }

    internal BridgeDescriptorLocator(
        BridgeClientOptions options,
        Func<int, bool> processValidator,
        Func<string, bool> aclValidator)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _processValidator = processValidator ?? throw new ArgumentNullException(nameof(processValidator));
        _aclValidator = aclValidator ?? throw new ArgumentNullException(nameof(aclValidator));
    }

    public BridgeCallResult<LocatedBridgeDescriptor> Locate()
    {
        if (!string.IsNullOrWhiteSpace(_options.ExplicitDescriptorPath))
        {
            return LoadSingle(_options.ExplicitDescriptorPath!);
        }

        if (!string.IsNullOrWhiteSpace(_options.EnvironmentDescriptorPath))
        {
            return LoadSingle(_options.EnvironmentDescriptorPath!);
        }

        if (!Directory.Exists(_options.RuntimeDirectory))
        {
            return NotReady("No active Spherewright runtime descriptor was found.");
        }

        var valid = new List<LocatedBridgeDescriptor>();
        foreach (var path in Directory.GetFiles(_options.RuntimeDirectory, "bridge-*.json", SearchOption.TopDirectoryOnly))
        {
            var loaded = TryLoad(path);
            if (loaded is not null)
            {
                valid.Add(loaded);
            }
        }

        if (valid.Count == 0)
        {
            return NotReady("No active Spherewright runtime descriptor was found.");
        }

        if (valid.Count > 1)
        {
            return NotReady("Multiple active Spherewright bridges were found.");
        }

        return BridgeCallResult<LocatedBridgeDescriptor>.Succeeded(valid[0]);
    }

    private BridgeCallResult<LocatedBridgeDescriptor> LoadSingle(string path)
    {
        var loaded = TryLoad(path);
        return loaded is null
            ? NotReady("The selected Spherewright bridge descriptor is unavailable or invalid.")
            : BridgeCallResult<LocatedBridgeDescriptor>.Succeeded(loaded);
    }

    private LocatedBridgeDescriptor? TryLoad(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath) || !_aclValidator(fullPath))
            {
                return null;
            }

            var descriptor = McpBridgeJson.Deserialize<BridgeRuntimeDescriptor>(File.ReadAllText(fullPath));
            if (descriptor is null
                || descriptor.ProcessId <= 0
                || descriptor.ProtocolVersion != ProtocolConstants.CurrentVersion
                || string.IsNullOrWhiteSpace(descriptor.BridgeInstanceId)
                || string.IsNullOrWhiteSpace(descriptor.PipeName)
                || string.IsNullOrWhiteSpace(descriptor.AuthToken)
                || !_processValidator(descriptor.ProcessId))
            {
                return null;
            }

            return new LocatedBridgeDescriptor(fullPath, descriptor);
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is JsonException
            || exception is ArgumentException
            || exception is NotSupportedException)
        {
            return null;
        }
    }

    private static BridgeCallResult<LocatedBridgeDescriptor> NotReady(string message)
    {
        return BridgeCallResult<LocatedBridgeDescriptor>.Failed(BridgeError.Create(
            BridgeErrorCodes.BridgeNotReady,
            message,
            true,
            "Start DSP with the Spherewright Plugin loaded, then retry."));
    }

    private static bool IsLiveDspProcess(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return string.Equals(process.ProcessName, "DSPGAME", StringComparison.OrdinalIgnoreCase)
                && !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasCurrentUserOnlyAcl(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var currentSid = WindowsIdentity.GetCurrent().User;
            if (currentSid is null)
            {
                return false;
            }

            var security = new FileInfo(path).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
            var owner = security.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
            if (owner is null || !owner.Equals(currentSid))
            {
                return false;
            }

            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Allow
                    && rule.IdentityReference is SecurityIdentifier sid
                    && !sid.Equals(currentSid))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException
            || exception is IOException
            || exception is PlatformNotSupportedException)
        {
            return false;
        }
    }
}

