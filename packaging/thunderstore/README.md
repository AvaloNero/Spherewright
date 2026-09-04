# Spherewright

Spherewright connects an external AI agent to Dyson Sphere Program through MCP. It exposes structured game observations and carefully bounded actions while keeping item production, construction, research, energy use, travel, saving, and recovery inside normal game mechanics.

This package contains Spherewright `{{VERSION}}` for Windows x64:

- a BepInEx 5 Plugin loaded by Dyson Sphere Program;
- a self-contained `Spherewright.Mcp.exe` for an external MCP host;
- the Agent opening-movement playbook used by the packaged MCP resource.

End users do not need the source repository or a .NET SDK.

## Supported scope

- Dyson Sphere Program `0.10.34.28529`
- BepInEx `5.4.17`
- Windows x64
- single-player peaceful mode; sandbox state and resource multiplier are reported but do not gate normal actions

The validated reference world remains non-sandbox with 1× resources. Sandbox and non-1× compatibility are enabled in 0.3.3 but await cross-computer live validation; Spherewright still never calls sandbox tools or injects resources. Dark Fog/combat, multiplayer or Nebula, and broad third-party Mod compatibility are not supported.

## Install with a Mod Manager

1. Install Spherewright and its BepInEx dependency through Thunderstore Mod Manager or r2modman.
2. Launch Dyson Sphere Program through the selected modded profile once so the Plugin creates its configuration and runtime descriptor.
3. Open that profile's folder and locate `BepInEx/plugins/{{TEAM_NAME}}-Spherewright/Spherewright.Mcp.exe`.
4. Register that executable as a local stdio MCP server in the external Agent host. Do not pass a runtime descriptor, authentication token, or save identity on the command line.

Spherewright starts observation-only. Set `Safety.AllowWrites=true` in `BepInEx/config/dev.spherewright.bridge.cfg` only when you intend to authorize structured gameplay commits. Importing a world that you loaded manually additionally requires `Safety.AllowUserSaveImport=true`, a fresh no-side-effect prepare, and a later explicit confirmation in the Agent conversation.

## Manual installation and documentation

The versioned installer package, integrity checksum, installation/upgrade/rollback instructions, source code, and issue tracker are available on [GitHub](https://github.com/AvaloNero/Spherewright/releases).

Spherewright is licensed under the MIT License.
