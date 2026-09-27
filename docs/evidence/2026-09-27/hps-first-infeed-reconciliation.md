# First HPS infeed: successful construction, persistence not yet proved

## Reconciled execution

The previous ten-action window was explicitly sealed after commit `a228d90` and green CI. The later root clarification authorized only three fixed 2012/filter1003 attachments and one save after all three succeeded; it did not reset game revision or erase previous receipts.

The interrupted execution accepted exactly one build, action `901a11b8-e625-4bd9-90d8-348dce88bedf`. It completed successfully at tick `75194266`, uniquely creating sorter `5158` for `1976 → 1980`, with player item2012 `3 → 2`. The original runtime was cohort `f4fc8e5`, DSP `0.10.35.29088`. The new external audit window therefore contains **one accepted action**, not zero or four. The other two attachments and planned save have no submission evidence and must not be reported as completed.

Protected execution prefix: `action-cb0ddae316214bf399acfacc52895011-`.

| Receipt | Verified fact | SHA-256 |
| --- | --- | --- |
| `0012` action result | Successful terminal and one normal material deduction | `EA026ACB40FC5092811481358BFEADB816BD4AF81BB806B781F94536103F1D11` |
| `0017` sorter inspect | 5158, filter1003, pick1976/insert1980, two connection edges | `5A01F4296E0D71665B8A1038054DE18C7F6263A851D19478DB5BA6ACECE87506` |
| `0018` source inspect | 1976.slot5 reciprocates 5158.slot1 | `3B1F0160D182EEAA969482E3DB27949A21C209075A852AAC9782063646C2E4B4` |
| `0019` destination inspect | 1980.slot5 reciprocates 5158.slot0 | `2B633B6581E43D86FFD948D2E5FEB11A0F30236E5512932ABA4C80656D3C56B5` |

The existing belt-side slot4 connections to sorter1981 remain in both endpoint receipts. This is not an independent readback of sorter1981 itself, an entire-factory comparison, or a sustained-delivery result.

## Corrected observation interpretation

`sorterEndpoints` describes geometry for attaching a new sorter to the inspected object. It does not describe the existing pick/insert connection proof of an already-built inserter. Consequently, 5158 returning `native_sorter_slots_unavailable_or_over_limit` in that preview is not evidence that the successful build lost its connections. Root and an independent Sol review checked the reader implementation and the original terminal plus all three entity receipts. Existing connections are proved by pick/insert IDs and reciprocal `connections`; power and actual delivery remain separate checks. Native prepare rejections or missing actual connection evidence must still stop execution.

The MCP-embedded playbook in this build now states that distinction. Its resource regression passed with all seven sorter-guidance tests; the full offline Core/Contracts/MCP rerun passed 2242 tests (1999/59/184), and the full Release solution built with zero warnings/errors. This changes caller guidance, not the native construction algorithm. The guidance has not yet been cold-deployed or exercised by a newly installed MCP host.

## Current recovery boundary

The latest recorded post-build session is `R8`, owned/healthy, with Journal91 durable and no pending/error; the last endpoint sample is tick `75203871`. Its last normal save is still **73728691**, before this build. On September27, read-only process checks found no DSPGAME process; the retained descriptor was not live. No new bridge request, load, save or game write occurred during this reconciliation. The exact primary's ticket was still valid when checked, but validity does not prove it covers unsaved construction.

Do not load that older primary or replay sorter5158. Root has requested explicit authorization to inspect only the fixed LastExit candidate for the same owned identity and sufficient progress; no candidate has been read or approved here. The reason the game exited is not established. Recovery, persistence of5158, remaining attachments, normal save, full-stage audit and sustained HPS output remain open.

Root reconciliation record: `action-dabf99f21259493bb5e44755a7c84605-0001-root-hps-first-infeed-unsaved-reconciliation.json`, SHA-256 `353CC7EE6283355302EE79DD4885EC0200642B4DCA63D5E016E093BBF67C96B6`.

## Authorized fixed LastExit inspection

On September27 the user explicitly allowed the fixed-slot read-only check and asked not to repeat this permission barrier for equivalent checks. This supersedes the pending-inspection boundary above, not the load/overwrite rules. AGENTS now records bounded inspection of protected metadata and fixed recovery candidates without arbitrary save enumeration.

The offline inspection reused the existing `OwnedSaveRecoveryLease.Open`: bounded identity-prefix parsing, identity comparison before full-file hashing, 64MiB/2s limits and read-only file sharing. Two opens returned the same fingerprint. The candidate is the same protected identity, peaceful and native `0.10.35.29088`, at tick **74238107**. The ticket was unexpired at inspection. Its exact protected Journal matches identity/tracking/history/start tick, contains contiguous sequences1..91, and satisfies the ticket minimum91.

This candidate is newer than primary73728691 but **956159 ticks before the accepted construction** (about4h25m36s of game time at60ticks/s), and also below the last observed75203871. Therefore neither known save covers the successful prefix. Journal continuity alone does not prove world persistence. No native load, save, Bridge call, ticket change, alternate-save scan or construction replay occurred. DSP remains closed; accepted1 stays recorded.

The initial reflection invocation failed before opening the candidate because PowerShell wrapped a string as `PSObject`; direct typed invocation completed within the original read-only authorization. This is a local caller error, not a failed native recovery or reason for another permission prompt. The existing Release prefix/lease/recovery-policy regression run passed **74 tests**; that is offline evidence, not a successful game restore. A newer recovery point or an explicit informed rollback decision is still required before gameplay resumes.

Protected inspection record: `action-2fa42d47bd6e4ed4ab558b1550c8da7c-0001-fixed-lastexit-readonly-20260927.json`, SHA-256 `D9860D7072255BBF93822A5B23D13FF7FA05515789512819CC9C31B9B0558B56`. Root independently read that receipt and verified its hash and tick/coverage conclusions. The stable lease fingerprint is a composite evidence hash, not a claim about entity contents or a substitute for post-load entity reconciliation.
