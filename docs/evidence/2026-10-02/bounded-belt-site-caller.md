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
`prepare_build`. Three or four explicit spans, one candidate per interface,
14 requests maximum and a 180-second dispatch deadline checked between requests;
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

## Verification boundary

Offline PowerShell validation completed with `pwsh -File`:

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
