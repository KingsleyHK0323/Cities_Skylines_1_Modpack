# Historical Release publisher contract

The current [artifact branch contract](ARTIFACT-CONTRACT.md) is the only
player-facing module distribution source. GitHub Releases are reserved for the
App's `Setup.exe`. The Release/bundle contracts below document retained internal
code and historical tests; they are not supported player download options.

This contract describes the current desktop implementation. Main Session owns
publication workflows. The App branch does not create Releases, run local
workflows, upload Workshop content or require a new signing service.

## Individual module releases

Use the configured repository `KingsleyHK0323/Cities_Skylines_1_Modpack`, the
module's exact branch and tag `<branch>-dev-<runId>-<attempt>`. Publish a non-draft
Release with an unambiguous `published_at`. The tag must resolve to the successful
Actions run attempt's `head_sha`; the repository, head repository, branch and
workflow path must match. Repo-wide Latest, titles, branch head and prerelease
flags do not determine an individual module's newest published build.

Every required runtime DLL, dependency and legal/data file must be a separate
uploaded Release asset. GitHub asset metadata must expose its exact size and
`sha256:<64 lowercase hex>` digest. Unexpected DLLs and missing required files
make the candidate unavailable. Game reference DLLs are never runtime assets.
All installed files currently use safe flat names, with no executable scripts.

Also publish `build-info.json` or `build-info.txt` as a separately hashed asset.
The App downloads and hashes these bytes before accepting DevTools evidence.
JSON fields are `sourceSHA`, `runId`, `runAttempt`, `DevTools:true`; text uses
`sourceSHA=...`, `runid=...`, `runattempt=...`, `DevTools=true`, one field per line.
They must match the tag's exact successful run attempt. Optional `repository` and
`ref` must match the repository and `refs/heads/<branch>`. Missing or inconsistent
evidence is unknown. A workflow text containing `DevTools=true` is insufficient.
These are publisher declarations, not proof that an arbitrary DLL implements a
specific feature. Assembly bytes are independently checked for expected names.

AI Improve currently requires `AIImprove.dll`, `CitiesHarmony.API.dll`, `LICENSE`,
`CitiesHarmony-LICENSE.txt`, `THIRD-PARTY-NOTICES.md`, `README.md` and the evidence
asset `build-info.json`. APTP requires `AutomatedPublicTransportPlanning.dll`,
`LICENSE`, `README.md` and `build-info.txt`. Other registry entries need complete
publishable mappings and DevTools evidence before installation becomes available.

## New or changed module mappings

Add `dev-manager.manifest.json` to the module branch. The App reads the branch
head's Git blob, checks its byte count/blob hash and rechecks the same mapping at
the selected build commit. A missing manifest permits only existing bootstrap
mappings. An invalid manifest does not silently fall back.

```json
{
  "schema": 1,
  "moduleId": "example-module",
  "repository": "KingsleyHK0323/Cities_Skylines_1_Modpack",
  "branch": "Example_Module",
  "sourceKind": "module-release",
  "assembly": "ExampleModule",
  "workflowPath": ".github/workflows/cloud-build.yml",
  "releaseTagPrefix": "Example_Module-dev-",
  "development": { "devTools": true, "provenanceAssetName": "build-info.json" },
  "runtimeAssets": [
    { "name": "ExampleModule.dll", "assemblyName": "ExampleModule" },
    { "name": "LICENSE", "assemblyName": null }
  ]
}
```

Use the registry's exact `moduleId`; this example does not add a sixteenth module.
Known assembly identities cannot silently change. Required dependencies and
redistribution notices must be complete. The manifest can enable already
registered modules without an App source edit. No dependency download is inferred.

## Scheduled modpack bundle

Publish tag `modpack-dev-<64 lowercase hex sourceIdentity>` with one asset named
`Cities Skylines_1_Modpack.zip` or `Cities.Skylines_1_Modpack.zip`, uploaded size
and GitHub SHA-256 digest. The archive must contain root `manifest.json` and
`SHA256SUMS`. The manifest's `sourceIdentity` must equal the tag suffix. Every
other file, including the manifest, needs exactly one `<sha256>  <path>` checksum.

Each `modules` record has `branch`, exact 40-hex `commit`, numeric positive
`runId` and `attempt`. Known bootstrap paths are in `registry.mjs`: AI Improve
uses `AI_Improve/`; APTP's DLL is at the archive root and its notices/build-info
are under `Documentation/Automated_Public_Transport_Planning/`. APTVM's current
provenance without DevTools confirmation is unavailable as a Development module.
That does not prevent separately proven modules from being installed.

Future records can explicitly provide `moduleId`, `assembly`, `workflowPath`,
`runtimeFiles:[{"name":"ExampleModule.dll","path":"Example/ExampleModule.dll",
"assemblyName":"ExampleModule"}]`, and `provenancePath`. Required known files
cannot be omitted. `devTools:true` is required when no separate provenance path
exists; existing build-info mappings are checked against the record. Every module
still needs its own exact successful Actions run check before installation.

The archive is downloaded once per release/asset/digest, checked in memory and
normalized into independent module packages. It is not extracted wholesale into
Mods. Safe paths, duplicate/collision checks, no links, 512-entry, 32 MiB/file,
64 MiB expanded-total and 64 MiB archive bounds apply. Each installed module gets
one direct owned folder with only its mapped runtime files and notices.

## Update ordering and operational limits

Later publication time alone does not prove a forward build. Different commits
require a successful GitHub compare showing ahead with no behind commits. The
same commit requires a later run attempt or a later successful build start time.
Ownership stores publication provenance for subsequent checks. Legacy records
without it remain removable but are not automatically updated.

Discovery is shared; additional source validation is done only for selected or
installed modules with changed candidates. API concurrency is two; observations
expire after an hour. Rate-limit cooldown is honored across modules, without an
authentication workaround or immediate retry. No public source means unavailable.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
