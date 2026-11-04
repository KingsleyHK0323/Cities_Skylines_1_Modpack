---
title: 路線圖
description: "各 Modpack 模組目前的進度與努力方向。"
---

> <span lang="en">Distribution guidance: If Dev Manager Windows Setup.exe is published, check GitHub Releases. Module availability varies; use verified builds shown in the app when available. Historical mod-release and bundle-download instructions below are not current download methods. See the <a href="/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/">Development guide</a>.</span>

<!--
> <span lang="en">Distribution update (3 November 2026): GitHub Releases is planned for the Dev Manager Windows installer. Individual mod artifacts are planned on separate branches; the AI Improve and Automated Public Transport Planning candidates and the installer are not yet published. Earlier mod-release and bundle-download instructions below are historical. See the <a href="/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/">Development guide</a>.</span>
-->

<!-- Distribution update: OpenAI-Codex. -->

<p>狀態更新，2026 年 11 月 2 日：15 個模組均已有工作坊項目，但可用性不同，建立項目不代表已可在遊戲中使用。專案有 22 項替代方向，請見<a href="/Cities_Skylines_1_Modpack/zh-tw/wiki/project/status/">完整狀態索引</a>。AI Gaming 開發已暫緩。以下較早的路線圖只作歷史背景，不代表目前可下載的內容，也不代表已完成組合遊戲驗收。</p>

<details>
<summary lang="en">Historical mod-release guidance — not a current download</summary>

> **開發版更新，2026 年 11 月 2 日：** 10 月 31 日的記錄曾描述 AI Gaming、AI Improve 及 Automated Public Transport Planning 各自的實驗性版本。目前公開可安裝檔案的供應情況尚未核實，亦可能改變；請查看 [GitHub Releases（發行清單）](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases)中的實際附件，自動產生的原始碼壓縮檔不是可直接安裝的版本。AI Gaming 開發已暫緩；較早版本需要配對的 Windows x64 輔助程式，遊戲內執行尚未驗證。Automated Public Transport Planning 原型可在確認後嘗試建立所選路線。請見[開發版下載與安裝](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。

</details>

這些是發展方向，並非發布日期。實驗性開發版可能損壞存檔，測試前請先備份。

## AI Improve

**現在：** 有工作坊發布記錄，但現時可用性未重新核實。[公開開發版原始碼 `75ef944`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/SettingsPageUI.cs) 的靜態內容有 25 個開關；工作坊穩定版數量未獨立核實。

**下一步：** 為本使用說明補上遊戲內截圖，並依玩家回報持續調整。

## Automated Public Transport Planning

| 階段 | 狀態 |
| --- | --- |
| 運輸網絡調查與診斷 | 調查只讀取資料；獨立的路線建立原型可能修改存檔 |
| 需求分析與候選市內公車路線 | 開發版測試中 |
| 確認後嘗試建立路線 | 開發原型；遊戲驗收待完成 |
| 既有路線移動站點 | 公開開發原型 `60976d7`；須先預覽、啟用及兩次確認，遊戲內與存檔重新載入驗收待完成 |
| 其他公共交通工具（電車、地鐵、火車、渡輪等），逐一加入 | 更後期 |

目前只支援普通市內公車。

## AI Gaming

| 階段 | 狀態 |
| --- | --- |
| 玩家確認的 Ollama Local 建議 | 較早開發版配對 DLL 與輔助程式；現時供應情況及遊戲內執行尚未驗證 |
| AI 觀看城市及有限度遊戲操作 | 規劃中，尚未實作 |

## 所有模組

Modpack 的模組以協同運作為設計方向，但組合運作尚未通過遊戲驗收。AI Gaming 公開開發版原始碼 `e7de649f` 會偵測提供方及讀取互通 manifest（資訊清單）；尚未證實已呼叫其他模組的功能。AI Gaming 開發仍然暫緩。

進度請追蹤[更新紀錄](/Cities_Skylines_1_Modpack/zh-tw/change-log/)。

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_zh_docs. -->

<!-- Author: OpenAI-Codex_status_worker. -->

<!-- Updated by OpenAI-Codex on 2026-11-02. -->

<!-- Updated by OpenAI-Codex_wiki_update on 2026-11-02. -->

<!-- Historical disclosure: OpenAI-Codex. -->
