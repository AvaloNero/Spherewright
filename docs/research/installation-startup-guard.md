# Installation startup guard — runtime boundary

2026-09-27; source-level and offline verification, not a cold-deployed result.

The current reference `BepInEx.dll` has assembly version `5.4.17.0` and SHA-256
`DC1CB6B58B962BDA5AAA1D6B5F9AE14EC174F61836A1A1F96C1A040C7E8381F7`.
Read-only inspection of `BepInEx.Paths` confirms the public static
`BepInExRootPath` getter. `SetExecutablePath` derives that root from its explicit
override or the game's `BepInEx` child, and derives `PluginPath` from its
`plugins` child. The guard uses that runtime root rather than guessing a Steam
installation or user directory. No DSP gameplay API or save format changes.

The target identity is the directory containing the actual
`typeof(SpherewrightPlugin).Assembly.Location`. The mutex key matches the manual
installer: `Local\SpherewrightInstallTarget-` followed by uppercase SHA-256 hex
of UTF-8 encoding of the canonical target path's invariant uppercase form.
The path must be an existing ordinary directory, including its ancestors.

`Awake` acquires the target lease before loading configuration or constructing
`SpherewrightBridgeHost`. While holding it, any pending marker at
`BepInExRootPath/.spherewright-install-pending.json`, or inability to establish
its absence, refuses startup. Marker contents cannot self-authorize startup.
The lease outlives pipe/descriptor cleanup and is released on the Unity main
thread. This prevents a guard-aware Bridge and cooperating installer from
owning the same target concurrently; it does not prevent CLR assembly loading.
An abandoned mutex grants ownership even though `WaitOne` throws, so both
implementations must release that ownership while refusing the operation.

Offline tests cover marker refusal, busy and abandoned mutexes, lease lifetime,
canonical naming, and actual Windows PowerShell/PowerShell 7 child-process
contention with the .NET 8 linked guard. The full `net472` Plugin builds against
the current references. Unity Mono interoperability, real loader paths and
`Awake`/`OnDestroy` behavior still require cold-deployed validation.

Older Plugin binaries do not contain this gate. Best-effort withholding of the
live main DLL on unresolved caught failures reduces that exposure but cannot
retrofit a lock into an already loaded old Plugin or guarantee safety during a
process kill or a failed withholding operation. No legacy-wide atomic-upgrade,
automatic crash recovery, or Mod Manager compatibility claim is made.
