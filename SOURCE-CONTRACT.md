# Development source contract assessment

This is a read-only assessment on 2026-11-02, not a configured downloader or a
claim that any candidate is the latest version for every module.

The connected GitHub tool successfully fetched the repository's
[release collection](https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/releases?per_page=10).
Only that first page was inspected. The authenticated response included drafts;
applications must reject unpublished/draft entries and must not select by a title
such as `[LATEST DEV]` alone.

Observed examples:

| Published release ID | Shape | Limitation |
| --- | --- | --- |
| 430978806 | `Cities.Skylines_1_Modpack.zip`, asset ID 700072080, API SHA-256 present | Bundle metadata identifies the packaging source, not every module. Its internal manifest and bytes were not downloaded or verified in this assessment. |
| 430467911 | AI Improve DLL, CitiesHarmony.API DLL, licenses, notices, `build-info.json`, `SHA256SUMS` | These exact assets are usable candidates for a future module-specific adapter; repository ordering is not proof of latest module source. |
| 430469694 | Planning DLL, `build-info.txt`, `SHA256SUMS` and installation documents | The tag denotes a development build but `prerelease=false` in this snapshot. The prerelease flag alone cannot safely classify the development channel. |

The module ID-to-branch/assembly mapping, version policy, build flags, selected
source SHA, workflow identity and full set of runtime files must be verified
before a release is converted into an installation contract. The current
`updateStatus` function compares opaque build identities and deliberately reports
`different-build`; it cannot infer newer versus older without a verified catalog.

No universal per-module latest catalog or standardized package schema was proved
by this limited query. The deployment pipeline is also being redesigned by Main.
Automatic latest selection and download are therefore **not implemented**. Future
work must not guess tags, silently use a repository-wide Latest release, equate an
Actions artifact with a durable public download, or treat a metadata digest alone
as module identity. The existing filesystem prototype is only a downstream
consumer of explicitly supplied fixture bytes and expected hashes.

No GitHub binary, game reference, mod DLL, credential or Steam file was downloaded
or installed as part of this assessment. Only JSON metadata and the official
project license text were read online.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## Proposed offline catalog contract (not a production publisher schema)

The pure `catalogStatus(installed, policy, data)` adapter accepts an explicit
per-module snapshot. It does not inspect release ordering or make network calls.
The caller must obtain `policy` through a separate trusted mechanism; computing
its digest from the same untrusted download does not establish trust. This
mechanism is not implemented. The adapter verifies exact byte SHA-256 before
parsing JSON (maximum 1 MiB).

Policy fields: `moduleId`, `repository` (`owner/repo`), `branch`, `assembly`
(without `.dll`), `workflow`, `requiredFiles` (complete runtime path allowlist),
and `catalogSha256`. No real module mapping or completeness is assumed by default.

Catalog fields:

| Field | Required meaning |
| --- | --- |
| `schema` | Integer `1`, a local proposed schema only |
| `moduleId`, `repository`, `branch`, `assembly`, `workflow` | Exact policy mapping |
| `channel`, `devTool` | `development`, boolean `true` |
| `buildId` | Nonempty opaque identity, not a sortable version |
| `sourceSha` | 40 lowercase hexadecimal characters, a declaration only |
| `release` | Positive safe-integer `id`, nonempty `tag`, explicit `draft:false`, valid UTC `publishedAt` in `YYYY-MM-DDTHH:mm:ssZ` format |
| `files` | Exact allowlisted runtime paths, including the primary assembly and dependencies |

Every file has `path`, `assetName`, `assetId`, `size`, `sha256` and `url`.
Asset IDs and names are unique; names are individual Windows-safe filenames.
Paths use the same safe relative-path rules as package preflight. Sizes are
positive integers, at most 32 MiB per asset and 64 MiB total; at most 256 files.
Hashes are lowercase SHA-256. URLs must exactly match
`https://github.com/<repository>/releases/download/<encoded-tag>/<encoded-assetName>`;
queries, alternative hosts, unexpected tags and API artifact URLs are refused.
Repository owner/name components and tags equal to `.` or `..` are refused.
Parsing must preserve the canonical URL, origin and exact expected pathname.
Ordinary dotted names and percent-encoded tag characters remain valid; literal
percent sequences are encoded once as data, never recursively decoded.
These are proposed individual-asset contracts, not support for extracting bundles.

The allowlist must come from a trusted, complete runtime contract. The validator
cannot discover an omitted dependency, inspect DLL assembly identity, prove that
`devTool` matches compiled bytes or attest a workflow run. It does not connect
catalog results to the filesystem executor or bypass downstream byte verification.
Published metadata and hashes do not independently prove any of those properties.

Invalid sources yield `unavailable` and no catalog. Valid pinned snapshots yield
`not-installed`, `current` or `different-build`, independently for each module.
`different-build` is not an update-order claim. The caller must expire/revoke pins;
stale or replayed but still pinned snapshots cannot be detected by this adapter.
Neither the repository-wide Latest entry nor `prerelease` determines selection.

Main's subsequent read-only handoff reports the current bundle manifest contains
only per-module `branch`, `commit`, `runId` and `attempt`, plus bundle checksums.
It lacks a verified full runtime file catalog. This handoff was not independently
requeried here. The current bundle is therefore not wired into this adapter;
production schema, trusted policy delivery and freshness rules remain undecided.

## Injected asset transfer boundary

`downloadCatalogAsset` validates this proposed pinned catalog before selecting
one exact asset path. It does not add authentication to a self-reported checksum.
Its injectable transport contract requires manual redirects, an exact response
URL, explicit `redirected:false`, lowercase headers and an async Uint8Array stream.
The caller's complete HTTPS redirect URL allowlist is separate from untrusted
response Location headers. The default list is empty; no real CDN policy has
been selected. Redirect targets are never logged or included in error messages.

Only fully received, size-matching and SHA-256-matching bytes are returned in
memory. There are no disk writes and no connection to install controls. A single
verified asset is not a complete module package; all required runtime files and
binary identities still need downstream verification. A future adapter must
honor abort and release response resources; no real transport, DNS behavior,
redirect service or network acceptance has been implemented or tested.

## Batch preparation contract

Batch assessment requires explicit installation-record knowledge and binds each
selected module ID to its own pinned catalog policy. The returned display records
can feed the selection model; they do not establish a live authenticated catalog.
Preparation snapshots all contracts, refuses any unavailable entry before the
first injected request and requires every runtime asset of every module to pass.
It only returns complete in-memory packages; no partial batch is returned on
failure. Batch redirect configuration, real transport and browser integration
remain unavailable. Package hash verification still does not inspect assembly
identity, prove workflow provenance or authorize installation.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
