---
title: AI Gaming
description: "0.4.0 開發版原始碼的功能、逐次確認流程，以及套件和執行限制。"
---

> **歷史開發版更新，2026 年 10 月 31 日：** 較早的發布曾分別提供 Windows x64 輔助程式及配對 DLL。開發維持暫停，目前公開檔案是否可用尚未核實。遊戲內執行行為尚未驗證。請見[開發版下載與安裝](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。

**[0.4.0 開發版原始碼 e7de649](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/e7de649f58c775f0692ef74d62eed351cf2d6607/Source/LocalAiRequestController.cs)** 透過 Windows x64 輔助程式提供由玩家確認的 Ollama Local 建議。它不會操控模擬。其他玩法仍屬規劃中。

## 0.4.0 原始碼目前實作

- 可選六項基本資料：遊戲日期、人口、資金、住宅需求、商業需求及工作需求；另有四組資料：公用設施、已建分區建築、交通與公共運輸，以及城市服務狀態。
- 六項及四組資料預設均勾選，可自行取消。獨立的城市摘要功能仍可依玩家要求，把遊戲日期、人口、資金、住宅需求、商業需求及工作需求寫入本機遊戲記錄。
- 遊戲回報為已安裝且可用的 DLC、創作者包及電台識別項會自動加入，沒有獨立的選擇控制項。
- 輸入 Ollama 模型名稱與問題，送出前在預覽中檢視完整資料、模型及問題。
- 每次請求都要獨立確認。模型回覆只作建議，不會轉成遊戲操作。

## 套件與遊戲執行狀態

較早的 GitHub 開發版本曾分別提供 DLL 及獨立封裝的 Windows x64 輔助程式。如有相符的官方發布檔案，請使用同一次發布的檔案：DLL 會核對輔助程式的確切雜湊值。發布檔案及雲端建置不代表已成功在遊戲內使用或完成 Ollama 請求；兩者尚未驗證。請備份存檔並使用獨立測試城市。

## 本機資料與 Ollama

每次確認後，AI Gaming 會透過本機輔助程式把預覽中的資料、模型名稱與問題傳給這台電腦上的 Ollama。Ollama 可能依自身設定把推論轉送至雲端服務；AI Gaming 無法檢查或保證該設定。0.4.0 原始碼沒有雲端供應商介接，也沒有 API key 輸入或儲存功能。自由輸入的問題請勿包含城市名稱、帳戶資料、秘密或個人資料。

## 規劃中的玩法

觀看 AI 建設城市及讓 AI 在有限範圍內操作遊戲都是構想，尚未實作。開發版原始碼會偵測提供方並讀取互通 manifest（資訊清單）；呼叫其他模組的能力及遊戲內互通尚未確立。

進度請見[路線圖](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/roadmap/)。

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_zh_docs. -->
<!-- Author: OpenAI-Codex_module_worker. -->
<!-- Author: OpenAI-Codex_gaming_update. -->
