---
title: AI Gaming
description: "0.4.0 開發版原始碼的功能、逐次確認流程，以及套件和執行限制。"
---

> **開發版更新，2026 年 10 月 31 日：** GitHub 發布檔案包括 Windows x64 輔助程式及配對 DLL。遊戲內執行行為尚未驗證。請見[開發版下載與安裝](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。

**0.4.0 開發版原始碼**加入可選的 Windows 輔助程式，讓玩家透過 Ollama Local 取得建議。它不會操控模擬。其他玩法仍屬規劃中。

## 0.4.0 原始碼目前實作

- 選擇要提供的城市統計資料：遊戲日期、人口、資金、住宅需求、商業需求及工作需求。
- 六項資料預設全選，不想分享的資料需自行取消。獨立的城市摘要功能仍可依玩家要求，把遊戲日期、人口與資金寫入本機遊戲記錄。
- 輸入 Ollama 模型名稱與問題，再檢視完整請求內容。
- 每次請求都要獨立確認。模型回覆只作建議，不會轉成遊戲操作。

## 套件與遊戲執行狀態

GitHub 開發預覽版本分別提供 DLL 及獨立封裝的 Windows x64 輔助程式。請使用同一次發布的檔案：DLL 會核對輔助程式的確切雜湊值。發布檔案及雲端建置不代表已成功在遊戲內使用或完成 Ollama 請求；兩者尚未驗證。請備份存檔並使用獨立測試城市。

## 本機資料與 Ollama

AI Gaming 只會把所選統計資料、模型名稱與問題傳給這台電腦上的 Ollama。Ollama 可能依自身設定把推論轉送至雲端服務；AI Gaming 無法檢查或保證該設定。現有原始碼沒有雲端供應商介接，也沒有 API key 輸入或儲存功能。自由輸入的問題請勿包含城市名稱、帳戶資料、秘密或個人資料。

## 規劃中的玩法

觀看 AI 建設城市及讓 AI 在有限範圍內操作遊戲都是構想，尚未實作。0.4.0 也沒有呼叫其他 Modpack 模組；使用其他模組能力是長期設計方向。

進度請見[路線圖](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/roadmap/)。

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_zh_docs. -->
