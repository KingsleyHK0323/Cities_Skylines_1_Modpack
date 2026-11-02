---
title: AI Improve 相容性
description: "AI Improve 與其他熱門 Cities: Skylines 1 模組一起使用時的行為。"
---

已核對的[開發版原始碼 75ef944](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/75ef94407962817051cef4aa74ec6fbc1831fd02) 會偵測部分模組，並針對某些組合作條件處理。相容性取決於涉及的版本及條件；遊戲內驗收仍待完成。

| 模組 | AI Improve 的處理方式 |
| --- | --- |
| **Harmony** | 必須使用。AI Improve 透過它修改遊戲行為。 |
| **UnifiedUI** | 選用。已核對的開發版原始碼提供空載城際列車及巴士掃描與設定重設面板；各功能開關位於 Content Manager。掃描後可能提供刪除車輛的選項，須另行確認。條件及限制請見[遊戲內指南](/Cities_Skylines_1_Modpack/zh-tw/wiki/ai-improve/in-game/)；實際安裝的版本可能不同。 |
| **Improved Public Transport**、**Express Bus Services** | 已核對的原始碼在偵測到其中一個模組時，會按條件停用「縮短空站停留」及「拉開同線車輛間距」。 |
| **Transfer Manager CE** | 它的消防派遣啟用時，已核對的原始碼會由它選擇火警，並保留每棟建築的出勤上限。 |
| **Traffic Manager: President Edition（TM:PE）** | 已核對的原始碼實作了緊急車輛尋路成本調整的候選方案。能否套用取決於 TM:PE 版本及指令是否符合預期；若遭拒絕，會保留原有邏輯。請在輸出記錄中檢查警告；遊戲內驗收仍待完成。 |
| **Real Time** | 會偵測。兩個模組均涉及消防派遣步驟；遊戲內相容性尚未確立。 |
| **SingleTrainTrackAI**、**Reversible Tram AI** | 會偵測。偵測本身不能證明相容。 |

AI Improve 不會改變任何載具的載客量。

## Release 與 Development 版同時使用

不要同時啟用 AI Improve 的兩個版本。它們是同一個模組，會互相衝突。

## 回報相容性問題

如果和其他模組一起使用時出問題，請開一個 [Issue](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose)，附上遊戲版本、完整模組清單、相關的 AI Improve 功能、重現步驟，以及輸出記錄的相關段落（見[常見問題](/Cities_Skylines_1_Modpack/zh-tw/wiki/ai-improve/faq/)）。

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
<!-- Author: OpenAI-Codex_module_worker. -->
<!-- Updated by OpenAI-Codex on 2026-11-01. -->
