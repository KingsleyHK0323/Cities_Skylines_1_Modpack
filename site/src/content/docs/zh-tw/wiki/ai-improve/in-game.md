---
title: 遊戲內使用 AI Improve
description: "已核對的 AI Improve 開發版原始碼將內容管理員設定與 UnifiedUI 掃描和重設分開。"
---

本指南描述[開發版原始碼 75ef944](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/IngameUI.cs)，其遊戲內行為尚未驗證。Steam 工作坊穩定版未經獨立核實，介面及行為可能不同。測試前請備份存檔。

## Content Manager（內容管理員）選項頁

開啟**內容管理員**，選擇 **AI Improve** 的選項頁，即可找到個別功能開關及可調整數值。此頁不需要 UnifiedUI。另見[設定說明](/Cities_Skylines_1_Modpack/zh-tw/wiki/ai-improve/settings/)。

## 遊戲內面板（需要 UnifiedUI）

載入城市或劇本時，已核對的原始碼會嘗試在 **UnifiedUI** 註冊 AI Improve 按鈕。若未安裝 UnifiedUI 或註冊失敗，該面板按鈕便無法使用；個別功能設定仍在內容管理員。

雖然面板標題為 **AI_Improve - 功能分類開關**，此開發版面板實際包含兩個空載車輛掃描及一個重設按鈕，沒有個別功能開關。

## 掃描空載的城際車輛

兩個掃描按鈕分別尋找符合條件的城際列車及巴士。原始碼依載具類型、交通路線 ID 為零及 `m_transferSize` 欄位為零進行篩選；符合條件不代表每節相連的拖掛車廂均空載。

未找到符合條件的首車時，面板會顯示沒有找到；否則會顯示數量並詢問是否刪除。取消不會透過該確認程序觸發刪除。確認後，[刪除程式碼](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/EmptyVehicleAuditor.cs#L160-L185)會檢查每個已記錄識別碼所對應的載具是否標記為 `Created`，以及是否仍符合類型、路線及載客數欄位條件。之後會嘗試移除該載具及相連拖掛車廂；它不會逐一以空載條件重查每節車廂。

**依原始碼推論，尚未在遊戲內驗證：** 這項複檢不會核對原始載具身分、`Deleted` 標記或識別碼是否仍指向首車。若刪除執行前識別碼已被另一輛仍符合檢查條件的載具重用，程式可能將該替代載具列為刪除目標。

## 重設設定

**重設為預設值**會在執行前要求確認。已核對的原始碼會重設功能開關及可調整數值，保留語言選擇；這是整體設定重設，並非個別功能開關。

## 截圖

> 面板與選項頁的遊戲內截圖待補。

<!-- Updated by OpenAI-Codex on 2026-10-26. -->
<!-- Author: OpenAI-Codex_module_worker. -->
