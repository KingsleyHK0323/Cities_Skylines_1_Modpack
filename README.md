# Automated Public Transport Vehicle Management

Version 0.1.0 is a read-only bus observation preview for Cities: Skylines 1. It does not control fleets, dispatch, depots, budgets, headways, or routes. APTP and AI Improve are not required. Optional cooperation is planned; no shared API is implemented.

## Use and current limitations

In the in-game Options menu, open this mod's settings and select **Refresh bus census**. The read-only field reports historical actual vehicle-chain counts for existing city bus lines. Refresh is manual and advances across simulation frames. Target counts and waiting passengers remain **Unknown**. Removed-prefab records may appear as InvalidPrefab with an unknown transport type. Long or invalid vehicle chains report Unknown. No playable-build acceptance is claimed until Steam Workshop subscribed-game testing is completed.

There is currently no Workshop item or ID and no subscription link. The approved icon concept is retained separately; release images must be verified before first upload.

## Validation and build

The foundation workflow checks documents; the branch-specific cloud-build workflow runs 13 pure observation checks on .NET Framework 4.8 and compiles the mod against .NET 3.5 game references on GitHub-hosted Windows. It validates the module assembly identity, nonempty output, output allowlist and SHA-256. Pure checks and compilation do not validate game runtime, UI, threading or mod compatibility.

Game references are restored from the authenticated encrypted supply at pinned commit 7b8a5e7c10f2db84388e8dd8af2084f239957b13. The project requires explicit GameReferencesRoot and never searches local game installations. References are not copied to output. No local runner, build, game deployment, automatic release, or Workshop upload is configured by this implementation. See docs/api-evidence.md.

After the first Release DLL passes file validation, the first Workshop item is created by a separately authorized manual upload. Its real ID is then recorded before configuring subsequent Actions/self-hosted upload updates. Cloud builds remain separate from local upload-only execution. Development downloads belong on GitHub; no Development Workshop item is created. Release downloads should provide the DLL and necessary files directly, without ZIP packaging.

## Licensing

Copyright Kingsley. Source-available code: PolyForm Noncommercial 1.0.0 (LICENSE). Original website content follows CC BY-NC-SA 4.0 under project policy; no website content is included here. The unmodified official license comes from polyformproject/polyform-licenses at commit 712d528014ccc8cb7d889be42fc136d9a3072b97, Polyform-Noncommercial.md.

## Repository

https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Automated_Public_Transport_Vehicle_Management

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management

Verified DLLs are retained for 14 days as Actions artifacts named vehicle-management-<run ID>-<attempt>, with build-provenance.json. This is a build handoff, not a GitHub Release or gameplay acceptance. Use the artifact ID for an exact download. Actions transport may wrap files in an archive; no ZIP is published as a module Release asset.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
