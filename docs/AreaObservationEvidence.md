# Area observation evidence — 2026-11-01

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Source: authorized Resource/Game_Data/Assembly-CSharp.dll, SHA256 DF557994A03C93D8BB4A14F80B1FBCC0FB29D547658442445D078D069BCDD9FE. Inspected method IL using existing dnlib; no game execution.

- GameAreaManager.CanUnlock(Int32,Int32): IL_0000–001D rejects coordinates outside [0,5); IL_001E–0030 checks area count against MaxAreaCount; IL_0031–0047 calls UnlockManager.Unlocked(area count); IL_0048–005A rejects owned tiles. IL_005B–0095 calculates cardinal adjacency. IL_0096–00A6 passes that result through AreasWrapper.OnCanUnlockArea and returns the modified value.
- AreasWrapper.OnCanUnlockArea: iterates its extension list and feeds each returned Boolean to the next extension. Registration order and live third-party changes are not established.
- CalculateTilePrice(Int32): calls CanUnlock before calculating; returns zero when eligibility fails. Zero is therefore not proof of a free purchasable tile.
- CalculateTilePrice(resource parameters): clamps the base-price index to the configured array, adds connection/resource/buildable-area terms, rounds, then calls AreasWrapper.OnGetAreaPrice. Its returned price is chained through extensions. No configured monetary values or affordability checks were captured.

Milestone number mapping, MaxAreaCount behavior, transaction affordability, thread safety and actual save behavior remain unverified. This is evidence for this assembly copy, not a live patched game.

DescribeObservation requires positive matching caller-owned lifecycle generations and coordinates in the observed 5x5 scope. The future owner must advance generations on unload/reload and observation invalidation; no automatic lifecycle adapter exists. Legacy Describe is input-only and has no freshness guarantee. Neither API grants purchase permission.

Pending cloud work, requiring separate approval: compile both C# files with a supported compiler; execute PreviewChecks; add missing-input and all coordinate-edge cases; verify lifecycle owner integration separately. No local checks were executed and no publishable game DLL is delivered.
