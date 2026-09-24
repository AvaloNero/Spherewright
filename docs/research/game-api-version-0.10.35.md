# DSP 0.10.35.29057 native API and save-format evidence

## Scope and evidence boundary

This note covers one exact local version pair only: save/ticket/Journal source `0.10.34.28529` and installed DSP `0.10.35.29057`. It is not a general cross-version compatibility declaration. All inspection on 2026-09-24 used `Mono.Cecil` or `ilspycmd` as metadata/decompilation readers; no game assembly was loaded for execution, no save was loaded, and no installed file, ticket, Journal, or compile reference was changed.

The installed `DSPGAME_Data/Managed/Assembly-CSharp.dll` is SHA-256 **E75D3FE4B6A9CA822766189F826BA3A8348DFB7E301AA37FF6779DB29A83FD8D**, length **8,019,456 bytes**. The former `0.10.34.28529` reference is SHA-256 **AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85**, length **7,830,016 bytes**. Installed `Updates/Versions.txt` records `0.10.35.29057,True,2026-09-23`; `GameConfig` embeds `new Version(0, 10, 35)`, while the former DLL embeds `new Version(0, 10, 34)`.

## Unchanged load/save entry points

The following methods have the same signature, code size, and normalized IL instruction stream in both DLLs:

| Method | Code size | Normalized IL evidence |
|---|---:|---|
| `bool GameSave.SaveCurrentGame(string)` | 846 | identical (`BE8CEF36E74EA37E…`) |
| `void GameSave.ReadHeader(string, bool, out GameSaveHeader)` | 604 | identical (`C1AC05D680217469…`) |
| `void DSPGame.StartGame(GameDesc)` | 87 | identical (`4A2B9F2FCC04627B…`) |
| `void DSPGame.StartGame(string)` | 86 | identical (`6912380381372CCA…`) |

`SaveCurrentGame` still writes native signature `VFSAVE`, header version `7`, four `GameConfig.gameVersion` components, game tick, UTC save time, screenshot/account data, then `GameData.Export`. This supports reuse of the researched bounded header layout; it does not by itself authorize loading an older save.

## Changed formats and explicit backward readers

The native container changes are real and must not be hidden by a DLL-hash exception:

- `GameData` keeps outer format version `13`, but `CURRENT_PATCH` changes from `22` to `23`. The new importer contains the `patch < 23` migration branch.
- The former `GalaxyData.ExportScannedDatas` writes runtime marker `0` and scanned-planet records through terminal `-1`. The new `ExportRuntimeData` writes marker `1`, the same scanned-record section through `-1`, then a new vegetation-collection section through a second `-1`.
- New `GalaxyData.ImportRuntimeData` reads the marker and the old scanned-record section first. When the marker is `< 1`, it returns at that first `-1`; therefore the exact former marker-0 payload has an explicit backward-read path. Marker 1 continues into the new section.
- `GameDesc.Export` changes format version `9` to `10` by adding `starNameLCID`. New `GameDesc.Import` reads that field only for `version >= 10` and otherwise sets it to `0`; the exact former version-9 descriptor has an explicit backward-read path.
- `GameData.Import/Export` and `GameDesc.Import/Export` consequently have changed IL. This note does not claim unchanged whole-save behavior merely because the four entry points above are unchanged.

## Prefix-reader implications

Before the exact-pair adaptation, `OwnedSavePrefixReader` required native header `7`, `GameData 13 / patch 22`, and `GameDesc 9`. That was the correct bounded parser for the existing `73573789` source save, but it rejected a newly resaved `0.10.35.29057` primary (`13 / 23`, `GameDesc 10`). The adaptation must preserve tuple identity by accepting only the researched source and target tuples rather than independently accepting arbitrary header, patch, and descriptor versions.

Recompiling against the installed DLL can establish API compatibility and remove the cold-deployment reference mismatch, but it cannot alone authorize recovery. The original ticket, source save prefix, and Journal identify `0.10.34.28529`; the running host identifies `0.10.35.29057`. Before the exact-pair adaptation, the stores and policies deliberately required exact version equality and therefore rejected before native load. The adaptation must name this exact source/current pair and keep every other version pair fail-closed.

## Adoption and Journal hazards for the narrow pair

The following are product requirements, not live-validated conclusions:

1. `GameSessionTracker.TryValidateResumeCandidate` verifies protected save identity, bounded game tick, the existing peaceful-mode policy, and planet, but does not independently establish source-to-current game-version migration. The pre-load prefix evidence and the narrow pair policy must carry that proof; `GameDesc.creationVersion` is a creation-version field and is not a substitute.
2. The existing Journal document is historical source evidence and currently records `0.10.34.28529`. Before the exact-pair adaptation, `GameplayJournalManager.AttachToCurrentOwnedSession` required `document.GameVersion == current host version`; the migration path must not silently relabel the file before exact checkpoint/content validation.
3. After exact old-Journal validation and successful native adoption, the Journal must be migrated to `0.10.35.29057` durably without changing its identity, tracking mode, historical-coverage flag, starting tick, sequence continuity, or entries. Confirmation, lease release, automatic normal save, and new-ticket issuance must be ordered so a crash cannot produce a current-version ticket backed by an old or partially rewritten Journal.
4. The automatic normal save should produce header `7`, `GameData 13 / patch 23`, and `GameDesc 10`. Before a refreshed credential is trusted, the same owned identity, tick floor, current version, healthy Journal, and current known tuple need verification. The exact-pair adaptation must perform that target-tuple check before issuing the refreshed credential.
5. Failure after old-ticket attempt/tombstone consumption remains a manual-reconciliation state. The backward-read evidence does not authorize retrying the old capability, selecting `LastExit`, importing a copy, editing ticket/Journal versions, or loading another filename.

## Pending live limits

No live prepare, native load, migration, adoption, Journal rewrite, automatic resave, refreshed ticket, or restart has been demonstrated on `0.10.35.29057`. Production/logistics state, sustained power, and later construction are outside this version-pair research. The exact save remains `73573789 / J91 / accepted5` until separately protected execution evidence proves otherwise.
