# Cities: Skylines 1 Modpack

## Workshop release request candidate

An in-development Issue form and controller prepare owner-reviewed, single- or
multi-module release requests. The candidate pins each selected source commit,
validates a one-use approval claim and prepares reviewed cloud build recipes.
Steam upload is explicitly disabled: the complete batch uploader, private-item
ownership checks, ledger protection, cloud build results and game-entry checks
still require verification. AI Gaming remains paused. The existing
`workshop-release` environment also requires an additional reviewer approval;
this differs from the intended label-only trigger and must be resolved before
uploads are enabled. No Issue or label currently enables this candidate to upload.
The pinned AI Improve, Wireless and APTP recipes passed separate cloud-only
build validations; the complete Issue-to-Steam pipeline remains unverified.
<!-- Signature: OpenAI-Codex. -->

## AI Improve description-only candidate

A separate candidate prepares an owner-approved update of the AI Improve
Workshop description without selecting a DLL, image or visibility change. It
had not been deployed or tested against Steam when prepared on 5 November 2026.
Its prerequisites, exact issue text, and unresolved safety limits are documented in
[`tools/steam-workshop/README.md`](tools/steam-workshop/README.md).
<!-- Signature: OpenAI-Codex. -->

## Development delivery migration

The scheduled combined Development Release is disabled while individual
artifact-branch sources are prepared for Dev Manager. Earlier bundle and
individual module Release download guidance is historical during this migration.
<!-- Signature: OpenAI-Codex -->

An owner-triggered cloud workflow supports an App-only
Dev Manager Windows installer. Its default is a build and installation check
without publication; choosing publication verifies the App payload and installer
before publishing one installer asset per version, labelling the latest and
historical App Releases. No App Release is created merely by editing this file.
The candidate checks an exact App file allowlist, approved runtime notices and
draft identity before publication. Release notes disclose that module sources
may be unavailable and that game launch and coexistence checks remain incomplete.
The next installer recipe uses the Windows icon from its pinned App source;
previously published installers remain unchanged. <!-- Signature: OpenAI-Codex -->
Automatic Release title transitions are best effort because GitHub updates are
not conditional atomic writes; manual changes require review. The workflow's
actual build and publication results must be checked separately. <!-- Signature: OpenAI-Codex -->
If publication fails after a successful build, an owner-only recovery workflow
can reuse that exact unexpired build artifact and existing App Release ID. It
validates the original run, build job, artifact, package and draft before
uploading; it cannot create a replacement Release or overwrite an asset.
<!-- Signature: OpenAI-Codex -->
The default-branch dispatch registration does not build or publish a module;
choose the module branch to run its independently owned Development workflow.
<!-- Signature: OpenAI-Codex -->
<!-- Signature: OpenAI-Codex -->

The [dated module status index](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/wiki/project/status/) lists 15 modules and 22 confirmed replacement directions, separating design, source implementation, artifacts, public availability and subscribed-game acceptance. AI Gaming development is paused. Independent operation and compatibility are design goals rather than verified guarantees. <!-- OpenAI-Codex_Responsible: Modpack_Website --> <!-- Updated by OpenAI-Codex. -->

Visit the [Cities: Skylines 1 Modpack website](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/) for project information, documentation, and updates.

Check [GitHub Releases](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases) for current Development availability and confirm that a release has matching installable attachments; source archives alone are not installers. Availability was not verified as of 2 November 2026. AI Gaming development is paused; its earlier build is a historical preview. Back up saves, use a separate test city, and follow each release's installation and dependency instructions. Steam Workshop Development editions have been removed; check the individual Workshop listings for stable-version availability. <!-- OpenAI-Codex_Responsible: Modpack_Website --> <!-- Updated by OpenAI-Codex. -->

Author: OpenAI-Codex

The website exposes Development download guidance in the documentation sidebar and a release-availability check on mod pages. On small screens, the Wiki menu contains language and theme controls. <!-- OpenAI-Codex_Responsible: Modpack_Website --> <!-- Updated by OpenAI-Codex. -->

This is a source-available, noncommercial project.

- Mod and website code: [PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0/) (see [LICENSE](LICENSE)).
- Website content, including text and images: [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) (see [LICENSE-CONTENT](LICENSE-CONTENT)).
- Copyright © Kingsley. Third-party components keep their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

The English and Traditional Chinese homepages introduce the project as a whole, link to its dated status and getting-started guides, and include a compact Development notice. <!-- OpenAI-Codex_Responsible: Modpack_Website --> <!-- Updated by OpenAI-Codex. -->

Module artwork is served as WebP where displayed. The Automated Public Transport Vehicle Management icons remain in the repository but are no longer displayed on the homepage; its prototype and publication status are described in the status index. Original resource images are preserved; Steam Workshop artwork uses JPG. <!-- OpenAI-Codex_Responsible: Modpack_Website --> <!-- Updated by OpenAI-Codex. -->

The transport-planning illustration uses a labelled group of pressed-state buttons to select its caption, with arrow/Home/End keyboard navigation and a polite status message. AI Improve reference text carries language metadata for English fallbacks. Theme/language controls use a 44px minimum target and visible keyboard focus; existing browser icon PNGs are retained. <!-- OpenAI-Codex_Responsible: Modpack_Website -->

The AI Improve reference and settings demonstration use data extracted from Development source `75ef94407962817051cef4aa74ec6fbc1831fd02`: 25 switches and one tuning-only card. Installed versions may differ; in-game behavior has not been verified. <!-- Updated by OpenAI-Codex. -->

<!-- Author: OpenAI-Codex. -->

Traditional Chinese routes include localized download/status guides, shared notices, navigation and change-log text. Steam Workshop Development links are disabled and marked removed; historical item IDs are retained separately from GitHub prerelease downloads. <!-- Author: OpenAI-Codex. -->

Traditional Chinese Starlight interface overrides cover heading-link labels and other untranslated framework text. <!-- Author: OpenAI-Codex. -->

## Central development bundle automation

The central bundle workflow collects exact-source verified cloud outputs
from AI Improve, Public Transport Planning and Public Transport Vehicle Management.
See [collector documentation](tools/modpack-release/README.md) for scheduling,
artifact retention, integrity gates and publication behavior. Cloud publication and unchanged-source suppression passed on 2026-11-02. Signature: OpenAI-Codex.

Status refresh (2 November 2026): 15 gameplay modules, 14 non-paused verified cloud game-DLL builds and AI Gaming paused with its older preview. The status/download guides distinguish the deployed three-module collector from the planned cloud-compile pipeline, identify Dev Manager as planning-only, and record warning-only duplicate handling and IMT exclusion. No game acceptance is claimed. Signature: OpenAI-Codex.

Workshop and local-build update (2 November 2026): screenshots provided by the owner show a Workshop item for each of the 15 modules, with visibility varying by item. Twelve modules were also compiled locally without Dev Tools. Neither item creation nor compilation establishes public availability or in-game acceptance. The status and download guides separate these facts from the existing three-module bundle and future publishing plans. Signature: OpenAI-Codex.

Main navigation links expose the current page to assistive technologies. <!-- Updated by OpenAI-Codex. -->

The documentation theme picker preserves an explicit automatic preference across pages, while first visits default to dark. <!-- Updated by OpenAI-Codex. -->

When browser preference storage is blocked, the documentation theme picker remains usable on the current page; a preference cannot be guaranteed across full page loads. <!-- Updated by OpenAI-Codex. -->
