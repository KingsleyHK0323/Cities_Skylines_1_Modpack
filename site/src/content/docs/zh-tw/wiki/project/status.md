---
title: 模組狀態與替代目標
description: "附日期的 15 個模組及 22 項已確認替代方向總覽，分別說明實作與驗收狀態。"
---

狀態快照：**2026 年 11 月 2 日**。專案包含**處於不同階段的 15 個模組**，並非 15 個已完成的下載項目。下方 22 項外部目標是已確認的替代方向，不代表替代已完成、可以取消訂閱，或已證明相容。

設計交付、原始碼實作、雲端編譯、可下載產物、公開發布及遊戲驗收是不同里程碑。專案已確認各模組的責任範圍，但不代表相關功能已遷移或通過遊戲驗收；實作細節及介面仍可能屬於提案。本快照採用的證據尚未證明任何完整替代已完成。

## 最新交付快照 — 2026 年 11 月 2 日

15 個遊戲模組中，**10 個未暫停模組已有成功的遊戲 DLL 雲端建置**。AI Gaming 保留舊預覽版本並維持暫停。Railway 及 Vehicle 的來源批次待雲端編譯；Intercity 及 Wireless 仍需補最小遊戲入口。基礎 DLL 編譯成功不代表完整功能或遊戲內載入驗收。

| 近期遊戲 DLL | 已核對雲端執行 | 目前功能界線 |
| --- | --- | --- |
| Realistic City Time | [41382255222](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41382255222) | 只觀測；未實作 1:1 時間控制及加速 |
| Full Map Unlock And Expansion | [41383511005](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41383511005) | 只觀測；不解鎖或購地 |
| Realistic Urban Population | [41390004437](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41390004437) | 未啟用控制的基礎版；不寫人口或需求 |
| Realistic City First Response | [41391816077](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41391816077) | 不派遣、抑火或改交通 |
| Realistic Border Control | [41403613247](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41403613247) | 不設旅客配額，未接即時旅客讀取 |
| Realistic Import Export Control | [41404552970](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41404552970) | 不作貨運配對或經濟控制 |
| Realistic Pedestrian Traffic Control | [41405888872](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41405888872) | 不改過路規則、號誌、路徑或標線 |

其餘三個已核對 DLL 是 AI Improve、Public Transport Planning 及 Public Transport Vehicle Management。近期基礎版產物保留 14 日，下載可能需要登入 GitHub；它們不是個別公開發布版本。過往建置成功不保證產物仍可下載。

### 發布及規劃中的行為

已部署整包流程收集 AI Improve、Planning 及 Vehicle Management 的既有成功產物，香港時間每日 08:00、13:00、20:00 及手動觸發；GitHub 可能延遲執行。新目標則是在雲端編譯含實際開發工具的開發版，再打包及發布；**這項改造尚未實作**。正式版規劃為擁有者核准工單、雲端編譯排除開發工具，再交本機僅上傳執行器處理，尚未完整實作。

Cities Skylines 1 Modpack Dev Manager 目前只有本機分支及需求筆記，**尚無可安裝 App**。預期提供逐模組更新偵測、單一／批量安裝、更新及解除安裝。

已確認的後續並存行為：同一模組正式版及開發版同時安裝時，進遊戲只顯示警告；不自動停用、解除安裝、取消訂閱或阻止載入。此警告尚未套用至全部模組，不代表已驗證並存相容性。開發版遊戲名稱將採以下固定英文格式：

```text
<Module Name> - Development Version. This version has DevTool. Be careful; the development version can let your save be broken.
```

這是名稱規格，不表示全部基礎版已具備開發工具。自動交通燈管理屬 Vehicle Traffic Control 的規劃功能，目前基礎版未實作或啟用。

**Intersection Marking Tool 不取代。** Advanced Stop Selection Revisited、Advanced Building Level Control 及 Unlimited Outside Connections Revisited 仍是未確認候選；尚未批准新增建築控制模組。

<!-- Updated by OpenAI-Codex on 2026-11-02. -->
## 較早的功能範圍背景（11 月 1 日）

| 模組 | 證據階段 | 發布與驗收界線 |
| --- | --- | --- |
| [AI Gaming](/Cities_Skylines_1_Modpack/zh-tw/ai-gaming/) | 開發已由專案擁有人暫緩；已有較早的預覽版本 | 現有文件只作歷史背景，不代表恢復工作。遊戲內執行尚未驗證。 |
| [AI Improve](/Cities_Skylines_1_Modpack/zh-tw/ai-improve/) | 已有實作；已交付責任及遷移清單 | 已確認責任包括 PathUnits、CitizenUnits 及跨運具路徑協調；新增責任與遷移未被證實完成，模組組合驗收待完成。 |
| [Automated Public Transport Planning](/Cities_Skylines_1_Modpack/zh-tw/automated-public-transport-planning/) | 已有開發原型；已交付共用站點缺口分析 | 可在確認後嘗試建立路線或移動既有路線站點，並可能修改存檔。共用站點替代、遊戲內執行及存檔重新載入驗收尚未完成。 |
| Realistic Urban Population | 已確認可調整住宅／商業／工業或工作需求的方向 | 各類可選原生需求或固定 0–100 目標，預設原生。工業與辦公耦合仍待核實；遊戲實作未通過驗收。 |
| Realistic City First Response | 已交付首版設計及遷移清單 | 緊急應變是已確認責任；消防應變細節及遷移尚未通過遊戲驗收。 |
| Realistic Vehicle Traffic Control | 已交付道路功能清單及單一寫入方設計 | 首版先觀察／預覽，後續計畫分階段加入人工確認控制；車道及號誌細節仍屬設計工作。 |
| Realistic Pedestrian Traffic Control | 已交付行人／行人過路處設計及介面計畫 | 首版先觀察／預覽，後續計畫分階段加入人工確認控制；限制、視覺變更及整合仍屬設計工作。 |
| Realistic Railway Traffic Control | 已交付鐵路安全模型及遷移計畫 | 首版先觀察／預覽，後續計畫分階段加入人工確認控制；軌道安全責任不代表控制功能已遷移或驗收。 |
| Realistic Border Control | 已交付旅客資料及配額預覽設計 | 首版先觀察／預覽，後續計畫分階段加入人工確認控制；尚未證實有主動限制抵達人數的功能。 |
| Realistic Import Export Control | 已交付貨運配對及貿易設計 | 首版先觀察／預覽，後續計畫分階段加入人工確認控制；尚未證實有主動貨運配對功能。 |
| Intercity Resource Sharing | 已交付水／電力／熱能交換設計 | 資料來源、守恆及結算仍屬設計工作，並非控制另一個存檔。 |
| Automated Public Transport Vehicle Management | 已實作唯讀公車觀察；DLL 已編譯並透過 Actions 產物交付 | 尚無公開正式版本或工作坊項目。車隊控制提案及工作坊訂閱版本的遊戲驗收待完成。 |
| Full Map Unlock And Expansion | 已交付 25 區里程碑／存檔設計 | 25 區購買與 81 區擴展是不同範圍；不宣稱已有經驗證的解鎖實作。 |
| Wireless Utilities And Global Coverage | 已交付單一城市電力／供水／污水／供暖設計 | 設施行為及水管需求仍屬設計選項，與城際交換分開看待。 |
| Realistic City Time | 已有基礎文件／分支，已交付時間一致性設計 | 已確認方向：正常速度下現實一分鐘等於遊戲一分鐘，預設 1×、最高 10×，可輸入小數倍率。小數精度及詳細行為仍待決定，遊戲驗收待完成。 |

### Vehicle Management 建置產物證據

[雲端執行 41250632157](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41250632157) 已成功完成，對應提交為 `3b14890f3ab37c4a66799230ba6ac496a1627871`。2026 年 11 月 1 日核對時，產物 `vehicle-management-41250632157-1`（ID `13060977517`）仍存在且未過期。專案交接已回報及核對交付的 DLL 為 **11,776 位元組**；壓縮產物大小是另一項量測。

Actions 建置產物可能過期，下載時亦可能需要登入 GitHub。上述證據不代表已有公開正式版本、工作坊 ID、成功的遊戲內觀察，或已實作車隊控制。規劃中的版本名稱不代表已公開發布。

## 已確認的外部替代方向

下表列出唯一的外部工作坊 ID。多個內部模組共用的目標只計算一次。已確認的責任範圍不代表每項功能、遷移及相容規則均已定案；本清單不宣稱已全面覆蓋，也不替未解決的工作指定負責方。

名稱及版本文字沿用附日期的既有清冊，未即時逐項核實現行版本或可用性。

| 外部目標 | 工作坊參考 |
| --- | --- |
| TM:PE 11.9.4.1 STABLE (Traffic Manager: President Edition) | [1637663252](https://steamcommunity.com/sharedfiles/filedetails/?id=1637663252) |
| Remove Need For Power Lines | [572888650](https://steamcommunity.com/sharedfiles/filedetails/?id=572888650) |
| Remove Need For Pipes | [576997275](https://steamcommunity.com/sharedfiles/filedetails/?id=576997275) |
| All 25 Areas purchasable | [405810376](https://steamcommunity.com/sharedfiles/filedetails/?id=405810376) |
| Real Time 2.8 | [3059406297](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297) |
| More Simulation Speed Options 2 | [3343115359](https://steamcommunity.com/sharedfiles/filedetails/?id=3343115359) |
| Realistic Population 2 2.2.4 | [2025147082](https://steamcommunity.com/sharedfiles/filedetails/?id=2025147082) |
| Lifecycle Rebalance Revisited 1.6.9 | [2027161563](https://steamcommunity.com/sharedfiles/filedetails/?id=2027161563) |
| Outside Traffic Balancer | [2893645082](https://steamcommunity.com/sharedfiles/filedetails/?id=2893645082) |
| Improved Public Transport 3 | [3690061052](https://steamcommunity.com/sharedfiles/filedetails/?id=3690061052) |
| Transfer Manager CE v3.1.39 | [2804719780](https://steamcommunity.com/sharedfiles/filedetails/?id=2804719780) |
| Improved Lane Connections v2 | [2119310640](https://steamcommunity.com/sharedfiles/filedetails/?id=2119310640) |
| SharedStopEnabler | [2096382380](https://steamcommunity.com/sharedfiles/filedetails/?id=2096382380) |
| Rain Firefighting | [629370088](https://steamcommunity.com/sharedfiles/filedetails/?id=629370088) |
| Reversible Tram AI | [2740907672](https://steamcommunity.com/sharedfiles/filedetails/?id=2740907672) |
| Optimised Outside Connections | [1721492498](https://steamcommunity.com/sharedfiles/filedetails/?id=1721492498) |
| More Outside Interaction | [1706703944](https://steamcommunity.com/sharedfiles/filedetails/?id=1706703944) |
| Infinite Demand | [1522785644](https://steamcommunity.com/sharedfiles/filedetails/?id=1522785644) |
| More PathUnits 1.1.3 | [2710657019](https://steamcommunity.com/sharedfiles/filedetails/?id=2710657019) |
| More CitizenUnits | [2654364627](https://steamcommunity.com/sharedfiles/filedetails/?id=2654364627) |
| Hide TM:PE Crosswalks: Renewed | [3682758360](https://steamcommunity.com/sharedfiles/filedetails/?id=3682758360) |
| Right of Way - Priority for Emergency Vehicles | [3808510434](https://steamcommunity.com/sharedfiles/filedetails/?id=3808510434) |

專案已將 More CitizenUnits 的替代方向，包括全域 CitizenUnits 容量及資料完整性責任，分配給 AI Improve。容量擴充及安全移除尚未通過遊戲驗收；使用擴充容量的存檔可能依賴其提供方。

附日期的收藏清單共有 210 個唯一項目：21 項已確認目標、2 項已識別依賴及 187 項未分類項目。Harmony 及 Patch Loader 是依賴，並非替代目標。這不是功能覆蓋百分比，也不是可取消訂閱的證明：收藏亦包含本專案不會重製的素材與工具。

## 相容性與選用協作

模組獨立運作及選用的公共運輸協作是設計目標，並非安全組合運作的證明。本索引沒有引入強制共用 DLL。已確認的責任範圍不代表協作協定或功能遷移已完成；詳細介面仍屬設計工作。不包括建築或其他 3D 素材製作。

現有預覽版本請參考[開發版安裝指南](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。請備份存檔、使用測試城市，並只啟用每個模組的一個版本。不要只因外部模組列在目標清單便停用它們。雲端／檔案核對與 Steam 工作坊訂閱版本的遊戲驗收必須分開看待。

<!-- Author: OpenAI-Codex_zh_docs. -->

<!-- Author: OpenAI-Codex_status_worker. -->

目前確認方向總數為22；新增的Right of Way不在上述210項歷史收藏快照中，因此不把187項未分類扣成186，也不聲稱目前線上收藏仍有210項。
<!-- Updated by OpenAI-Codex. -->
