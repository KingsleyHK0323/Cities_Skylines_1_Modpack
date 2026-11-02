---
title: 開發版下載
description: "下載及安裝三個 Cities: Skylines 1 Modpack 模組的實驗性開發版。"
---

**2026 年 11 月 1 日更新：Steam Workshop（Steam 工作坊）的開發版已下架。** 下方 GitHub 預覽版本是獨立的發布來源，仍提供下載；此變動不表示 GitHub 開發版一併下架。穩定版是否可用，請檢查各模組的 Steam 工作坊頁面。

下方個別版本來自公開 GitHub 預覽發布；AI Gaming 為歷史版本並維持暫停。請下載個別發布檔案，並依照對應發布頁面的完整指示操作。GitHub 自動產生的原始碼壓縮檔只是原始碼快照，並非安裝套件。

> **存檔警告：** 開發版包含實驗性工具，可能損壞存檔。請備份存檔並使用獨立測試城市。停用同一模組的其他本機及工作坊版本，只啟用一個版本。雲端建置及檔案核對不代表已通過遊戲內驗收。

關閉遊戲前，先在 Content Manager（內容管理員）停用已啟用的工作坊版本。使用 AI Improve 時，請透過 Steam 訂閱 Harmony，並在重新啟動遊戲後於內容管理員啟用 Harmony。
此要求只適用於同一模組的不同版本；不必停用無關模組。

## 最新整包及基礎版建置 — 2026 年 11 月 2 日

[下載已核對的三模組開發版整包](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/modpack-dev-199e88e5fc5794f8c076244c4c273a8fd8773636587ec6205850219d281b0637)。請下載附加的安裝 ZIP，而非 GitHub 自動產生的原始碼壓縮檔。附件實際名稱是 `Cities.Skylines_1_Modpack.zip`，顯示名稱保留 `Cities Skylines_1_Modpack.zip`。

整包包含 **AI Improve、Public Transport Planning 及 Public Transport Vehicle Management**。保留 `AI_Improve/` 資料夾及其執行依賴；每個獨立 DLL 請放入各自的模組資料夾，並保留內附安裝教學及授權聲明。遊戲參考組件不包含在內。這不是全部 15 模組整包，也不代表已完成組合遊戲驗收。

既有收集器於香港時間每日 08:00、13:00、20:00 及手動觸發；來源及打包規則未變時跳過重複發布，GitHub 可能延遲執行。它收集既有成功產物，新決定的雲端編譯含開發工具流程尚未實作。

另外七個基礎遊戲 DLL 的確切雲端執行見[附日期的專案狀態](/Cities_Skylines_1_Modpack/zh-tw/wiki/project/status/)。Actions 產物保留 14 日，下載可能需要登入 GitHub；它們不是完整玩法版本，也未自動加入整包。部分基礎版仍沒有開發工具。Railway 及 Vehicle 尚待首次雲端遊戲 DLL 編譯。

**AI Gaming 維持暫停。** 下方下載是歷史預覽，不代表恢復開發或本次重新核對發布。

### 安裝管理器及重複版本

Cities Skylines 1 Modpack Dev Manager 仍在需求規劃，沒有可安裝 App。逐模組更新偵測、單一／批量安裝、更新及解除安裝屬規劃功能。

已確認的後續遊戲行為是：同一模組正式版與開發版同時安裝時只顯示警告；模組及規劃中的管理器不會僅因並存而自動停用、解除安裝、取消訂閱或阻止載入。此行為尚未套用至全部模組，亦非相容性保證。下方手動教學仍由你自行選擇使用版本。

開發版遊戲名稱規劃為 `<Module Name> - Development Version. This version has DevTool. Be careful; the development version can let your save be broken.`，不代表全部基礎 DLL 已有開發工具。擁有者核准工單、雲端排除開發工具編譯及本機僅上傳的正式發布路線亦尚未完整實作。

<!-- Updated by OpenAI-Codex on 2026-11-02. -->
## AI Gaming

[下載 AI Gaming 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/AI_Gaming-dev-41205697941-1)

1. 關閉遊戲，停用其他 AI Gaming 版本。依據 `SHA256SUMS.txt` 核對下載檔案。
2. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AI Gaming Development`，並將 `AIGaming.dll` 放入其中。
3. 建立 `Companion` 子資料夾，並將 `AIGaming.Companion.exe` 放入其中。DLL 及 Windows x64 輔助程式必須來自**同一次發布**：DLL 會核對輔助程式的確切雜湊值。安裝時請保留隨附的 README、授權、.NET 聲明及建置資訊。
4. 在內容管理員啟用模組、設定本機 Ollama，並使用存檔副本測試。逐次檢視及確認每項請求。直接操控遊戲及雲端供應商介接尚未實作；實際遊戲執行及 Ollama 請求行為尚未驗證。

## AI Improve

[下載 AI Improve 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/AI_Improve-dev-41291471598-1)

1. 關閉遊戲，停用其他 AI Improve 版本。訂閱並啟用 [Harmony 工作坊依賴模組](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)。
2. 從**同一次發布**下載 `AIImprove.dll` 及 `CitiesHarmony.API.dll`，並依據 `SHA256SUMS` 核對。
3. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment`，將兩個 DLL 放在一起。請保留隨附的授權及第三方聲明。
4. 在內容管理員啟用開發版，並使用獨立測試城市。開發工具及解除安裝方法請依照發布頁面操作。

## Automated Public Transport Planning

[下載 Automated Public Transport Planning 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/Automated_Public_Transport_Planning-dev-41291785222-1)

1. 關閉遊戲，停用其他 Automated Public Transport Planning 版本。依據 `SHA256SUMS` 核對 `AutomatedPublicTransportPlanning.dll`。
2. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\APTPDevelopment`，並將 DLL 放入其中。不要安裝遊戲參考組件。
3. 在內容管理員啟用開發版，並使用獨立測試城市。原型可預覽提案；只有解鎖功能並明確確認後，才會嘗試建立所選路線。提案不保證路線連通或能夠運行，成功建立路線及儲存／重新載入尚未通過工作坊訂閱版本的遊戲驗收。

[開發版原始碼 60976d7](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/60976d7c34f73aee698af192a660abe001ec347d/README.md#existing-route-editing--development-prototype) 亦包含既有路線的移站編輯原型，適用於有 3–6 個街道站點的普通市內巴士線。此實作設計為保留路線 ID 及站點數目，不新增或刪除站點。請先檢視修改前後的預覽、啟用編輯，然後確認兩次，才會嘗試編輯。原站及目標站位若位於橋、隧道或車站所屬道路，或使用未知或自訂道路 AI，會被拒絕；這不代表已檢查兩站之間的每一段道路。失敗後程式會盡力回復，但可能失敗，亦可能留下乘客、車輛或路徑方面的影響。請備份存檔；編輯及儲存／重新載入尚未在遊戲內驗證。

若要返回可用的 Steam 工作坊版本，請檢查該模組的工作坊頁面，先關閉遊戲並依照開發版發布的解除安裝指示操作，再啟用工作坊版本。移除開發版不會復原已儲存至城市的變更。

<!-- Author: OpenAI-Codex_zh_docs. -->

<!-- Author: OpenAI-Codex_module_worker. -->
