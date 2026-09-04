# Installing a Spherewright release

The versioned release package targets Windows x64, Dyson Sphere Program `0.10.34.28529`, and BepInEx `5.4.17.0`. Its exact Spherewright version is recorded in `manifest.json` and reported by the installer. The package includes the Plugin and a self-contained MCP executable; end users do not need the repository, source code, or a .NET SDK.

The supported gameplay scope is single-player, peaceful, non-sandbox, and 1× resources. Dark Fog/combat, sandbox, multiplayer or Nebula, non-1× resources, broad third-party Mod compatibility, an arbitrary save picker, and arbitrary save-name loads are not guaranteed.

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

## Quick start

### Start a new world

Leave DSP at the idle main menu, set `Safety.AllowWrites=true`, restart DSP, and ask the Agent to create a new world. The normal new-game flow creates a peaceful, non-sandbox, 1× save named `Spherewright_New_*`. Legacy `Spherewright_M0_*` saves are not migrated or renamed and can still be resumed only through their exact protected tickets.

### Continue an existing save

Set `Safety.AllowWrites=true` and `Safety.AllowUserSaveImport=true`, restart DSP, then manually load the intended supported save. Ask the Agent to prepare import; review its disclosure and reply with a later explicit confirmation. Only then may commit create and Header-verify an independent `Spherewright_Imported_*` copy. The original save remains unchanged. Continue manual and Agent play in the copy, and after a restart leave DSP at the main menu and use protected resume. Whenever you operate that copy manually, the Agent must fresh-read it and prepare later writes with the current state hashes instead of reusing stale observations or plans.

A Spherewright-looking filename never grants ownership. Import does not provide arbitrary save selection or loading, and the imported copy's journal begins at the import point rather than reconstructing earlier history.

The repository records offline, local-live, and cross-computer-live evidence separately. At present, the checked-in save-import evidence is offline only; these setup steps do not claim that the full import flow has completed local or cross-computer live validation.

## Verify or troubleshoot

- A modified or incomplete package is rejected before installation.
- The installer refuses to run while `DSPGAME` is active so it cannot overwrite loaded assemblies.
- If more than one DSP installation is found, pass the intended directory explicitly with `-DspDir`.
- Report the Spherewright version, supported DSP/BepInEx versions, structured error code, and sanitized state evidence. Never publish tokens, plan tokens, runtime descriptors, save names, or save files.
