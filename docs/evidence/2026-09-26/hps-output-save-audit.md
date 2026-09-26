# West HPS D.5/D.6 output attachments and save audit

## Scope

This record closes only the last three accepted actions in the frozen west-HPS window. Its fresh baseline was `R1 / save73573852 / J91 / accepted7`; the preceding seven actions remain anchored to their already reviewed protected records and were not replayed or re-collected here.

The installed runtime cohort for this stage was `f4fc8e5` on native DSP `0.10.35.29088`. That identifies the deployed runtime used for the evidence; it is not a claim that an arbitrary source HEAD was installed.

## Execution and final boundary

- D.5 build action `bf4044bb-bb6b-447c-a2d8-89760d80a863` completed at tick `73726405`, uniquely creating basic sorter `5156` with filter `1105` on `5115.slot7 → 5156 → 5137`.
- D.6 build action `9696c2a8-480d-4f42-988c-6e805ddcf625` completed at tick `73727885`, uniquely creating basic sorter `5157` with filter `1105` on `5151 → 5157 → 843.slot10`.
- Normal save action `8b12ebf9-87de-4ed7-8320-98182c6b74d2` completed at tick `73728691`. The resulting boundary is `R6 / save73728691 / J91 / healthy / accepted10`; the player delta for this suffix is only `2011 −2` and no accepted action was replayed.

Protected execution completion: `action-95051b10385546a1b34019c1a094220a-0071-hps-west-stage-d-output-sorters-save-three-complete.json`; protected summary SHA-256 `2786F661709328ED6B858334674D44FBBA6127CD7F24F629DA1AD20AEA3BDA19`.

## One collection and deterministic comparison

Terra ran one post-save, read-only collection: 67 calls in `17.4451089` seconds. It captured 52 nonempty built pages for 5157 entities and a separate empty prebuild response; the latter is not a 53rd built page. Manifest: `action-4c6f1773eb1b47e2882b54431a6e944c-0068-hps-west-stage-d-ten-read-collection.json`, SHA-256 `8E2222A3D2C071D3691904BD3861620100A8A014D697CE1FAD3B87AF2E088940`.

The fixed offline comparison is limited to the `accepted7 → 10` suffix against `action-b27473ea40fb462d8fe2417e93d63469-`. It confirms 5155 → 5157 entities, exactly new IDs `5156/5157`, reciprocal directed edges 10042 → 10050, zero prebuilds, the two declared attachment chains, matching original/fresh terminals, and J91 identity plus all durable entries unchanged except capture tick. After removing only the four expected new reciprocal edges, the existing entities retain their static identity, pose, configuration and topology. Comparison proof: `action-91bdc2bbb78148eca4fee37d0e882b00-0001-hps-west-stage-d-ten-terra-static-comparison.json`, SHA-256 `CE4EDF68090AE11041DF18879972ED3CBCD3B660F40DB421BF1691F1C3EFF936`.

Dynamic configuration hash, buffer/cargo contents, progress, generation, and required-energy values are not cross-tick equality fields.

One earlier local entry guard rejected before any bridge call because descriptor `pluginVersion` is only product version `0.4.0`, not cohort provenance. The corrected binding uses the fixed Plugin DLL hash/ProductVersion and bridge identity together; that 0-call/0-write guard is not a native construction result. The transferable rule is summarized in the [experience ledger](../../experience-ledger.md).

Sol's final acceptance closes the cumulative frozen `0 → 10` window by referencing the preceding seven independently reviewed actions plus these three terminals. It reports cumulative material net `2001 −16`, `2203 −2`, `2011 −2`; final proof `action-e7688e34ed4644fcb9c1e4901c555801-0001-hps-west-stage-d-ten-sol-final-acceptance.json`, SHA-256 `4FE98CE527412D5E1DEAB8F4845CA83A9857D9B2B376A93B09EC7EC0F67E2764`, last audit tick `73746042`.

## Boundary

Accepted `10` remains frozen until an explicit root seal; documentation and CI do not reset it. This does not prove sustained HPS throughput, continuous power or fuel, production rate, restart/resume, or a general factory-health result.
