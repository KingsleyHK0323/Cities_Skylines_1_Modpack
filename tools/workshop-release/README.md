# Workshop release controller candidate

This directory contains a local, unverified implementation candidate. The
workflow's self-hosted upload job is hard-disabled, and every module has
`releaseReady: false`. It cannot yet publish a Steam item.

The Issue form accepts one or more registered modules. A preview identifies each
selected branch head and target item. A subsequent owner label is checked against
the Issue, preview, branch heads and issue-event history; a durable ledger claim
is intended to prevent a request from being consumed twice. Build candidates use
exact reviewed source commits and fixed source or project recipes. Each package
has its own target, dependency list and content hashes. The candidate batch
uploader checks every package and Steam item before attempting the first upload.

## Current limitations

- No candidate workflow has been deployed or executed in GitHub Actions. The
  cloud build recipes and resulting assemblies have not been verified in this run.
- `workshop-release-validation.yml` is a proposed owner-only, GitHub-hosted
  validation for the exact reviewed AI Improve and Wireless sources. It uses a
  synthetic snapshot and performs no Issue approval, artifact publication,
  Steam upload or self-hosted execution; it has not yet run in the cloud.
- AI Improve and twelve other modules have source recipes. APTP does not yet
  have a reviewed recipe for its current release boundary; AI Gaming remains paused.
  Full Map is blocked because its reviewed source identifies itself as a
  Development Preview. Several other recipes are observation-only foundations,
  not evidence that their intended gameplay functions are complete.
- An anonymous Steam Web API query returned creator and game information for
  the public AI Improve item, but a sampled hidden item was not available. A
  reliable authenticated preflight for hidden items is not implemented.
- SteamCMD process completion alone does not prove a successful item update. The
  candidate therefore marks the first attempted submission as unknown and stops;
  it cannot yet complete automatic multi-item publication. A result of unknown
  must be independently investigated and must not be retried automatically.
- The current `workshop-release` environment has an existing required reviewer.
  If used unchanged, that adds a manual approval after the Issue label; this is
  different from the requested label-only operation.
- Steam Client and SteamCMD account identity have not been bound. A successful
  public-item metadata lookup does not prove the SteamCMD login belongs to the
  same account. Changes between the last checks and Steam reading files are not
  atomic, and other legacy upload workflows do not share the new concurrency
  group. GitHub status and game acceptance are separate evidence.

## Deployment prerequisites

Before any deployment, review the exact workflow, recipes and the separately
approved setup. The ledger branch `Workshop_Release_Ledger` must be initialized
once with `workshop-release-ledger.json` containing schema `1`, repository
`KingsleyHK0323/Cities_Skylines_1_Modpack`, and purpose
`workshop-release-claims`. The controller refuses a missing or invalid marker
and will not recreate the ledger automatically. Protect the branch against
deletion and force pushes, and restrict writers to the trusted controller and
necessary repository administrators. These settings do not by themselves prove
that all future commits are append-only.

The cloud build needs the existing encrypted game-reference key. The future
self-hosted upload needs its trusted SteamCMD executable and login configuration
and a private `STEAM_WORKSHOP_OWNER_ID` setting. Never enter credentials into an
Issue or a public source file. The exact Steam ownership provider, batch success
evidence, additional environment review and local Runner operation must be
resolved and explicitly authorized before the hard-disabled job is enabled.

Signature: OpenAI-Codex.
