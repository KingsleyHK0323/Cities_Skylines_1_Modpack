# Harmony dependency options for Full Map

**Reviewed:** 2026-11-13, Hong Kong time. **Status:** decision support only; no package was downloaded, installed, enabled, executed or selected. The existing Full Map project has no declared Harmony runtime dependency, and the 25-area writer remains unavailable.

## Official version and asset evidence

At the review time, GitHub's [latest stable release page](https://github.com/pardeike/Harmony/releases/tag/v2.4.3) identified Harmony 2.4.3 as Latest. The releases list also contains newer Harmony 3 release candidates, which are pre-releases and are not interchangeable with Harmony 2. This is a dated observation, not a promise that 2.4.3 remains the latest release.

The official [2.4.3 expanded asset page](https://github.com/pardeike/Harmony/releases/expanded_assets/v2.4.3) displayed these SHA-256 digests for the complete ZIP assets:

| Official asset | SHA-256 displayed by GitHub |
| --- | --- |
| `Harmony-Fat.2.4.3.0.zip` | `2aad201cc048899e3e0d558cc08173d91cfa4b8a2d6d9a5231fca5c295081880` |
| `Harmony-Ref.2.4.3.0.zip` | `cbad8587290fc028d738187a03dd3a4a94fc3fc89e7ab9b6c4a8994d2b9683db` |
| `Harmony-Thin.2.4.3.0.zip` | `4ab9e3b66586db1ca365c84eaba672b1ae2ae8aa8ff060d9aee939a0a1441f2f` |

These are GitHub-displayed archive digests, not independently recomputed hashes or hashes of individual DLLs inside the archives. No archive contents were inspected. The same page listed source-code ZIP and TAR.GZ links without displayed digests, and no separate signature asset was visible there. This does not rule out signatures inside an archive or information elsewhere. The GitHub API endpoint was inaccessible through the available web tool.

## Distribution choices

| Choice | What the official material establishes | Decision and unresolved requirements |
| --- | --- | --- |
| Existing CitiesHarmony installation | A different local module declares `CitiesHarmony.API` 2.2.0; the reviewed local API DLL is a helper, not a verified Harmony patch engine. Its historical restore record lists `CitiesHarmony.Harmony` 2.2.2 as a compile asset, not a loaded runtime. | Reuse avoids obtaining a new archive only if a compatible runtime is actually supplied and loaded, which has not been established. Full Map would still depend on it at runtime. The API helper has possible installer, event and UI effects and must not be invoked as a purported read-only probe. |
| Official Fat ZIP | The versioned [Lib.Harmony project](https://github.com/pardeike/Harmony/blob/v2.4.3/Lib.Harmony/Lib.Harmony.csproj) produces the Fat-named ZIP; the official [repository README](https://github.com/pardeike/Harmony) describes Lib.Harmony as a dependency-merged distribution. | A reasonable first artifact for later review if an additional runtime dependency is expressly authorized. Check actual archive contents, version, dependent notices, host compatibility and interaction with already loaded Harmony before any use. A matching archive hash alone proves none of these. |
| Official Thin ZIP | The versioned [Thin build definition](https://raw.githubusercontent.com/pardeike/Harmony/v2.4.3/Lib.Harmony.Thin/Lib.Harmony.Thin.csproj) declares a MonoMod.Core dependency; the official README says Thin requires the consumer to supply its dependencies. | Offers dependency control but adds availability and version-coordination work. No dependency set or runtime behavior has been verified. |
| Official Ref ZIP | The versioned [Ref build definition](https://raw.githubusercontent.com/pardeike/Harmony/v2.4.3/Lib.Harmony.Ref/Lib.Harmony.Ref.csproj) specifies `ProduceOnlyReferenceAssembly` and a `netstandard2.0` target. | Reference-only output is not a runtime patch engine. Its build target alone does not prove that it can compile the module's .NET 3.5 target; the released ZIP contents were not inspected. |

The Full Map project targets .NET Framework 3.5. Harmony 2.4.3's [build configuration](https://github.com/pardeike/Harmony/blob/v2.4.3/Directory.Build.props) lists `net35`; the [official runtime guidance](https://harmony.pardeike.net/v2/articles/intro.html) discusses Mono/Unity and notes limitations for the Unity .NET Standard profile. A listed target does not prove that the user's actual game runtime, architecture, installed mods or chosen archive are compatible. Harmony's own [shared-state source comment](https://github.com/pardeike/Harmony/blob/v2.4.3/Harmony/Internal/HarmonySharedState.cs) warns that Unity Mono may load only the first of multiple same-named assemblies.

## Licensing and remaining boundary

The version-pinned official [Harmony 2.4.3 license](https://raw.githubusercontent.com/pardeike/Harmony/v2.4.3/LICENSE) identifies the MIT License and requires retaining its copyright and permission notices in copies or substantial portions. Before any redistribution, inspect the actual selected package and all included or separately required third-party notices; none was downloaded for this review. This research does not change the project's existing license.

Keeping the normal build entirely free of Harmony references is possible only if all Harmony-dependent source and dependency declarations are excluded from that build; an actual resulting assembly must be checked before asserting this. A default-false runtime flag alone does not remove a compiled assembly reference. Reusing another mod's loader or calling Harmony by reflection still introduces a runtime dependency when the feature is enabled.

No option establishes the exact call-site patch, matching after other transpilers, reentrancy safety, the live city's milestone values, game-thread/lifecycle identity, safe unpatching or real game behavior. Until those are resolved and a particular dependency and game operation are separately authorized, keep the native writer and any Harmony patch disabled. See [the call-site assessment](25_Area_Minimal_Integration_Design.md#harmony-2-call-site-assessment--2026-11-13).

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

## Evidence and build-scope clarifications

The repository README linked above is the live `master` README, not a version-pinned 2.4.3 README; the 2.4.3 project definitions are separately version-pinned. None establishes the contents of the released ZIP without inspecting it.

The statement about excluding declarations concerns a normal build with no Harmony build or restore dependency. A different claim is that a particular output DLL has no Harmony `AssemblyRef`: a declared but unused compile reference is not necessarily emitted as an assembly reference. Verify the actual output metadata and loader behavior before making either claim. A runtime flag alone does not remove an assembly reference that was actually emitted.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

## Future verification of the official Fat asset

1. Only after separate explicit user approval, download the named `Harmony-Fat.2.4.3.0.zip` from the official Harmony 2.4.3 release into a designated isolated directory inside the project.
2. Before opening the ZIP as an archive, listing or inspecting its contents, extracting it, or executing or installing anything from it, compute SHA-256 over the complete ZIP bytes and compare it with the official digest above for that exact version and filename. Reading raw bytes for hashing is part of this comparison.
3. If the digests differ, stop immediately and preserve the ZIP and the expected and observed digests as evidence. Do not retry or replace the artifact as part of this sequence.
4. A matching digest confirms only that the downloaded archive bytes agree with the official displayed SHA-256. It does not establish the archive contents, a publisher signature, third-party licenses, or Unity/Mono compatibility; those require separate review.

No download was performed as part of this plan.

Signature: OpenAI-Codex_release_isolation

## Later authorized archive inspection

After the dated decision-support review and separate authorization, the official Harmony-Fat.2.4.3.0.zip was downloaded once. Its complete archive SHA-256 matched 2AAD201CC048899E3E0D558CC08173D91CFA4B8A2D6D9A5231FCA5C295081880 before ZIP inspection. The 45-entry inventory contained one net35/0Harmony.dll, extracted to an isolated research directory with SHA-256 C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503. PE metadata reports assembly version 2.4.3.0 and references to mscorlib 2.0, System 2.0 and System.Core 3.5; no TargetFrameworkAttribute was found.

The archive inventory contained no filenames matching license, licence, notice, copying, copyright or third-party. The earlier review of the official versioned Harmony MIT license does not settle the licenses and notices of the Fat package's merged contents. A bounded attempt to decode the embedded ILRepack.List as strict UTF-8 failed, so no component list was inferred. Do not redistribute the archive or extracted DLL on the strength of this review.

Harmony was subsequently executed only in isolated Windows .NET Framework fake-method checks. That is not evidence of Unity or Mono compatibility, safe interaction with other game patches, or permission to install or use a game patch.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion