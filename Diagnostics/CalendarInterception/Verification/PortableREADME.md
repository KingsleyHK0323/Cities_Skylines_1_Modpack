# Portable synthetic matcher checks

These authored tests use only the fake `SimulationManager`, synthetic reflection members and the source-only matcher. They do not include or load a game DLL, Harmony DLL or recorded game IL. The separate fake-method probe decodes the compiled fake method; it does not authenticate or inspect a real game method.

From the module root in a fresh PowerShell 7 process:

```powershell
$base = Join-Path (Get-Location).Path 'Diagnostics/CalendarInterception'
$fixture = Join-Path $base 'HarmonyFixture'
Add-Type -Path @((Join-Path $base 'CalendarIlMatcher.cs'), (Join-Path $fixture 'FakeSimulation.cs'), (Join-Path $fixture 'FakeIlProbeChecks.cs'), (Join-Path $base 'Verification/PortableMatcherChecks.cs'))
[PortableMatcherChecks]::Run()
[FakeIlProbeChecks]::Run()
```

A local run completed 12 portable matcher checks and nine fake-method checks. These cover unique, missing and duplicate shapes, wrong identities and operands, direct date-address and additional-store rejection, fake frame/date agreement, pause and a fake overflow. They do not establish valid control flow for arbitrary IL, source provenance, real Harmony patch behavior or game compatibility.

A separate local-only reference-text projection completed 24 checks using synthetic reflection members. The reference text, its metadata byte extracts and raw execution evidence are not part of a source-only backup. The actual Harmony fake-target study is described separately in the fixture documentation.

Signature: OpenAI-Codex
