---
title: Using AI Improve in game
description: "The checked AI Improve Development source separates Content Manager settings from UnifiedUI scans and reset."
---

This guide describes [Development source 75ef944](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/IngameUI.cs). Its in-game behavior has not been verified. The Steam Workshop stable edition has not been independently checked and may differ. Back up saves before testing.

## Content Manager options

Open **Content Manager** and choose the **AI Improve** options page for individual feature switches and adjustable values. This page does not require UnifiedUI. See the [settings guide](/Cities_Skylines_1_Modpack/wiki/ai-improve/settings/).

## In-game panel (with UnifiedUI)

When a city or scenario is loaded, the checked source attempts to register an AI Improve button with **UnifiedUI**. If UnifiedUI is absent or registration fails, that panel button is unavailable; use Content Manager for feature settings.

Despite its **AI_Improve - Feature Toggles** title, this Development panel contains two empty-vehicle scans and a reset button. It does not contain individual feature switches.

## Scanning for empty intercity vehicles

The two scan buttons look for eligible intercity trains or buses. The source filters by vehicle type, a transport-line ID of zero and a `m_transferSize` field of zero. A match does not establish that every linked trailer is empty.

If no matching lead vehicles are found, the panel reports that none were found. Otherwise it reports counts and asks whether to delete them. Cancelling does not trigger deletion through that confirmation. If confirmed, the [deletion code](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/EmptyVehicleAuditor.cs#L160-L185) checks that the vehicle at each recorded ID is marked `Created` and still matches the type, line and transfer-size criteria. It then attempts to release that vehicle and its linked trailers without separately re-checking each trailer against the empty-vehicle criteria.

**Source-based inference, not verified in game:** This re-check does not verify the original vehicle's identity, the `Deleted` flag or whether the ID still belongs to a lead vehicle. If an ID is reused by a different vehicle that passes the checks before deletion runs, the code could target the replacement.

## Resetting settings

**Reset to defaults** asks for confirmation before applying the reset. The checked source resets feature switches and adjustable values; it leaves the language choice unchanged. This is a settings reset, not an individual feature toggle.

## Screenshots

> In-game screenshots of the panel and options page are pending.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->
<!-- Author: OpenAI-Codex_module_worker. -->
