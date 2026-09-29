# DSP 0.10.35.29104 exact-pair recovery research

## Evidence and scope (2026-09-29)

Steam updated the installed game while starting it for the next owned-world trial. The running Bridge reported `0.10.35.29104`; the protected, unconsumed primary ticket and its matching runtime/handoff replica still record `0.10.35.29088`, saved tick `77676444`, planet `104`, and durable Journal sequence `91`. The Journal has 91 entries, origin `0.10.34.28529`, and one existing durable `28529 → 29088` transition. A single read-only `reauthorize_expired_primary` prepare on the old installed Plugin returned `SESSION_NOT_OWNED`; no commit, load, save, or ticket consumption occurred.

This research authorizes only the additional directional pair **`0.10.35.29088 → 0.10.35.29104`** on the protected expired-primary path. It does not authorize `28529 → 29104` directly, `29057 → 29104`, downgrade, a version wildcard, LastExit, arbitrary saves, or an automatic retry after an interrupted attempt.

## Native comparison

The retained 29088 `Assembly-CSharp.dll` is SHA-256 `C43A484F6ADF8A9E4B956156047070891B46860D5B5C707BA1377B6A2AF25732` (8,020,480 bytes). The installed 29104 DLL is SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12` (8,022,016 bytes); Steam's installed build ID is `25599610`. Both assemblies were read as metadata with Mono.Cecil, without invoking their game code. For each method below, the full signature, body byte length, instruction count, and per-instruction opcode/operand text were equal:

| Native method | Body bytes | Instructions |
| --- | ---: | ---: |
| `GameSave.SaveCurrentGame(string)` | 846 | 290 |
| `GameSave.SavePath(string)` | 42 | 14 |
| `GameSave.ReadHeader(string,bool,out GameSaveHeader)` | 604 | 263 |
| `DSPGame.StartGame(GameDesc)` | 87 | 26 |
| `DSPGame.StartGame(string)` | 86 | 28 |
| `GameData.Export(BinaryWriter)` | 616 | 223 |
| `GameData.Import(BinaryReader)` | 2146 | 780 |
| `GameDesc.Export(BinaryWriter)` | 268 | 97 |
| `GameDesc.Import(BinaryReader)` | 560 | 223 |
| `GalaxyData.ExportRuntimeData(BinaryWriter)` | 284 | 122 |
| `GalaxyData.ImportRuntimeData(BinaryReader)` | 224 | 101 |

29104 still declares `GameData.CURRENT_PATCH = 23`. Since the listed export/import bodies are instruction-identical to the previously researched 29088 build, the bounded save tuple remains header `7`, `GameData 13 / patch 23`, `GameDesc 10`, and Galaxy runtime marker `1`. This is a narrow persistence-path comparison, not a claim that every game method or gameplay behavior is unchanged. Rebuilding the Plugin against the exact installed 29104 DLL and testing are separate requirements.

## Required product boundary

The existing Journal already has a first transition. The second migration must validate that original full Journal and transition at the ticket's durable checkpoint, append only `29088 → 29104` after native adoption, persist it before normal primary saving, then read back the new native save and issue a 29104 ticket. The historical 91 entries, origin, first transition, owned identity, save name, and ticket expiry must not be rewritten. An expired-primary prepare must still disclose the exact candidate and require the user's **subsequent** explicit confirmation before commit. Any failure after credential consumption remains manual reconciliation, not replay.

Offline method equality, compilation and tests do **not** constitute live migration, successful save/readback, future resume, production continuity, or complete 0.4 acceptance.
