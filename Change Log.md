# Change Log

Author: OpenAI-Codex

## 2026-11-01 — Illustration semantics and preference controls

- Replaced the transport illustration's tab semantics with a caption-selection button group, retaining arrow/Home/End navigation and adding a readable polite status message.
- Marked AI Improve reference headings, descriptions and tuning labels with the language of the bundled translation or English fallback.
- Raised theme/language targets to a 44px minimum and added visible select focus and an inset Wiki title focus ring.
- Kept existing favicon, Apple and manifest PNG assets; no suitable existing WebP brand source was identified, so no replacement artwork was introduced.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-11-01 — Navigation accessibility and dated project status

- Removed the narrow-screen navigation scroll container that clipped the Mods menu, allowed navigation to wrap, and enlarged its interaction targets.
- Darkened the dark-theme primary button background to exceed 4.5:1 contrast with white text.
- Replaced homepage compatibility guarantees with acceptance boundaries and linked the 15-module / 20-target status index. Marked AI Gaming development paused and unresolved responsibility assignments as proposals.
- Updated Vehicle Management to a read-only prototype with a delivered Actions artifact, separate from public Release / Workshop availability.
- Added English language metadata to the shared download button group.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-10-31 — Planned vehicle management artwork

- Added the approved Release and Development WebP icons for Automated Public Transport Vehicle Management to a separate planned-project section on both homepages.
- Kept existing project artwork served as WebP and preserved original images. No download or Workshop link is advertised for the planned project.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-10-31 — Compact homepage Development notice

- Replaced the oversized homepage announcement with a shared compact, responsive notice and retained the download action and save precautions.
- Localized the Traditional Chinese homepage notice and marked its English installation guide destination.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-10-31 — Mobile documentation and download navigation

- Updated active project URLs and workflow repository checks for the KingsleyHK0323 account name. Historical status records retain their original account names.

- Kept the documentation header on one row at narrow widths, with language and theme controls available in the native mobile menu.
- Added a prominent GitHub Development download button before the Workshop links and a Development downloads entry in the documentation sidebar.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-10-31 — Development downloads and installation

- Added links to the separate GitHub Development prereleases of AI Gaming, AI Improve, and Automated Public Transport Planning, with installation dependencies and save warnings. Stable releases remain on Steam Workshop.
- Clarified that the AI Gaming DLL and Companion must match, and that the Automated Public Transport Planning Development prototype can attempt confirmed route creation. Cloud builds and file checks are not in-game acceptance.
- Author: OpenAI-Codex_Responsible: Modpack_Website.

## 2026-10-27 — License terms aligned

- Updated license files, documentation and the site footer: mod and website code use the PolyForm Noncommercial License 1.0.0; website content, including text and images, uses CC BY-NC-SA 4.0.
- Copyright © Kingsley. Third-party components keep their own licenses.
- Author: OpenAI-Codex.

## 2026-10-22 — Language switch label fixed

- The language button in the site header now shows the language it switches to: English on Traditional Chinese pages, 繁體中文 on English pages. Its screen-reader label was corrected to match.
- Author: Anthropic-Claude_Code.

## 2026-10-22 — Automated Public Transport Planning guide updated

- The guide and roadmap now state that the mod covers regular city buses only, with other public transport types to be added one at a time later.
- The guide describes the Development Version's read-only planning preview (being tested) and its safer test controls: creating a test line asks for confirmation every time, and removing lines from the mod is disabled.
- Author: Anthropic-Claude_Code.

## 2026-10-22 — Development Versions on the website

- The Development Versions of AI Improve, Automated Public Transport Planning and AI Gaming are now public on Steam Workshop, and each mod page links to them. The Release Versions of Automated Public Transport Planning and AI Gaming are still marked coming soon.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — AI Improve compatibility note corrected

- The AI Improve compatibility page no longer claims that the emergency-vehicle cost adjustment works with TM:PE. Testing showed that this adjustment currently fails to load when TM:PE is enabled; a fix is being tested on the Development Version. The optional mid-route emergency reroute is not affected.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — Workshop preview images per edition

- Each Workshop item now has its own preview image: Release and Development Versions are marked with a version badge.
- Upload workflows prefill the matching image for their edition. The images are JPG copies of the new artwork, each under Steam's 1 MB limit.
- The new images appear on each Workshop page after that item's next upload.
- Author: Anthropic-Claude_Code.
## 2026-10-21 — New website and wiki

- Rebuilt the website with Astro and Starlight as a product site plus a bilingual (English and Traditional Chinese) wiki with search. It is built from the `site/` folder and deployed to GitHub Pages by a workflow on every change.
- Mod pages explain what each mod does for players, show Workshop buttons (hidden items are marked coming soon), and include a feature browser, a collapsible settings preview, a planned bus-planning walkthrough and an AI Gaming mode explainer.
- The wiki covers getting started, every AI Improve feature and setting, in-game use with UnifiedUI, compatibility, FAQ, roadmap and contributing.
- Corrected the AI Improve compatibility description: it does not change vehicle capacity, and it defers fire dispatch choices to Transfer Manager CE.
- Authors: OpenAI-Codex (initial build), Anthropic-Claude_Code (build fixes and redesign).

## 2026-10-21 — Release and Development Workshop pipeline

- All six Workshop items are registered: AI Improve (3782858610, 3823575900), Automated Public Transport Planning (3823739383, 3823739653) and AI Gaming (3823740106, 3823740277). The AI Gaming upload workflows are no longer blocked.
- Development Versions are always built with DevTools and can be uploaded automatically after each push to the module branch. Release Versions are always built without DevTools, are uploaded only on request, and wait for approval. Each build is checked for DevTools content before upload.
- Preview images now come from the JPG icon set, which stays within Steam's 1 MB preview limit.
- Automatic Development uploads and Release approval stay switched off until the self-hosted runner is set up.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — AI Gaming 0.1.0 early preview

- First AI Gaming preview, with English and Traditional Chinese settings. Players can write a read-only city summary (population, funds and in-game date) to the game log.
- This version does not connect to an AI model, make network connections or control the simulation.
- Author: OpenAI-Codex (recorded by Anthropic-Claude_Code).

## 2026-10-21 — Automated Public Transport Planning 0.2.0 early preview

- The mod description in all 11 languages now accurately describes a read-only city survey preview instead of finished route planning.
- The save round-trip test and its city-changing controls are excluded from standard builds and kept for the DevTools build; read-only diagnostics remain.
- Mod and assembly versions are 0.2.0.
- Author: OpenAI-Codex (recorded by Anthropic-Claude_Code).

## 2026-10-21 — Workshop descriptions from the mod branches

- Upload workflows can now update the Workshop description from `Workshop/Release.txt` or `Workshop/Development.txt` on the mod branch. Without that file the current description is kept.
- Visibility and tags are still left unchanged by uploads.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — Workshop item IDs pinned in upload workflows

- Registered the AI Improve Workshop items: Release Version 3782858610 and Development Version 3823575900. Both upload workflows now prefill their ID.
- The upload script now rejects any ID that does not match the registered item for that module and edition, and refuses items that have no registered ID yet (Transport and AI Gaming).
- AI Improve uploads no longer copy the build into the runner account's own Mods folder.
- Workflows still need the self-hosted runner and SteamCMD setup before they can run.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — AI Improve author credit and settings links

- AI Improve: the mod description shown in the Content Manager now credits Kingsley instead of SpaceSquare. The internal Harmony ID is unchanged for compatibility.
- AI Improve: settings-page links no longer point to the removed standalone repository. "GitHub Source" opens the AI_Improve branch, "Documentation Wiki" opens the AI Improve website page, "Changelog" opens the website Change Log, and "Report an Issue" opens this repository's issue forms.
- Takes effect in game after the next Workshop update.
- Author: Anthropic-Claude_Code.

## 2026-10-21 — GPL-3.0 license

- Added the GNU General Public License v3.0 to the website and all three mod branches (AI Improve, Automated Public Transport Planning, AI Gaming), copied verbatim from https://www.gnu.org/licenses/gpl-3.0.txt.
- Added a license line to each branch README and updated the licensing note in DEVELOPMENT.md. Third-party components keep their own licenses.
- Author: Anthropic-Claude_Code.

## 2026-10-20 — Website experience and artwork

- Added the three project-specific artworks to the home page selector and project pages; each image is identified as artwork, not gameplay.
- Improved narrow-screen header and page-content wrapping and removed the decorative Change Log divider.
- Added bilingual, page-specific descriptions and artwork alt text, and ordered generated Change Log entries by date.
- Added a subtle city-art motion that respects reduced-motion preferences.
- Author: OpenAI-Codex.

## 2026-10-20 — Manual Steam Workshop upload workflows

- Added separate, confirmation-gated Release Version and Development Version workflows for AI Improve and Automated Public Transport Planning.
- Added blocked workflows for both AI Gaming versions because the branch has no buildable mod project or package definition.
- Added local self-hosted runner and SteamCMD setup instructions; Steam credentials and preview images are not stored in the repository.

## 2026-10-19 — Collection attribution

- Added "by Kingsley" beneath the site brand and updated the collection credit to Kingsley.
- Added a small inline SVG Hong Kong flag icon beside the collection credit for consistent display across platforms.

## 2026-10-19 — Initial local website

- Added the collection home page and three project introduction pages.
- Added English/dark defaults and independently persisted Traditional Chinese/light preferences.
- Added a keyboard-accessible project selector, static fallback content, and original conceptual city illustrations.
- Marked AI Gaming as not started and left other unverified project statuses explicit.
- Added responsive layouts, reduced-motion behavior, local documentation, and dependency-free page generation.
- Adapted the user-provided Uiverse GitHub button reference with source attribution and MIT notices.
- This entry describes local website development; it does not announce a mod release or public deployment.

## 2026-10-19 — Original repository website

- Created the modpack website with a landing page and one subfolder per mod.
- Moved the Automated Public Transport Planning preview from its old repository (preview branch commit 0af5dc6) into `automated-public-transport-planning/` and pointed its links at this repository.

## 2026-10-20 — Website documentation entry points

- Replaced local README entry files across the branch checkouts with links to the matching website pages.
- Preserved the website development guide as DEVELOPMENT.md.
- Added a generated Change Log page sourced from this file.

## 2026-10-20 — Website branding and social preview

- Added the Website Icon to the shared site header and favicon.
- Added the 1280 × 640 social preview image to Open Graph and Twitter metadata across generated pages.
- Author: OpenAI-Codex.
