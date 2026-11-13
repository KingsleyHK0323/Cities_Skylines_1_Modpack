# Independent Development artifact branch contract

Status: locally implemented and tested with isolated fixtures. Main Session owns
the publisher workflows. Artifact branches are the only player-facing module
source. Legacy Release/bundle code stays internal for historical compatibility;
this App change does not publish, remove or modify any Release.

## Layout and manifest

Publish each module independently to `Development_Build/<source branch>` in
`KingsleyHK0323/Cities_Skylines_1_Modpack`. Commit the complete required file set
and `manifest.json` together. No ZIP, Release, asset ID or release ID is required.
The App reads the branch ref, pins its full artifact commit and requests all
remaining contents at that commit. The manifest must not try to contain its own
artifact commit, which would create a self-reference.

Example for AI Improve (replace sizes, hashes, source SHA and run with real data):

```json
{
  "schema": 1,
  "sourceKind": "artifact-branch",
  "moduleId": "ai-improve",
  "repository": "KingsleyHK0323/Cities_Skylines_1_Modpack",
  "sourceBranch": "AI_Improve",
  "artifactBranch": "Development_Build/AI_Improve",
  "sourceSha": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "assembly": "AIImprove",
  "workflowPath": ".github/workflows/cloud-compile-test.yml",
  "runId": 123,
  "attempt": 1,
  "buildId": "AI_Improve-dev-123-1",
  "devTools": true,
  "files": [
    { "path": "files/AIImprove.dll", "name": "AIImprove.dll", "assemblyName": "AIImprove", "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
    { "path": "files/CitiesHarmony.API.dll", "name": "CitiesHarmony.API.dll", "assemblyName": "CitiesHarmony.API", "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
    { "path": "files/LICENSE", "name": "LICENSE", "assemblyName": null, "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
    { "path": "files/CitiesHarmony-LICENSE.txt", "name": "CitiesHarmony-LICENSE.txt", "assemblyName": null, "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
    { "path": "files/THIRD-PARTY-NOTICES.md", "name": "THIRD-PARTY-NOTICES.md", "assemblyName": null, "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
    { "path": "files/README.md", "name": "README.md", "assemblyName": null, "size": 123, "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
  ]
}
```

`path` is a safe relative path inside the artifact commit; `name` is the flat
installed file name. Both are unique case-insensitively. DLLs declare their real
assembly names. Versioned paths such as `builds/<runId>-<attempt>/<filename>` are
supported. Optional `signature: "OpenAI-Codex"` is accepted as author metadata;
it is not treated as a cryptographic signature or a trust credential. README,
Workshop-link text and JSON/text build-info files can be included in `files` with
`assemblyName:null`. The publisher validates its existing SHA256SUMS before
generating manifest hashes; the App independently hashes every listed file.
The bare name `SHA256SUMS` is not currently an installable asset type and need not
be listed: the manifest itself carries the per-file hashes.

Supported legal/data files use JSON `null`. The main DLL must be
`<assembly>.dll`. Known registry-required dependencies and notices cannot be
omitted. Source branch/module/repository/known assembly/workflow identities must
match the App registry. Previously unmapped registered modules can provide their
assembly/workflow mapping in this publisher-controlled manifest; this does not
create new registry entries or infer inter-module dependencies.

The manifest is limited to 256 KiB; at most 256 nonempty files, 32 MiB per file and
64 MiB per module. Script/executable assets and configured game reference DLLs
are rejected. Only listed files are installed; the branch is not extracted as a
whole. LFS pointer text does not satisfy the declared DLL hash/assembly checks;
automatic LFS resolution is not implemented.

The existing blocked reference list is applied case-insensitively to both the
installed DLL name and declared assembly identity, including renamed files. It
includes the recorded `Assembly-CSharp` and `System.Xml` identities. This is not
a complete inventory of all game versions or framework components.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## Verification

1. Read the exact artifact ref and pin its full commit SHA.
2. Read `manifest.json` at that commit using GitHub Contents, checking type,
   base64 byte length and Git blob SHA-1. Validate every manifest identity/field.
3. Read the source branch head. The manifest source must equal it or be an
   ancestor confirmed by GitHub compare: `ahead`, matching base, zero behind.
   An explicitly returned head commit must match the requested full head SHA;
   an omitted head field is not assumed to be an error by the existing contract.
4. Check the exact `runId`/`attempt`: successful and completed, matching source
   SHA/branch, repository/head repository, configured workflow, push or manual
   dispatch. Validate build start/update times against the observation clock.
5. Re-read the artifact ref; a changed ref invalidates this observation.
6. Download only `https://raw.githubusercontent.com/<repo>/<artifact commit>/<path>`.
   Refuse redirects, encoding transforms, length/hash mismatches and partial files.
7. Inspect actual managed assembly names without executing module code. Verify
   the complete package before owned-folder filesystem transactions.

`devTools:true` is the publisher workflow's declaration for that exact file set
and successful source run. It is not inferred from a workflow text search and is
not independent proof of compiler flags or reproducible binaries. Repository
control and GitHub HTTPS/API metadata are the trust model; no extra signer is
required. The workflow must publish only the intended Development build outputs.
While its publishing run remains in progress, the App deliberately stays unknown.

## Ordering and ownership

Build IDs are opaque identities. A different one alone never enables an update.
The artifact commit must advance on the same artifact branch, and the source must
advance without divergence or the same source must have a later run attempt/build
start. Run update time is stored as the observation's publication time and is not
used alone to prove a newer build. Branch rewrites, older republished runs or
unknown ancestry stay unavailable for automatic update.

New ownership provenance uses `provider:artifact-branch`, `artifactCommit`,
`artifactBranch`, `manifestSha256`, `sourceSha`, `runId`, `attempt`, `buildStartedAt`
and `publishedAt`; no release ID is required. Existing Release/bundle records
remain readable/removable. Legacy records without ordering evidence are not
automatically adopted or upgraded. No unknown files are overwritten.

A matching build ID is current only when an artifact-branch installation also
matches the candidate artifact branch and the SHA-256 of the exact manifest bytes.
Different verified manifest hashes produce an identity mismatch. A historical or
invalid record without comparable artifact provenance produces an unknown identity;
these states both block automatic updates without removing installed files. A new
artifact commit with identical manifest bytes can remain current. Other providers
retain their existing comparison behavior, and different build IDs still require
forward update-order evidence.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## Migration and remaining acceptance

### Shared discovery and request budget

Automatic checks share the GitHub `git/matching-refs/heads/` snapshot rather than
performing five API requests per module. The whole bounded response is required;
missing or malformed response headers, a pagination `Link` header in any casing,
or incomplete refs are not accepted as a complete list.
Mixed-repository batches are refused before the ref query; check each repository
separately.
Read the snapshot again after the round to detect source/artifact ref changes.
Read a changed module's manifest at its pinned commit; reuse hash-checked immutable
manifest contents when the artifact commit is unchanged. Discovery is explicitly
unverified: it does not claim the successful source run was checked.

For 15 available, uninstalled modules, the synthetic same-head fixture uses 17 API
requests initially (two shared snapshots plus 15 manifests) and two when unchanged.
Complete pre-install verification of those 15 uses 32 requests (two snapshots plus
15 manifests and 15 successful-run checks). Source ancestry and update-order
comparisons can add requests; those numbers are fixture measurements, not universal
cost guarantees. Installation always performs the full validation path again.

Automatic rounds cap logical API calls at 40, reserving a final snapshot request.
Full selected verification caps at 50. The shared transport independently caps
actual requests at 50 in a rolling hour, preserves that budget across refreshes,
and honors GitHub's remaining/reset and rate-limit cooldown. No credentials or
unbounded retries bypass the limit. A batch exceeding the remaining budget does
not install unverified modules; wait for retry or use a smaller explicit batch.

Manifest discovery rotates its starting point when work is deferred. Changed
installed modules receive at most three full automatic update checks per round,
in rotating order. Verified forward-order results can be reused only for the same
installed build, artifact commit and observed source head; installation revalidates.
Unverified/negative ordering is not cached as permanent rejection. Queue states
explain deferred verification; they are not represented as up-to-date or successful.
Low-budget tests exercise all 15 modules across repeated hourly budget windows.

API behavior reference: [GitHub Git references](https://docs.github.com/en/rest/git/refs).

**Independent Development build branches** is the startup default in the desktop
interface, backend and service, and the only source offered to players. Missing or
invalid artifact sources stay unavailable; the App never falls back to module
Releases or bundle ZIPs. Internal legacy code remains for historical tests and
records, but cannot be selected through the desktop/backend source command.
Offline removal of intact owned installations is preserved. GitHub Releases are
reserved for the App's `Setup.exe`.
Existing portable outputs predate this source; no replacement portable package
or desktop acceptance is claimed by the source-only milestone.

Local tests include pinned refs/blobs, identities, source ancestry, run/DevTools
mismatches, missing dependencies, transfer limits, forward updates, older builds,
legacy removal and actual synthetic assembly inspection in isolated Mods folders.
Actual remote artifact publication/download and game acceptance remain pending.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
