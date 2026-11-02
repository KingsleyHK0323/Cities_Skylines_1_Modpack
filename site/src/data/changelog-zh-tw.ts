import type { ChangeEntry } from '../lib/changelog';

// Author: OpenAI-Codex_header_review. Historical entries retain their original claims and credits.
// Updated by OpenAI-Codex on 2026-11-01.
const translations: Record<string, { title: string; bullets: string[] }> = {
    // Signature: OpenAI-Codex.
    'MDX source comment correction': {
        title: 'MDX 來源註解修正',
        bullets: [
            '將兩個 Wiki 首頁改用 MDX 支援的註解格式，讓雲端網站建置可解析內容。',
            '署名：OpenAI-Codex。',
        ],
    },
    // Updated by OpenAI-Codex on 2026-11-02. Keep counts aligned with the source parser.
    'Central development bundle workflow': {
        title: '集中開發版整包工作流程',
        bullets: [
            '新增排程及手動觸發的三模組 ZIP 收集器，核對來源、完整產物，並在來源未變時跳過重複發布。',
            '公開開發預發行版本前下載核對草稿 ZIP，並在整包頻道內維護最新開發版及歷史標示。',
            '七項雲端契約檢查、公開 ZIP 驗證及來源未變跳過均已通過，工作流程已啟用。',
            '署名：OpenAI-Codex。',
        ],
    },
    'Delivery status and distribution guidance': {
        title: '交付狀態與分發指南',
        bullets: [
            '更新共用狀態摘要及雙語狀態／下載指南，加入七個基礎 DLL 建置參考與已核對的三模組整包。',
            '區分規劃中的開發工具編譯、正式版核准上傳、並存警告及管理器，避免與已部署功能混淆。',
            '列明包括 Right of Way 的 22 項確認替代方向；三項新候選維持未確認，IMT 不取代。',
            '保留網站既有未提交修改；本輪只更新本機來源，不宣稱已部署或通過遊戲驗收。',
            '署名：OpenAI-Codex。',
            '同步四個既有 Wiki 首頁／路線圖摘要的 22 項計數，保留獨立的歷史收藏清冊。署名：OpenAI-Codex。',
        ],
    },
	'Project overview and Development guidance': {
		title: '專案總覽及開發版指南',
		bullets: [
			'重新整理兩種語言的首頁，以整個專案、目前狀態及入門指南為主，並保留現有模組專頁與美術素材。',
			'將附日期的狀態索引更新為 15 個模組及 21 個已確認替代方向，分開說明產品決策、實作及遊戲驗收。',
			'修正三個過時的 GitHub 開發版連結，並說明 AI Gaming 所選資料、自動加入的 DLC 識別資訊及逐次確認。',
			'更新 AI Improve 開發版功能資料、相容性及遊戲內指南，並釐清公車路線編輯；遊戲驗收仍待完成。',
			'作者：OpenAI-Codex。',
		],
	},
	'Workshop Development removal and complete Chinese navigation': {
		title: '工作坊開發版本下架與完整繁中導覽',
		bullets: [
			'將 Steam 工作坊開發版本標示為已下架，並保留獨立的 GitHub 預先發行版本下載。',
			'完成共用公告、專案摘要、文件連結目的地及繁中頁面內容的在地化，包括更新紀錄與框架標題標籤，並修正中文警告的粗體格式。',
			'新增繁體中文安裝及模組狀態指南，並翻譯側邊欄標籤。',
			'作者：OpenAI-Codex。',
		],
	},
	'Illustration semantics and preference controls': {
		title: '插畫語意與偏好設定控制項',
		bullets: [
			'將交通插畫的分頁語意改為圖說選擇按鈕群組，保留方向鍵、Home 及 End 導覽和瀏覽器修飾鍵快捷鍵，並新增可供讀取的禮貌狀態訊息。',
			'依內附翻譯或英文備用文字的語言，標示 AI Improve 參考資料的標題、描述及調整設定標籤。',
			'將主題及語言控制項的操作範圍提高至最少 44px，為選單加入清晰焦點標示，並在 Wiki 標題內側顯示焦點框。容許緊湊的路線圖卡片及長標籤縮小，避免超出頁面。',
			'保留現有網站圖示、Apple 及資訊清單的 PNG 素材；未找到合適的既有 WebP 品牌素材，因此沒有加入替代美術圖。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'Navigation accessibility and dated project status': {
		title: '導覽無障礙與附日期的專案狀態',
		bullets: [
			'移除會裁切模組選單的窄螢幕導覽捲動容器，容許導覽換行，並擴大操作範圍。',
			'加深深色主題主要按鈕的背景，令其與白色文字的對比超過 4.5:1。',
			'將首頁的相容性保證改為驗收界線，並連結至 15 個模組／20 個目標的狀態索引。標示 AI Gaming 開發暫停，未確定的職責分配則標示為提案。',
			'將車輛管理狀態更新為已有 Actions 產物的唯讀原型，並與公開發行版本及工作坊供應狀態分開說明。',
			'為共用下載按鈕群組加入英文語言標記。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'Planned vehicle management artwork': {
		title: '規劃中的車輛管理美術圖',
		bullets: [
			'在兩個首頁的獨立規劃中專案區段，加入已批准的 Automated Public Transport Vehicle Management 正式版及開發版 WebP 圖示。',
			'繼續以 WebP 提供現有專案美術圖，並保留原始圖片。此規劃中專案不提供下載或工作坊連結。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'Compact homepage Development notice': {
		title: '緊湊的首頁開發版本公告',
		bullets: [
			'將過大的首頁公告改為共用、緊湊且適應不同螢幕的公告，並保留下載操作及存檔注意事項。',
			'翻譯繁中首頁公告，並標示其安裝指南連結通往英文頁面。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'Mobile documentation and download navigation': {
		title: '手機文件與下載導覽',
		bullets: [
			'將現行專案網址及工作流程的儲存庫檢查更新為 KingsleyHK0323 帳號名稱。歷史狀態記錄保留原有帳號名稱。',
			'在窄螢幕維持單行文件頁首，語言及主題控制項則可在原生手機選單使用。',
			'在工作坊連結前加入醒目的 GitHub 開發版本下載按鈕，並在文件側邊欄加入開發版本下載項目。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'Development downloads and installation': {
		title: '開發版本下載與安裝',
		bullets: [
			'加入 AI Gaming、AI Improve 及 Automated Public Transport Planning 各自的 GitHub 開發預先發行版本連結，並說明安裝相依項目及存檔警告。穩定版本仍在 Steam 工作坊提供。',
			'說明 AI Gaming 的 DLL 與輔助程式必須配對，以及 Automated Public Transport Planning 開發原型可嘗試建立已確認的路線。雲端建置及檔案檢查並不等於遊戲內驗收。',
			'作者：OpenAI-Codex_Responsible: Modpack_Website。',
		],
	},
	'License terms aligned': {
		title: '統一授權條款',
		bullets: [
			'更新授權檔案、文件及網站頁尾：模組及網站程式碼採用 PolyForm Noncommercial License 1.0.0；網站內容（包括文字及圖片）採用 CC BY-NC-SA 4.0。',
			'版權所有 © Kingsley。第三方元件保留各自的授權。',
			'作者：OpenAI-Codex。',
		],
	},
	'Language switch label fixed': {
		title: '修正語言切換標籤',
		bullets: [
			'網站頁首的語言按鈕現在顯示切換後的語言：繁中頁面顯示 English，英文頁面顯示繁體中文。螢幕閱讀器標籤亦已修正為相同意思。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'Automated Public Transport Planning guide updated': {
		title: '更新 Automated Public Transport Planning 指南',
		bullets: [
			'指南及路線圖現在說明模組只涵蓋一般城市公車，其他公共交通類型將在日後逐一加入。',
			'指南說明開發版本的唯讀規劃預覽（測試中）及較安全的測試控制：每次建立測試路線均須確認，並停用從模組移除路線的功能。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'Development Versions on the website': {
		title: '網站加入開發版本',
		bullets: [
			'AI Improve、Automated Public Transport Planning 及 AI Gaming 的開發版本現已在 Steam 工作坊公開，各模組頁面均有相關連結。Automated Public Transport Planning 及 AI Gaming 的正式版本仍標示為即將推出。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'AI Improve compatibility note corrected': {
		title: '修正 AI Improve 相容性說明',
		bullets: [
			'AI Improve 相容性頁面不再聲稱緊急車輛成本調整可與 TM:PE 一起運作。測試顯示啟用 TM:PE 時，此調整目前無法載入；修正正在開發版本測試。可選的途中緊急重新規劃路線功能不受影響。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'Workshop preview images per edition': {
		title: '各版本專用工作坊預覽圖片',
		bullets: [
			'每個工作坊項目現在都有專屬預覽圖片，正式版本及開發版本均以版本徽章標示。',
			'上傳工作流程會預填所屬版本的圖片。圖片是新美術圖的 JPG 副本，每張均低於 Steam 的 1 MB 限制。',
			'新圖片會在該項目下次上傳後顯示於工作坊頁面。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'New website and wiki': {
		title: '全新網站與 Wiki',
		bullets: [
			'使用 Astro 及 Starlight 重建網站，結合產品網站與提供搜尋功能的英語及繁體中文 Wiki。網站從 `site/` 資料夾建置，每次變更均由工作流程部署至 GitHub Pages。',
			'模組頁面解釋各模組對玩家的用途，顯示工作坊按鈕（隱藏項目標示為即將推出），並提供功能瀏覽器、可摺疊設定預覽、規劃中的公車規劃導覽及 AI Gaming 模式說明。',
			'Wiki 涵蓋入門、AI Improve 的每項功能及設定、透過 UnifiedUI 在遊戲內使用、相容性、常見問題、路線圖及貢獻方法。',
			'修正 AI Improve 相容性描述：它不會更改車輛容量，並將消防派遣選擇交由 Transfer Manager CE 處理。',
			'作者：OpenAI-Codex（初始建置）、Anthropic-Claude_Code（建置修正及重新設計）。',
		],
	},
	'Release and Development Workshop pipeline': {
		title: '正式版及開發版工作坊發佈流程',
		bullets: [
			'全部六個工作坊項目已登記：AI Improve（3782858610、3823575900）、Automated Public Transport Planning（3823739383、3823739653）及 AI Gaming（3823740106、3823740277）。AI Gaming 上傳工作流程不再被封鎖。',
			'開發版本一律包含 DevTools 建置，並可在每次推送至模組分支後自動上傳。正式版本一律不包含 DevTools，只按要求上傳並等待批准。每次建置均在上傳前檢查 DevTools 內容。',
			'預覽圖片現在取自符合 Steam 1 MB 預覽限制的 JPG 圖示組。',
			'設定好自託管執行器前，自動開發版本上傳及正式版本批准維持關閉。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'AI Gaming 0.1.0 early preview': {
		title: 'AI Gaming 0.1.0 早期預覽',
		bullets: [
			'首個 AI Gaming 預覽提供英文及繁體中文設定。玩家可將唯讀城市摘要（人口、資金及遊戲內日期）寫入遊戲記錄。',
			'此版本不會連接 AI 模型、建立網絡連線或控制模擬。',
			'作者：OpenAI-Codex（由 Anthropic-Claude_Code 記錄）。',
		],
	},
	'Automated Public Transport Planning 0.2.0 early preview': {
		title: 'Automated Public Transport Planning 0.2.0 早期預覽',
		bullets: [
			'全部 11 種語言的模組描述現在準確說明這是唯讀城市調查預覽，而非已完成的路線規劃。',
			'存檔往返測試及其更改城市的控制項不包含在標準建置中，只保留於 DevTools 建置；唯讀診斷則繼續保留。',
			'模組及組件版本均為 0.2.0。',
			'作者：OpenAI-Codex（由 Anthropic-Claude_Code 記錄）。',
		],
	},
	'Workshop descriptions from the mod branches': {
		title: '從模組分支取得工作坊描述',
		bullets: [
			'上傳工作流程現在可從模組分支的 `Workshop/Release.txt` 或 `Workshop/Development.txt` 更新工作坊描述。沒有該檔案時會保留現有描述。',
			'上傳仍不會更改可見度及標籤。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'Workshop item IDs pinned in upload workflows': {
		title: '在上傳工作流程固定工作坊項目 ID',
		bullets: [
			'登記 AI Improve 工作坊項目：正式版本 3782858610 及開發版本 3823575900。兩個上傳工作流程現在均會預填所屬 ID。',
			'上傳腳本現在會拒絕與該模組及版本已登記項目不符的 ID，亦會拒絕尚未登記 ID 的項目（Transport 及 AI Gaming）。',
			'AI Improve 上傳不再將建置複製到執行器帳號自己的 Mods 資料夾。',
			'工作流程仍須完成自託管執行器及 SteamCMD 設定才可執行。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'AI Improve author credit and settings links': {
		title: 'AI Improve 作者署名與設定連結',
		bullets: [
			'AI Improve：內容管理員顯示的模組描述現在署名 Kingsley，而非 SpaceSquare。為保持相容性，內部 Harmony ID 維持不變。',
			'AI Improve：設定頁連結不再指向已移除的獨立儲存庫。「GitHub Source」開啟 AI_Improve 分支，「Documentation Wiki」開啟 AI Improve 網站頁面，「Changelog」開啟網站更新紀錄，「Report an Issue」開啟本儲存庫的問題表單。',
			'下次工作坊更新後於遊戲生效。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'GPL-3.0 license': {
		title: 'GPL-3.0 授權',
		bullets: [
			'為網站及全部三個模組分支（AI Improve、Automated Public Transport Planning、AI Gaming）加入 GNU General Public License v3.0，全文逐字取自 https://www.gnu.org/licenses/gpl-3.0.txt。',
			'在各分支 README 加入授權說明，並更新 DEVELOPMENT.md 的授權備註。第三方元件保留各自的授權。',
			'作者：Anthropic-Claude_Code。',
		],
	},
	'Website experience and artwork': {
		title: '網站體驗與美術圖',
		bullets: [
			'將三個專案的專用美術圖加入首頁選擇器及專案頁面；每張圖片均標示為美術圖，並非遊戲畫面。',
			'改善窄螢幕頁首及頁面內容換行，並移除裝飾性的更新紀錄分隔線。',
			'加入雙語及各頁專用的描述與美術圖替代文字，並按日期排列產生的更新紀錄項目。',
			'加入細微的城市美術動畫，並遵循減少動態效果偏好。',
			'作者：OpenAI-Codex。',
		],
	},
	'Manual Steam Workshop upload workflows': {
		title: '手動 Steam 工作坊上傳工作流程',
		bullets: [
			'為 AI Improve 及 Automated Public Transport Planning 加入分開且須確認的正式版本及開發版本工作流程。',
			'為 AI Gaming 的兩個版本加入被封鎖的工作流程，因為該分支沒有可建置的模組專案或套件定義。',
			'加入本機自託管執行器及 SteamCMD 設定指引；Steam 憑證及預覽圖片不會儲存在儲存庫。',
		],
	},
	'Collection attribution': {
		title: '合集署名',
		bullets: [
			'在網站品牌下方加入「by Kingsley」，並將合集署名更新為 Kingsley。',
			'在合集署名旁加入小型內嵌 SVG 香港旗幟圖示，確保跨平台顯示一致。',
		],
	},
	'Initial local website': {
		title: '初始本機網站',
		bullets: [
			'加入合集首頁及三個專案介紹頁面。',
			'加入預設英文及深色主題，並獨立保留繁體中文及淺色主題偏好。',
			'加入可用鍵盤操作的專案選擇器、靜態備用內容及原創概念城市插畫。',
			'將 AI Gaming 標示為尚未開始，並明確保留其他未驗證的專案狀態。',
			'加入適應不同螢幕的版面、減少動態效果行為、本機文件及無需相依套件的頁面產生功能。',
			'改編使用者提供的 Uiverse GitHub 按鈕參考，並保留來源署名及 MIT 聲明。',
			'本項記錄描述本機網站開發，並非宣佈模組發行或公開部署。',
		],
	},
	'Original repository website': {
		title: '原始儲存庫網站',
		bullets: [
			'建立模組包網站，提供登陸頁及各模組的獨立子資料夾。',
			'將 Automated Public Transport Planning 預覽從舊儲存庫（preview 分支提交 0af5dc6）移至 `automated-public-transport-planning/`，並將連結指向本儲存庫。',
		],
	},
	'Website documentation entry points': {
		title: '網站文件入口',
		bullets: [
			'將各分支檢出目錄的本機 README 入口檔案改為對應網站頁面的連結。',
			'將網站開發指南保留為 DEVELOPMENT.md。',
			'加入由本檔案產生的更新紀錄頁面。',
		],
	},
	'Website branding and social preview': {
		title: '網站品牌與社交預覽',
		bullets: [
			'將網站圖示加入共用網站頁首及網站小圖示。',
			'在產生頁面的 Open Graph 及 Twitter 中繼資料加入 1280 × 640 社交預覽圖片。',
			'作者：OpenAI-Codex。',
		],
	},
};

export function getLocalizedChangeEntries(entries: ChangeEntry[]): ChangeEntry[] {
	return entries.map((entry) => {
		// Fail explicitly instead of publishing untranslated change entries. OpenAI-Codex_header_review.
		if (!Object.hasOwn(translations, entry.title)) {
			throw new Error(`Missing Traditional Chinese changelog translation: ${entry.title}`);
		}
		const translation = translations[entry.title];
		if (translation.bullets.length !== entry.bullets.length) {
			throw new Error(`Traditional Chinese changelog bullet count mismatch: ${entry.title}`);
		}
		return { ...entry, title: translation.title, bullets: [...translation.bullets] };
	});
}

// Author: OpenAI-Codex.
