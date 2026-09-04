# Installing a Spherewright release

The versioned release package targets Windows x64, Dyson Sphere Program `0.10.34.28529`, and BepInEx `5.4.17.0`. Its exact Spherewright version is recorded in `manifest.json` and reported by the installer. The package includes the Plugin and a self-contained MCP executable; end users do not need the repository, source code, or a .NET SDK.

## Prerequisites

- Install Dyson Sphere Program and BepInEx 5.
- Exit DSP before installing or upgrading Spherewright.
- Keep the extracted release package until installation has completed and its integrity check succeeds.

## Install

Extract the zip, open PowerShell in the extracted `Spherewright-<version>` directory, and run:

```powershell
.\install.ps1
```

The installer verifies every packaged file against `manifest.json`, locates DSP through the explicit `-DspDir` argument, `SPHEREWRIGHT_DSP_DIR`, or Steam, and then installs the Plugin under `BepInEx\plugins\Spherewright`. The self-contained MCP server is installed under `%LOCALAPPDATA%\Spherewright\mcp\<version>` by default.

For a nonstandard DSP location:

```powershell
.\install.ps1 -DspDir 'D:\Games\Dyson Sphere Program'
```

The command returns JSON containing the exact installed Plugin directory and MCP executable. Register that `Spherewright.Mcp.exe` path as a stdio MCP server in the external Agent host; do not pass runtime descriptors, authentication tokens, or save identities as arguments.

Reinstalling the same MCP version requires `-Force`. The installer never starts DSP, changes a save, enables writes, or installs BepInEx. Spherewright's generated BepInEx configuration remains observation-only by default; enable `Safety.AllowWrites` only when you intend to authorize structured commits. Importing a world that you loaded manually also requires the separate `Safety.AllowUserSaveImport=true` opt-in, followed at runtime by a fresh no-side-effect prepare and your subsequent explicit confirmation in the Agent conversation.

## Verify or troubleshoot

- A modified or incomplete package is rejected before installation.
- The installer refuses to run while `DSPGAME` is active so it cannot overwrite loaded assemblies.
- If more than one DSP installation is found, pass the intended directory explicitly with `-DspDir`.
- Report the Spherewright version, supported DSP/BepInEx versions, structured error code, and sanitized state evidence. Never publish tokens, plan tokens, runtime descriptors, save names, or save files.
