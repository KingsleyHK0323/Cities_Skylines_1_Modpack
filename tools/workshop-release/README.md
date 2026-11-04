# Workshop release controller candidate

This directory contains an in-development implementation candidate. The
Issue controller and a cloud-only validation workflow are present on the
default branch. The self-hosted upload job is hard-disabled, and every module has
`releaseReady: false`. It cannot yet publish a Steam item.

The Issue form accepts one or more registered modules. A preview identifies each
selected branch head and target item. A subsequent owner label is checked against
the Issue, preview, branch heads and issue-event history; a durable ledger claim
is intended to prevent a request from being consumed twice. Build candidates use
exact reviewed source commits and fixed source or project recipes. Each package
has its own target, dependency list and content hashes. The candidate batch
uploader checks every package and Steam item before attempting the first upload.

## Current limitations

- An end-to-end Issue approval, ledger claim and Steam upload has not been
  validated. `workshop-release-validation.yml` ran successfully for the pinned
  AI Improve and Wireless sources in run 41826121499; AI Improve's optional
  localization check was skipped because its default Python path was missing.
  This validation used a synthetic snapshot and did not approve an Issue,
  publish an artifact, upload to Steam or use a self-hosted runner.
- AI Improve and thirteen other modules have source recipes. APTP's dedicated
  MSBuild batch path passed cloud-only validation in run 41835685397 using
  controller commit `bf6ff50494bb6a1a7b463d9a92f4cc38b4d506ee` and its pinned
  source. It checked the Release DLL's identity, allowed files and hashes;
  no artifact was published. The batch checks the pinned project's declared
  XML inputs and DevTools conditions, not the full set of evaluated MSBuild
  imports or game behavior. AI Gaming remains paused.
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

## Prerequisites for enabling Workshop uploads

Before enabling uploads, review the exact workflow, recipes and the separately
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
