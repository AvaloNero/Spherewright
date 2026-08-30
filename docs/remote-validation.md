# Deferred architecture — LAN game validation host

Status: **deferred until local M0 is complete and repeatable**.

Spherewright currently uses the same Windows computer for development, build, DSP runtime, and in-game validation. This keeps the earliest incomplete M0 work focused on ordinary-game observation and action correctness.

## Future topology

```text
Development computer
  source / build / automated tests
        | authenticated SSH + SCP or SMB deployment
        v
LAN game-validation computer
  DSP + BepInEx + Spherewright.Plugin
  Spherewright.Mcp
        | local authenticated Named Pipe
        v
  Spherewright.Plugin / DSP main thread

Structured test results and sanitized logs
        ^
        | authenticated SSH file/result collection
```

The Named Pipe remains local to the game computer. It must not be exposed over TCP or shared through SMB. `Spherewright.Mcp` runs on the game computer, discovers the runtime descriptor locally, and transports only MCP stdio over the authenticated remote command channel. The bridge token and runtime descriptor stay on the game computer.

## Intended workflow

1. Verify the game computer's DSP version, BepInEx version, and required assembly hashes.
2. Build against the exact verified game version.
3. Refuse deployment while DSP is running; never kill the game or overwrite a loaded Plugin.
4. Copy only Spherewright Plugin/MCP artifacts to a versioned staging location, then install the intended files.
5. Start DSP in an already logged-in interactive Windows session and wait at the main menu.
6. Run the same structured M0 validation suite used locally; do not use Computer Use, visual recognition, or keyboard/mouse macros.
7. Retrieve only structured results and sanitized Spherewright/BepInEx logs. Do not enumerate, copy, or read save files.
8. Restore `Safety.AllowWrites=false` after the run.

## Security boundary

- Prefer Windows OpenSSH with key authentication; never place a password, bridge token, or authorization code in scripts or chat.
- Restrict the firewall rule and remote account to the development computer and required commands.
- Do not add HTTP/SSE/Streamable HTTP to the Plugin for this purpose.
- Do not share `%LOCALAPPDATA%\Spherewright\runtime` over the network.
- Continue accepting only Spherewright-created test sessions and ordinary peaceful 1x non-sandbox worlds.
- Keep deployment and result collection separate from any save directory.

## Deferred scripts

These are design placeholders only and must not be created until the user resumes this work:

```text
scripts/remote/prepare-game-host.ps1
scripts/remote/deploy-game-host.ps1
scripts/remote/run-validation.ps1
scripts/remote/collect-evidence.ps1
```

Before implementation, choose the exact remote transport and interactive-session launch method for the game computer. Remote GUI launch from an SSH service session is not assumed to work; a logged-in user session or an explicitly configured on-demand scheduled task may be required.

## Activation gate

Do not begin this deferred architecture until all of the following are true:

- Local Gate B and Gate C are complete.
- The complete local first-red-matrix run is repeatable from a fresh ordinary world.
- Local evidence collection contains no save reads, secrets, Computer Use, sandbox actions, or unexplained item deltas.
- The user explicitly asks to resume LAN validation-host work.
