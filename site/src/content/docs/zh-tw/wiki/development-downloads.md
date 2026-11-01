---
title: 開發版下載
description: "下載及安裝三個 Cities: Skylines 1 Modpack 模組的實驗性開發版。"
---

**2026 年 11 月 1 日更新：Steam Workshop（Steam 工作坊）的開發版已下架。** 下方 GitHub 預覽版本是獨立的發布來源，仍提供下載；此變動不表示 GitHub 開發版一併下架。穩定版仍透過 Steam 工作坊發布。

下方三個開發版均為公開的 GitHub 預覽版本。請下載個別發布檔案，並依照對應發布頁面的完整指示操作。GitHub 自動產生的原始碼壓縮檔只是原始碼快照，並非安裝套件。

> **存檔警告：** 開發版包含實驗性工具，可能損壞存檔。請備份存檔並使用獨立測試城市。停用同一模組的其他本機及工作坊版本，只啟用一個版本。雲端建置及檔案核對不代表已通過遊戲內驗收。

關閉遊戲前，先在 Content Manager（內容管理員）停用已啟用的工作坊版本。使用 AI Improve 時，請透過 Steam 訂閱 Harmony，並在重新啟動遊戲後於內容管理員啟用 Harmony。
此要求只適用於同一模組的不同版本；不必停用無關模組。

## AI Gaming

[下載 AI Gaming 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/ai-gaming-dev-41185662728-1)

1. 關閉遊戲，停用其他 AI Gaming 版本。依據 `SHA256SUMS.txt` 核對下載檔案。
2. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AI Gaming Development`，並將 `AIGaming.dll` 放入其中。
3. 建立 `Companion` 子資料夾，並將 `AIGaming.Companion.exe` 放入其中。DLL 及 Windows x64 輔助程式必須來自**同一次發布**：DLL 會核對輔助程式的確切雜湊值。安裝時請保留隨附的 README、授權、.NET 聲明及建置資訊。
4. 在內容管理員啟用模組、設定本機 Ollama，並使用存檔副本測試。逐次檢視及確認每項請求。直接操控遊戲及雲端供應商介接尚未實作；實際遊戲執行及 Ollama 請求行為尚未驗證。

## AI Improve

[下載 AI Improve 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/ai-improve-dev-41185802009-1)

1. 關閉遊戲，停用其他 AI Improve 版本。訂閱並啟用 [Harmony 工作坊依賴模組](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)。
2. 從**同一次發布**下載 `AIImprove.dll` 及 `CitiesHarmony.API.dll`，並依據 `SHA256SUMS` 核對。
3. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment`，將兩個 DLL 放在一起。請保留隨附的授權及第三方聲明。
4. 在內容管理員啟用開發版，並使用獨立測試城市。開發工具及解除安裝方法請依照發布頁面操作。

## Automated Public Transport Planning

[下載 Automated Public Transport Planning 開發版](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/aptp-dev-41185686621-1)

1. 關閉遊戲，停用其他 Automated Public Transport Planning 版本。依據 `SHA256SUMS` 核對 `AutomatedPublicTransportPlanning.dll`。
2. 建立 `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\APTPDevelopment`，並將 DLL 放入其中。不要安裝遊戲參考組件。
3. 在內容管理員啟用開發版，並使用獨立測試城市。原型可預覽提案；只有解鎖功能並明確確認後，才會嘗試建立所選路線。提案不保證路線連通或能夠運行，成功建立路線及儲存／重新載入尚未通過工作坊訂閱版本的遊戲驗收。

要返回穩定版，請先關閉遊戲，依照該次發布的解除安裝指示操作，再啟用該模組的 Steam 工作坊版本。移除開發版不會復原已儲存至城市的變更。

<!-- Author: OpenAI-Codex_zh_docs. -->
