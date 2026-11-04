---
title: Automated Public Transport Planning
description: "城市調查診斷、開發版路線提案，以及確認後嘗試建立路線的限制。"
---

> <span lang="en">Distribution guidance: If Dev Manager Windows Setup.exe is published, check GitHub Releases. Module availability varies; use verified builds shown in the app when available. Historical mod-release and bundle-download instructions below are not current download methods. See the <a href="/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/">Development guide</a>.</span>

<!--
> <span lang="en">Distribution update (3 November 2026): GitHub Releases is planned for the Dev Manager Windows installer. Individual mod artifacts are planned on separate branches; the AI Improve and Automated Public Transport Planning candidates and the installer are not yet published. Earlier mod-release and bundle-download instructions below are historical. See the <a href="/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/">Development guide</a>.</span>
-->

<!-- Distribution update: OpenAI-Codex. -->

> **開發版更新，2026 年 10 月 31 日：** GitHub 開發原型可在解鎖功能並確認後，嘗試建立所選路線；它可能修改存檔，並非唯讀工具。成功建立路線及儲存／重新載入尚未通過工作坊訂閱版本的遊戲驗收。請見[開發版下載與安裝](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。

Automated Public Transport Planning 的開發原型會估算市內公車路線，並可在解鎖功能及確認後，嘗試建立所選提案。原型可能修改存檔，不保證路線能夠運行。其他交通類型仍屬未來方向。

## 城市調查與診斷

- **城市調查。** 模組在背景讀取道路、站點與公共運輸網絡。調查本身只讀取資料；另一項開發版路線建立原型則可能修改存檔。
- **模組選項中的診斷工具。** 每個按鈕都會把結果寫入遊戲記錄：

| 按鈕 | 回報內容 |
| --- | --- |
| 將測試路線寫入記錄檔 | 確認模組已載入並正常回應。 |
| 列出公車 prefab 詳細資料 | 你的遊戲中可用的公車車型。 |
| 量測規劃成本（會短暫停頓） | 在你的城市上進行規劃需要多少時間。 |
| 擷取世界快照 | 規劃器會用到的道路與站點。 |
| 建立路線圖 | 各站點之間可能的連接網絡。 |

- **語言選擇。** 11 種語言，可在模組選項中切換，不需重新啟動。

## 目前還不能做什麼

較早的 0.2.0 正式預覽版不會建立、修改或建議公車路線。開發原型專注於市內公車；其他交通類型仍屬未來方向。請見[路線圖](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/roadmap/)。

## 開發版

實驗性開發版的指引請見[開發版下載與安裝](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)；使用前請先備份存檔。Steam 工作坊開發版已下架。

- **規劃預覽（測試中）。** 按「Generate a read-only planning preview」（產生唯讀規劃預覽），會依城市需求估算市內公車路線，並列出評分。不會建立或修改任何路線。這些估算尚未經遊戲本身的尋路驗證。
- **路線建立原型。** 解鎖功能並逐次確認後，可提交所選提案嘗試建立路線。提案不保證路線連通或能夠運行。成功建立路線及儲存／重新載入尚未通過工作坊訂閱版本的遊戲驗收；請備份存檔並使用獨立測試城市。

<details>
<summary lang="en">Historical prototype release — not a current mod download</summary>

- **既有路線移站原型。** [歷史開發版發行紀錄](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/Automated_Public_Transport_Planning-dev-41252417067-1)來自[原始碼 `60976d7`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/commit/60976d7c34f73aee698af192a660abe001ec347d)，當時包括手動及自動的純移站編輯，適用於有 3–6 個站點的普通市內公車既有路線；設計上保留線路 ID 及站點數量。嘗試前必須預覽變更、啟用功能並確認兩次。原站及目標站的站位均須使用確切的 `RoadAI`，並通過路段資格及放置檢查；橋樑、隧道、車站專用道路、未知或自訂 AI（人工智能）道路上的站點會被保守拒絕。操作可能修改存檔；回復並非原子操作，可能失敗，亦不保證乘客、車輛或路徑能完整還原。請先備份。遊戲內運作及存檔重新載入尚未通過驗收。

</details>

<p lang="en">Historical route edits can affect saves. Back up saves, use a separate test city, and note that game and save/reload acceptance remain pending.</p>
<!-- Historical prototype: OpenAI-Codex. -->

<details>
<summary lang="en">Historical mod-release guidance — not a current download</summary>

2026 年 11 月 2 日可用性核查：目前無法確認可安裝的附件。安裝前請查看 [GitHub Releases（發行清單）](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases) 及各發行項目的實際附件；自動產生的原始碼壓縮檔不是可安裝的模組檔案。

</details>

## 協助測試

在不同規模的城市上執行診斷工具，並在[回報結果或問題](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose)時附上記錄內容。這能幫助規劃器在推出前調整得更好。

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_zh_docs. -->

<!-- Author: OpenAI-Codex_status_worker. -->

<!-- Author: OpenAI-Codex_transport_update. -->

<!-- Historical disclosure: OpenAI-Codex. -->
