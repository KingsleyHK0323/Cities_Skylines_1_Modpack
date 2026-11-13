# Isolated calendar IL matcher

This source-only diagnostic locates the observed `SimulationManager.SimulationStep` date-addition shape in an instruction snapshot. It has no Harmony or game-assembly reference, does not install a patch, and is not included in the checked normal game or controlled-host source lists. It cannot control time. The implementation and checks can be compiled together in memory with PowerShell 7 `Add-Type`; no game assembly is loaded by the checks.

## Contract

A future adapter supplies an independently trusted `SimulationManager` runtime type, the original `MethodInfo`, and one stable, ordered array of `CalendarInstruction` values. Each value carries the `OpCode` and operand from the corresponding input instruction. For example, an adapter can construct each entry from a Harmony `CodeInstruction` object's `opcode` and `operand`; ~~the adapter and Harmony are not included here.~~ A fixture-only adapter exists in [HarmonyFixture](HarmonyFixture/README.md); no production adapter or Harmony DLL is included. Signature: OpenAI-Codex. A `Unique` result gives the index of the addition call in that same ordered input, not an IL byte offset.

The matcher checks the observed private, non-static, non-generic, parameterless `void` method shape, unique same-name method, expected public instance fields, actual `FieldInfo`/`MethodInfo` identities and the exact eight-instruction sequence. It rejects zero or multiple exact matches, a second direct target date store or matching `DateTime.op_Addition` call, any direct date-field address load, unknown opcodes, missing operands, unresolved member operands and unsupported inline signatures. This is deliberately conservative and may refuse otherwise usable input.

A result does not authenticate the supplied type, method or instruction provenance; the adapter must bind them to the authorized runtime method. The matcher copies the array only for its own scan: callers must prevent concurrent mutation and apply any index only to the unchanged list from which that snapshot was made. The check does not verify all operand types, branches, stack, labels or exception blocks; it cannot rule out indirect writes or other ways to change dates. A later transpiler can alter the result or force a rebuild. Preserving and validating the original instruction labels and exception blocks, handling other patches, and proving date/display/save coherence are separate unresolved responsibilities. A match must never by itself enable a native writer.

## Evidence and checks

<!-- [Reference metadata evidence](Verification/ReferenceMetadataEvidence.md) --><!-- [Reference verification overview](Verification/README.md) records the verified reference file hash, method and field metadata, and target IL bytes. -->[Portable verification](Verification/PortableREADME.md) describes self-contained fake checks; local reference extracts are not distributed. <!-- [Date-field address evidence](Verification/DateAddressReferenceEvidence.md) --><!-- [Date-field verification overview](Verification/README.md) records the named reference-text inspection. -->The local reference-text inspection is not distributed in a source-only backup. The isolated check reads only a caller-specified reference IL text file. It maps the target operands to synthetic runtime members and uses placeholders for unrelated members; its positive result does not prove that those members came from the real method or that a Harmony adapter works.

On 2026-11-13, a fresh PowerShell 7.6.9 process compiled the matcher and check source in memory and completed 20 checks, including altered or repeated shapes, unresolved operands and invalid method/field signatures. A later extended run passed 22 checks. A then-final run passed 23 checks. A subsequent <!-- [24-check run](Verification/LocalCheckEvidence-24.md) --><!-- [24-check overview](Verification/README.md) -->24-check local reference projection passed, including the date-address example. Unrelated field, method, branch and numeric operands in the text fixture are represented by synthetic placeholders, not decoded live Harmony operands. ~~No Unity or game behavior, Mono compatibility, Harmony integration, save/load or native clock write was tested.~~ A later Harmony test exercised a fake method only; real game, Unity/Mono, save/load and native clock writes remain unvalidated. Signature: OpenAI-Codex.

Signature: OpenAI-Codex

## Adapter sketch

The following is an illustrative fragment, not an installed patch. `stableInstructions` is a caller-owned ordered list of Harmony instructions, `original` is Harmony's original-method argument, and `trustedManagerType` must be independently authenticated. All three must describe the same runtime target; the matcher cannot verify that binding.

```csharp
var snapshot = new CalendarInstruction[stableInstructions.Count];
for (int i = 0; i < snapshot.Length; i++)
    snapshot[i] = new CalendarInstruction(stableInstructions[i].opcode, stableInstructions[i].operand);
CalendarMatch match = CalendarIlMatcher.Find(trustedManagerType, original as MethodInfo, snapshot);
```

A future adapter must use the unchanged list and validate labels, exception blocks, final patch ordering and all runtime safety conditions separately. This fragment has not been compiled against Harmony.

## Reproduce the isolated checks

**Source-only alternative:** The <!-- [verification overview](Verification/README.md) -->[portable verification](Verification/PortableREADME.md) uses an authored synthetic fixture. The historical command below requires local reference evidence that is not included in a source-only backup.
Signature: OpenAI-Codex.


From the module root in a fresh PowerShell 7 process, the local text fixture can be checked without loading the game reference assembly:

```powershell
$dir = Join-Path (Get-Location).Path 'Diagnostics/CalendarInterception'
$fixture = Join-Path (Get-Location).Path 'Local_Validation_20261113_CityTime/day-night-il-20261113-01/simulation-manager-il.txt'
Add-Type -Path @((Join-Path $dir 'CalendarIlMatcher.cs'), (Join-Path $dir 'Verification/CalendarIlMatcherChecks.cs')) -IgnoreWarnings
[CalendarIlMatcherChecks]::Run($fixture)
```

Signature: OpenAI-Codex
