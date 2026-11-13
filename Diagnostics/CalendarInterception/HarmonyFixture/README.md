# Harmony calendar fixture and 1x candidate assessment

The calendar requirement is one simulated minute per real minute at 1x. Which intervals are eligible during pause, loading, suspension or interruption, and whether to catch up afterward, are separate policy and continuity questions. Day/night phase and anchoring are separate decisions.

## Current result

A real Harmony 2.4.3.0 assembly was loaded only into a fresh 32-bit .NET Framework fixture process. Nineteen checks passed for a synthetic `SimulationManager`: unique instruction matching, cloned instruction metadata, a passive addition helper, frame/date agreement, a fake pause, a fake exception and owner-scoped unpatching. Twelve of those checks ran before patching, six after patching the fake target and one after owner-scoped unpatching. Patch-install failures, unpatch failures and interference from another owner were not injected. Signature: OpenAI-Codex. The compiler emitted 13 CS1685 duplicate-type warnings. The helper returns the original `DateTime + TimeSpan`; it does not implement 1x time. The fake pause and stop-after-error guard are fixture behavior, not a game safety mechanism. The artificial label and exception-block checks test copying, not full control-flow validity. The test does not validate Unity/Mono, actual game events or saves.

The model sources provide separate pure arithmetic examples. They do not patch the fake target and cannot commit a clock. Their 23 checks include monotonic metadata at an algebraic boundary, frame conversions, pause assumptions, fail-closed inputs and negative controls for raw-display reversal and mixed saves. The positive fake-method probe has nine checks and obtains its IL from the compiled fake method. It is distinct from a projection of the game reference IL text; neither proves a live game method or patch chain.

## Product decision

No native writer is connected or enabled. The current evidence is insufficient to offer a product candidate that can safely maintain 1x, frame/date conversion, nondecreasing display, pause handling and conservative stop together. Two concrete approaches illustrate the gap:

| Approach | Conditional benefit | Unresolved consequence |
| --- | --- | --- |
| Keep native frame duration `P`, change the date increment from `P` to `d`, and set `O' = O + d - P`. | At a complete boundary, `M = F * P + O` can be retained. | A separate reader can see intermediate values; for the reference `P = 147.65625` seconds and `d = 1` second, the model shows a fixed-reference raw display moving backward by 146.65625 seconds. |
| Change `P` and re-anchor using `O' = O + F * (P - P')`. | The date associated with the current frame can be retained at the selected boundary. | A recorded event frame `E` changes its interpreted date by `(E - F) * (P' - P)`. A half-rate synthetic example still moves the raw display backward by 36.914064 seconds. |

Event rescheduling may be an explicitly disclosed product effect; it must not silently corrupt or duplicate records. Neither option has established a coherent commit for every relevant reader or save path. A save can mix an older metadata date with a newer frame even when reconstructing an offset makes the equation appear consistent. The inspected queued save route and private frame lock are conditional evidence for one path, not proof that all readers and saves share a lock.

A future candidate needs an authenticated session and verified monotonic interval, explicit pause/multiple-frame and overflow handling, consistent date and frame conversion, a nondecreasing raw display, and conservative behavior when a frame or save may be partial. It must not infer continuity from matching endpoints, retry uncertain writes, or assume that a postfix ran after every exception. An eligible 60-second synthetic interval is an example, not evidence that actual throughput reaches 1x.

## Fixture boundary

`RunPortableHarmonyFixture.ps1` accepts the authorized DLL and output-root paths as parameters, checks the expected DLL hash, and builds a fresh x86 fixture under the supplied module-local validation directory. A caller must authorize the exact DLL path, lock and verify the launcher itself, and invoke it in a fresh process. The launcher must not be used to choose or patch a real game method. Output binaries and run logs are local test artifacts, not release assets. The approved Harmony DLL is not included in this module or release; its official ZIP did not provide license and third-party notices in the inspected package.

The final path of the open DLL handle could not be obtained in the test environment. Ancestor checks and locked-byte hashes narrow the risk but leave a check/open race. Patch-chain changes, other modification mechanisms, labels and exception blocks, actual native lifecycle and unloading behavior remain unresolved.

Signature: OpenAI-Codex

Path parameters remove machine-specific private paths, but do not make the launcher universally portable. It requires Windows x86 .NET Framework and pins the approved Harmony, fixture-source and local compiler/reference hashes. A framework update or checkout newline conversion can cause a conservative refusal; do not bypass hash checks without independently verifying and approving replacement inputs. CLR default probing can occur before the host's resolver; the experiment uses a fresh isolated output directory and detects unexpected Harmony or named game assemblies, but this is not a guarantee about all possible external code.

Signature: OpenAI-Codex
