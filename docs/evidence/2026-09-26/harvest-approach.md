# Harvest approach watchdog — offline implementation boundary

## Scope

The current implementation adds a watchdog only to the **approach** portion of an already accepted normal harvest order. It does not add a tool, route planner, teleport, retry loop, or gameplay execution.

## Native signal basis

The current referenced DSP `0.10.35.29088` `Assembly-CSharp.dll` (SHA-256 `C43A484F6ADF8A9E4B956156047070891B46860D5B5C707BA1377B6A2AF25732`) was directly checked for `PlayerOrder.ReachTest(OrderNode)`: it resets `OrderNode.targetReached` each tick and sets it when the player reaches the order target; for `Mine` it also sets the field once the player is within the native object-versus-approach relationship. The existing interplanetary-flight adapter already reads that same native field for its owned order. The harvest adapter therefore treats `targetReached` as an approach-complete signal only, not as a completed yield.

## Behavior and tests

- `HarvestApproachProgressWatchdog` wraps the established 180-tick physical-progress and 600-tick route-progress thresholds against the exact `CalculateMiningApproach` point captured at commit.
- The approach monitor retires permanently once the native order reaches that point, an inventory yield is observed, or a **successful resource inspection** shows positive node reduction. An unrelated failed inspection is not treated as observed node reduction or a false progress signal.
- Energy starvation pauses only approach monitoring; recovery resets its window. The pre-existing `max(7200, estimatedTicks * 8)` harvest deadline remains checked before that pause can return.
- Targeted Core tests cover the 180-tick no-movement bound, 600-tick circular route bound, permanent retirement for approach/yield/reduction signals, recovery reset, and structured recovery advice tick/distance fields. They do not constitute live DSP harvesting validation.

Verification: **8/8 targeted Core cases**, one embedded-MCP-guide regression, full Release build **0 warnings / 0 errors**, and **2234 Release tests** (`1992 Core + 59 Contracts + 183 MCP`) passed. Independent Sol review checked the approach-only lifetime, failed-inspection guard, energy reset and deadline ordering. The helper tests do not exercise a real Plugin mining scene; native stall/recovery acceptance remains open.

The package playbook records the same bounded-recovery rule. No deployment, game call, or fresh harvest action is part of this evidence slice.
