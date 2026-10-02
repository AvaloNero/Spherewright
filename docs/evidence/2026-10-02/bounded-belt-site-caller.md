# Fixed belt-site qualification caller

Scope: external PowerShell only; the Plugin/MCP tool surface and native placement,
material, identity, action and ten-write rules are unchanged. This is not a game
construction or release milestone.

## Proven caller limitation and replacement

The previous one-shot caller encoded exactly A/D/H. It could not express the
root-approved four-span graphite-route qualification. Luna stopped before any
Bridge request rather than improvising an executor or dropping a required span.

Reuse `scripts/SpherewrightStageTools.ps1`:

```powershell
# Action client, then the existing protected transport, then stage helpers.
# ApprovedPlan is an explicitly approved local JSON object, not a planner.
Invoke-SpherewrightBeltSiteQualification -ApprovedPlan $approvedPlan `
    -ExpectedRevision $freshApprovedRevision -AcceptedBefore $externalAccepted `
    -MinimumDurableSequence $durableFloor -RecordEvidence $protectedRecorder
```

The function dispatches only session/player/entity/Journal reads and
`prepare_build`. Three, four or five explicit spans, one candidate per interface,
16 requests maximum and a 180-second dispatch deadline checked between requests;
each in-progress call retains the existing transport timeout. No commit, Move,
save, exit, resume, fallback or retry. It binds actual entity IDs/slots, fresh player and endpoint hashes, native
path/layer/material echoes, exact attachment results and contiguous returned
endpoints. It checks the 30 m endpoint chord, not summed polyline length.
Ordinary crossing tokens are never returned or sent to the evidence callback.
Raw responses remain in the existing credential-redacting protected transport.

First failure stops subsequent prepares and performs bounded health-bound closing
reads. A lost or invalid closure is reported separately, not converted to a pass.
External accepted, revision, normal-save tick and Journal stay distinct. Site
positives never prove the future actual-ID cover join or authorize construction.

The fifth-span allowance is a narrow external-caller correction: a complete
source-to-side-entry route can require three separate crossings between its two
ramps, each still subject to the native 30 m chord and 64-point limit. The caller
does not choose, split or generate that route. Six spans, insufficient closure
budget and more than 16 requests fail before transport. This changes no Plugin
tool, game permission, native geometry, ten-write boundary or lifecycle behavior.

## Verification boundary

The original three/four-span change passed offline PowerShell validation with
`pwsh -File`:

- `scripts/test-belt-site-qualification.ps1`: 40 checks passed, zero real game
  calls; 2.891 seconds.
- `scripts/test-stage-tools.ps1`: 26 passed, zero game calls; 3.212 seconds.
- `scripts/test-production-sampling.ps1`: 27 passed, zero game calls; 3.081
  seconds.

A bounded streaming Claude Code review of the shared helper and fixture reached
`APPROVE` with no blocking findings in 269.843 seconds. Its non-blocking coverage
note: the helper's runtime request/time-exhaustion guard is not exercised
(`request_budget` only tests plan validation); the elapsed-time branch and the
combined primary-failure/closure-failure path are also untested. Individual
request duration continues to rely on the existing transport timeout.

These results cover offline fixtures only. This section does not assert a live
qualification result, DSP placement, installed cohort, production, package or
restart proof; live qualification evidence is tracked separately.

## Narrow virtual-belt endpoint correction

A fresh observation exposed a caller-only restriction: the installed native
preview already supports a belt direction with `slot=-1`, but this shared caller
accepted only nonnegative built-device slots. An existing automatic supply belt
could therefore not be qualified through the normal shared entry point.

The external helper now accepts exactly observed item2001 `belt_virtual` endpoints:
four unique direction indices0–3, all with slot-1 and occupancy/connection fields
still null. It selects the explicitly approved direction, checks every endpoint
pose against the actual belt, and requires the native attachment echo to match
item2001, direction, slot, hash, filter, material budget and `Ok`/span2. Null
occupancy is **not** treated as free; a performed, positive native check remains
mandatory. Ordinary built-device free-slot checks are retained. Missing or
invalid explicit direction data fails closed; old device plans without the
explicit zero `existingBeltQuarterTurns` must be prepared anew.

No Plugin/MCP change, new building type, game write, retry, autonomous planning,
save or lifecycle action was added. This does not require a DSP cold deployment.

The directly related offline run passed:

- `scripts/test-belt-site-qualification.ps1`: 69 checks, zero real game calls;
  3.218 seconds. Covers both roles with nonzero virtual directions, rejected
  identity/schema/pose/occupancy data, wrong native echoes and an unperformed
  native check; original built-device checks remain.
- `scripts/test-stage-tools.ps1`: 26 passed, zero game calls; 3.095 seconds.
- Both edited PowerShell files: zero AST errors.

The streaming Claude Code review reached `approve — no concrete blockers found`
(`is_error=false`) in 331.774 seconds. Some virtual negative variants remain
source-side only; destination rejection, nonselected-point displacement and
explicit occupied-true variants are not separately covered. Fixtures do not
prove real Bridge/native placement. Live qualification, continuous allocation,
future actual-ID joins and the complete1210 chain remain separate acceptance
work; no such result is claimed by this correction.

The bounded five-span follow-up passed offline validation only:

- `scripts/test-belt-site-qualification.ps1`: 80 checks, zero real game calls;
  3.10 seconds.
- `scripts/test-stage-tools.ps1`: 26 passed, zero game calls; storage 28,
  material 41, successful fixture requests 14, material fixture requests 20;
  2.77 seconds.
- Both PowerShell files parsed with zero AST errors and no `Import-Module`
  statements. These fixtures do not establish live/native qualification.

The streaming Claude Code review terminated with `BLOCK` (`is_error=false`) in
331.676 seconds. Its sole finding assumed the dispatched span order was
`[A,H1,H2,H3,D]`; the fixture actually initializes `[D,A]` and appends
`[H1,H2,H3]`, so prepare number three is `H1`, matching the assertion and the
80-check result. Root independently checked that source order and classified
the finding as based on a misread; this note does not recast the external
review terminal as APPROVE. No game, Bridge, install, save, load or deployment
operation was part of this offline change.
