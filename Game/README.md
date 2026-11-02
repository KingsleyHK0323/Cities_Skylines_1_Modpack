# Observation-only game assembly candidate

This local candidate adds the game's `IUserMod`, loading, and threading entry classes. It has not been compiled, loaded, or accepted in gameplay. Its description explicitly states that it does not change game time or speed. Enabling it does not implement the approved 1:1 clock or decimal acceleration.

The reference copies were inspected as PE metadata only, without loading or executing game assemblies. They expose `IUserMod.Name/Description`, `LoadingExtensionBase.OnLevelLoaded/OnLevelUnloading/OnReleased`, `ThreadingExtensionBase.OnAfterSimulationTick`, `SimulationManager.m_currentGameTime`, `m_currentFrameIndex`, and the two pause getters. `Singleton<T>.exists` is checked before requesting `instance`. Reference identity is not a verified installed game version or evidence of callback ordering.

The game reference metadata uses CLR 2.0 and mscorlib 2.0.0.0. Consequently the game candidate compiles only `GameClockState.cs` and `GameMod.cs`; it excludes the .NET 8 harness and the BigInteger-based pure model. No System.Numerics dependency or bundled game references are introduced. Actual compiler compatibility and game loading still require validation.

## Cached observations

Only an active gameplay load mode enables capture. Unloading/release clears the cache. Capture reads on `OnAfterSimulationTick`; failed reads replace values with unknowns. A load-generation token prevents a sample captured during an old city from publishing into the new city. Returned snapshots are immutable and remain internal to this assembly; there is no new cross-mod API or persistence format.

A cached sample is not necessarily fresh: paused games may not receive simulation ticks. No UI, external consumer, automatic elapsed-time inference, or model advancement is connected. Stopwatch counter units are explicitly normalized into TimeSpan ticks before caching. These values are observations, not applied game time. Runtime callback ordering, threading, pause coverage, and save/load behavior remain unverified.

## Main-owned cloud integration checklist

No workflow is changed by this batch. Main must separately approve and wire:

1. Obtain the already approved reference copies through the existing protected mechanism; do not upload or log their contents.
2. Select the approved hosted compiler's `csc.dll`, provide a fresh output directory, and invoke the following cloud-only script:

   ```powershell
   ./Game/Build-GameAssembly.ps1 -CompilerPath $compilerPath -ReferenceDirectory $referenceDirectory -OutputDirectory $freshOutputDirectory
   ```

3. The script passes `/noconfig /nostdlib+ /target:library /langversion:7.3` and explicit reference paths to Roslyn. It uses no game project restore, external package, SDK installation, reference copying, release upload, or Workshop operation. It refuses an existing DLL output. Script syntax validation alone does not establish compilation.
4. Run the existing .NET 8 harness, now also including the actual `GameClockState.cs` and its lifecycle cases. Those cases do not compile `GameMod.cs` or prove native callback integration. Separately compile and inspect `RealisticCityTime.dll` for the game entry classes, no forbidden clock writes, and intended assembly references.
5. Only after approved cloud build and distribution can a Steam Workshop subscribed build establish game loading/observation acceptance. No Workshop identity or distribution is configured here.

Remaining product gates include decimal input precision, interruption/catch-up policy, per-city activation, and a CLR-compatible bridge to the pure conversion behavior. The observer never sets date offsets, simulation speed, population schedules, or aging.

Signature: OpenAI-Codex_Realistic_City_Time
