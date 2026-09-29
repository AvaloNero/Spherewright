# 0.4.0 local package-surface preview (not release acceptance)

This is a local, clean-source packaging check of commit `222c51a7dc110538e7ad7224ad6c9f28da5e1706`. It does not replace the unfinished same-save production, Governor/readiness, cold-install, or final-release gates.

- The new protocol-only policy passed `37/37` offline cases. The source MCP initialized normally, advertised `64` tools and `1` resource, returned the same `91,253`-character Agent playbook as the source file, and exited cleanly on stdin close. Windows Core CI [run 36609466393](https://github.com/AvaloNero/Spherewright/actions/runs/36609466393) passed for this exact source commit.
- Full Release build during packaging completed with `0` warnings and `0` errors. The manual `win-x64` ZIP contains `238` manifest entries; its SHA-256 is `ff1ab340ce01a2cb9ffedbecd3a56b89a1a800548d90bdfbd18e6c5b498b20ba`. The package smoke test verified file hashes, MCP protocol startup/clean exit, `64` tools / `1` resource, merged 0.4 read-only/write metadata and options, and exact packaged/embedded playbook agreement.
- The Thunderstore candidate ZIP contains `12` entries; its SHA-256 is `dbf40be36edbc5b1bb1e27bfb0e1a4423300c184fd6831e485e60f4b123ca23b`. Static structure and source-commit binding passed; the Thunderstore runtime black-box check remains **not tested**.
- A pre-existing older local `0.4.0` ZIP was deliberately rejected by the new gate because it lacked `spherewright_get_foundry_plan`. That negative result must not be mistaken for a failure of the newly built ZIP. No package was installed into DSP, tagged, uploaded, or released by this check.

The two new ZIPs are local previews under `artifacts/local-preview-20260930-surface-222c51a/`. Later code or package-document changes require a fresh same-commit build and smoke check before any final candidate claim.
