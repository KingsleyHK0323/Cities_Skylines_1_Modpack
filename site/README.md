# Cities: Skylines 1 Modpack website

The site is built with Astro and Starlight. It contains a bilingual project website and source-based Wiki. Astro writes the static site to `dist/` with the GitHub Pages base path `/Cities_Skylines_1_Modpack/`.

## Licensing

The Modpack is source-available and noncommercial; see [Licensing](#licensing) below for code, website content, copyright and third-party terms.

## Local offline setup

This project uses the dependency versions in `package-lock.json`: Astro 7.4.1, Starlight 0.42.6, Sharp 0.35.5 and Playwright 1.64.1. Do not update these versions without reviewing the offline package preparation and site compatibility.

In PowerShell, from this folder:

```powershell
$env:npm_config_cache = 'D:\Code\Game Code\Cities_Skyline_1_Modpack\.tools\npm-cache'
$env:ASTRO_TELEMETRY_DISABLED = '1'
npm ci --offline
npm --offline run extract:ai-improve
npm --offline run prepare:assets
npm --offline run build
```

The Playwright browser is supplied outside the repository. Set its path before browser verification:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = 'D:\Code\Game Code\Cities_Skyline_1_Modpack\.tools\playwright-browsers'
```

## Current verification status

The local `npm --offline run build` attempt on 2026-10-26 failed during `astro sync` with `require is not defined` from `yaml/dist/index.js`. This records the observed error; its root cause has not been established. The existing `dist/` predates the current source changes and is not valid evidence for them. Playwright checks, page-size measurements and current screenshots remain pending. Their intended output location is the project `.tools\tmp\website-screenshots` folder.

## Source-derived content

`src/data/ai-improve.json` is generated from the AI Improve source branch's localization and settings UI. The extractor records the source commit and does not alter source files. If sandbox policy blocks its Git lookup, `AI_IMPROVE_SOURCE_COMMIT` can supply a commit hash that was verified separately. Project art is labelled as artwork, not gameplay capture. Missing game evidence uses an explicit screenshot placeholder.

The AI Gaming page describes the 0.4.0 Development source and identifies its companion packaging and game-runtime verification limits. The transport page distinguishes the 0.2.0 read-only preview from planned route-planning work. A static site build does not verify either mod in game.

## Deployment

This folder contains no deployment workflow. The Pages deployment change should be reviewed and approved separately before switching the public site.

## Licensing

This is a source-available, noncommercial project. Mod and website code use the [PolyForm Noncommercial License 1.0.0](../LICENSE). Original website content, including text and images, uses [CC BY-NC-SA 4.0](../LICENSE-CONTENT). Copyright © Kingsley. Third-party components and assets keep their own licenses as listed in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

<!-- Documentation updated by OpenAI-Codex on 2026-10-27. -->

Author: OpenAI-Codex

## Website changelog visibility

Dated `Change Log.md` headings ending with `<!-- internal -->` remain in the
repository record but are omitted from the public website changelog. Other
entries continue to require their existing Traditional Chinese translations.
Signature: OpenAI-Codex.
