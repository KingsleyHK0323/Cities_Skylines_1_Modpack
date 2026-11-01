---
title: 模組狀態與替代目標
description: "附日期的 15 個模組及 20 項已確認替代方向總覽，分別說明實作與驗收狀態。"
---

狀態快照：**2026 年 11 月 1 日**。專案包含**處於不同階段的 15 個模組**，並非 15 個已完成的下載項目。下方 20 項外部目標是已確認的替代方向，不代表替代已完成、可以取消訂閱，或已證明相容。

設計交付、原始碼實作、雲端編譯、可下載產物、公開發布及工作坊訂閱版本的遊戲驗收是不同里程碑。詳細責任分工與介面除非已獲明確確認，否則仍屬**提案**。本快照採用的證據尚未證明任何完整替代已完成。

## 模組狀態

| 模組 | 證據階段 | 發布與驗收界線 |
| --- | --- | --- |
| AI Gaming | 開發已由專案擁有人暫緩；已有較早的預覽版本 | 現有文件只作歷史背景，不代表恢復工作。遊戲內執行尚未驗證。 |
| AI Improve | 已有實作；已交付責任及遷移清單 | 現有專案／下載頁面與提議的新責任分開看待。模組組合驗收待完成。 |
| Automated Public Transport Planning | 已有開發原型；已交付共用站點缺口分析 | 可在確認後嘗試建立路線並修改存檔。共用站點替代及工作坊訂閱版本的遊戲驗收尚未完成。 |
| Realistic Urban Population | 已交付可調整住宅／商業／工業需求設計 | 模式、數值範圍及預設值仍屬提案；不宣稱已有經驗證的遊戲實作。 |
| Realistic City First Response | 已交付首版設計及遷移清單 | 消防應變行為及實作選項仍待決策；不宣稱已完成替代驗收。 |
| Realistic Vehicle Traffic Control | 已交付道路功能清單及單一寫入方設計 | 車道預覽／套用及交通燈控制權細節仍屬提案。 |
| Realistic Pedestrian Traffic Control | 已交付行人／行人過路處設計及介面計畫 | 限制、視覺變更及整合仍屬設計提案。 |
| Realistic Railway Traffic Control | 已交付鐵路安全模型及遷移計畫 | 觀察與雙向電車之間的優先次序尚未決定；不宣稱已有實際控制。 |
| Realistic Border Control | 已交付旅客資料及配額預覽設計 | 預覽與主動限制抵達人數之間的選擇尚未決定。 |
| Realistic Import Export Control | 已交付貨運配對及貿易設計 | 建議與主動配對之間的選擇尚未決定。 |
| Intercity Resource Sharing | 已交付水／電力／熱能交換設計 | 資料來源、守恆及結算仍屬設計工作，並非控制另一個存檔。 |
| Automated Public Transport Vehicle Management | 已實作唯讀公車觀察；DLL 已編譯並透過 Actions 產物交付 | 尚無公開正式版本或工作坊項目。車隊控制提案及工作坊訂閱版本的遊戲驗收待完成。 |
| Full Map Unlock And Expansion | 已交付 25 區里程碑／存檔設計 | 25 區購買與 81 區擴展是不同範圍；不宣稱已有經驗證的解鎖實作。 |
| Wireless Utilities And Global Coverage | 已交付單一城市電力／供水／污水／供暖設計 | 設施行為及水管需求仍屬設計選項，與城際交換分開看待。 |
| Realistic City Time | 已有基礎文件／分支，已交付時間一致性設計 | 已確認方向：正常速度為 1:1，預設 1×，最高 10×。中間速度控制及詳細行為仍屬提案。 |

### Vehicle Management 建置產物證據

[雲端執行 41250632157](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41250632157) 已成功完成，對應提交為 `3b14890f3ab37c4a66799230ba6ac496a1627871`。2026 年 11 月 1 日核對時，產物 `vehicle-management-41250632157-1`（ID `13060977517`）仍存在且未過期。專案交接已回報及核對交付的 DLL 為 **11,776 位元組**；壓縮產物大小是另一項量測。

Actions 建置產物可能過期，下載時亦可能需要登入 GitHub。上述證據不代表已有公開正式版本、工作坊 ID、成功的遊戲內觀察，或已實作車隊控制。Release／Development 圖示標籤表示預定版本類型，並非發布狀態。

## 已確認的外部替代方向

下表列出唯一的外部工作坊 ID。多個內部模組共用的目標只計算一次。詳細負責方、功能覆蓋、遷移及相容規則仍屬提案；本清單不宣稱已全面覆蓋，也不替未解決的工作指定最終負責方。

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
| Hide TM:PE Crosswalks: Renewed | [3682758360](https://steamcommunity.com/sharedfiles/filedetails/?id=3682758360) |

附日期的收藏清單共有 210 個唯一項目：20 項已確認目標、2 項已識別依賴及 188 項未分類項目。Harmony 及 Patch Loader 是依賴，並非替代目標。這不是覆蓋百分比：收藏亦包含本專案不會重製的素材與工具。

## 相容性與選用協作

模組獨立運作及選用的公共運輸協作是設計目標，並非安全組合運作的證明。本索引沒有引入強制共用 DLL。詳細協作協定、單一寫入方分工及功能遷移仍屬提案。不包括建築或其他 3D 素材製作。

現有預覽版本請參考[開發版安裝指南](/Cities_Skylines_1_Modpack/zh-tw/wiki/development-downloads/)。請備份存檔、使用測試城市，並只啟用每個模組的一個版本。不要只因外部模組列在目標清單便停用它們。雲端／檔案核對與 Steam 工作坊訂閱版本的遊戲驗收必須分開看待。

<!-- Author: OpenAI-Codex_zh_docs. -->
