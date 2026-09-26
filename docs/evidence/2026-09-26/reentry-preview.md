# Same-primary re-entry preview — no load

- Source baseline: `346087b`; installed cohort: `f4fc8e5`; native DSP: `0.10.35.29088`. This is not a new package deployment.
- The game was closed. One normal Steam launch reached the unloaded, unowned main menu; writes remained disabled.
- One fresh `reauthorize_expired_primary` prepare passed evidence-v2 identity, same-version and exact checkpoint checks: candidate/minimum tick `73573821`, durable Journal `91`. Only `USER_CONFIRMATION_REQUIRED` blocked commit.
- The disclosure was presented and the user subsequently explicitly confirmed this exact checkpoint. At the preview boundary there was **no commit, load, game save or construction**; accepted remained `6`, and the old ticket was unchanged. Confirmation alone is not a successful load. Old September 24 actions were not replayed.
- Private evidence index: `action-10a705878d36482284d045e63a3ceb89-0005-expired-primary-reauthorization-preview.json`. Plan credentials, disclosure digest, save name and absolute paths remain private.
- A caller's optional post-run summary failed because its helper was undefined. Reading the completed protected result established success; the prepare leaf was not rerun.

Next: protected recovery using a fresh bound plan and a fresh read-only factory audit before any construction. This preview is not recovery or production evidence.
