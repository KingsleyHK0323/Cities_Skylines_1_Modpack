using System.Collections.Generic;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Every user-facing string in the mod, in each language it has been translated
    /// into. English is the source language; the rest are additions to it.
    ///
    /// Adding a language is one method plus one line in BuildTables. Correcting a
    /// translation is one line in one method — the tables are grouped by language
    /// rather than by key so that a contributor who speaks one of them can find their
    /// block, change it, and touch nothing else.
    ///
    /// TRANSLATION STATUS: English is authoritative. The other ten were not written by
    /// native speakers and are offered as a starting point; corrections are welcome and
    /// wanted. See the project README for how to send one.
    ///
    /// The mod's NAME is deliberately absent. It is how the mod is found on the
    /// workshop and how players refer to it to each other, so it stays in English in
    /// every language, the way software product names usually do.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Language tags, in the order a picker should offer them. The stored setting
        /// and the dropdown both read this, so they cannot drift apart.
        /// </summary>
        internal static readonly string[] LanguageOrder =
        {
            "en", "zh_TW", "zh_CN", "ja", "ko", "de", "fr", "es", "pt-BR", "ru", "pl"
        };

        /// <summary>
        /// What each language calls itself, in the same order.
        ///
        /// These are NOT translated and must never be. Someone looking for their own
        /// language needs to find it written the way they write it — rendering the list
        /// in whatever language the panel happens to be in right now would hide the very
        /// entry they are hunting for.
        /// </summary>
        internal static readonly string[] LanguageNames =
        {
            "English", "繁體中文", "简体中文", "日本語", "한국어",
            "Deutsch", "Français", "Español", "Português", "Русский", "Polski"
        };

        public const string LabelLanguage = "label.language";
        public const string ModDescription = "mod.description";
        public const string GroupDiagnostics = "group.diagnostics";
        public const string ButtonTestLog = "btn.testLog";
        public const string ButtonBusPrefabs = "btn.busPrefabs";
        public const string ButtonMeasure = "btn.measure";
        public const string ButtonSnapshot = "btn.snapshot";
        public const string ButtonGraph = "btn.graph";
        // Release preview translations: OpenAI-Codex_aptp_release_boundary.
        public const string ButtonPlanningPreview = "btn.planningPreview";
        public const string LabelPlanningStatus = "label.planningStatus";
        public const string SnapshotStaleNote = "note.snapshotStale";
        public const string DemandProxyNote = "note.demandProxy";
#if DEVTOOLS
        // OpenAI-Codex: Development-only controls and statuses for confirmed proposal creation.
        public const string GroupRouteCreation = "group.routeCreation";
        public const string CheckRouteArm = "check.routeArm";
        public const string LabelRouteProposal = "label.routeProposal";
        public const string NoEligibleRouteProposal = "route.noEligibleProposal";
        public const string RouteProposalFormat = "route.proposalFormat";
        public const string ButtonRouteCreate = "btn.routeCreate";
        public const string RouteConfirmTitle = "route.confirm.title";
        public const string RouteConfirmCreate = "route.confirm.create";
        public const string RouteStatusIdle = "route.status.idle";
        public const string RouteStatusQueued = "route.status.queued";
        public const string RouteStatusRefused = "route.status.refused";
        public const string RouteStatusFailed = "route.status.failed";
        public const string RouteStatusChecking = "route.status.checking";
        public const string RouteStatusCreated = "route.status.created";
        public const string RouteStatusWarning = "route.status.warning";
#endif
#if DEVTOOLS
        public const string GroupSpike = "group.spike";
        public const string CheckSpikeArm = "check.spikeArm";
        public const string ButtonSpikeCreate = "btn.spikeCreate";
        public const string ButtonSpikeReport = "btn.spikeReport";
        public const string ButtonSpikeRemove = "btn.spikeRemove";
        public const string SpikeConfirmTitle = "spike.confirm.title";
        public const string SpikeConfirmCreate = "spike.confirm.create";
#endif

        internal static Dictionary<string, Dictionary<string, string>> BuildTables()
        {
            Dictionary<string, Dictionary<string, string>> tables =
                new Dictionary<string, Dictionary<string, string>>();

            tables["en"] = English();
            tables["zh_TW"] = TraditionalChinese();
            tables["zh_CN"] = SimplifiedChinese();
            tables["ja"] = Japanese();
            tables["ko"] = Korean();
            tables["de"] = German();
            tables["fr"] = French();
            tables["es"] = Spanish();
            tables["pt-BR"] = Portuguese();
            tables["ru"] = Russian();
            tables["pl"] = Polish();

            return tables;
        }

        // ------------------------------------------------------------- English (source)

        private static Dictionary<string, string> English()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Language";
            d[ModDescription] = "Early preview. Surveys your city's road and transit network to prepare automatic bus-line planning. Read-only: it does not change your city.";
            d[GroupDiagnostics] = "Diagnostics";
            d[ButtonTestLog] = "Write a test line to the log";
            d[ButtonBusPrefabs] = "List bus prefabs in detail";
            d[ButtonMeasure] = "Measure planning costs (brief pause)";
            d[ButtonSnapshot] = "Capture a world snapshot";
            d[ButtonGraph] = "Build the route graph";
            d[ButtonPlanningPreview] = "Generate a read-only planning preview";
            d[LabelPlanningStatus] = "Planning preview status";
            d[SnapshotStaleNote] = "Snapshot may become stale after city changes.";
            d[DemandProxyNote] = "Demand is estimated from five capacity slots per citizen unit, not actual people or ridership.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - create a selected route proposal (writes to your city)";
            d[CheckRouteArm] = "Arm route creation; every click still asks for confirmation";
            d[LabelRouteProposal] = "Eligible route proposal";
            d[NoEligibleRouteProposal] = "No proposal fits the 3-6 distinct-stop limit";
            d[RouteProposalFormat] = "Proposal {0} - {1} stops - {2}% coverage";
            d[ButtonRouteCreate] = "Create selected bus route";
            d[RouteConfirmTitle] = "Create selected route proposal?";
            d[RouteConfirmCreate] = "This creates one APTP-proposed bus line with 3-6 distinct stops in the loaded city. It changes the save; simulation effects cannot be rolled back by this tool. Back up your save before continuing. Continue?";
            d[RouteStatusIdle] = "Route creation requires confirmation for every click. Select a proposal and arm the control.";
            d[RouteStatusQueued] = "Selected route is queued for simulation-thread revalidation.";
            d[RouteStatusRefused] = "Creation refused. Refresh the planning preview and check the log for details.";
            d[RouteStatusFailed] = "Route creation failed and was rolled back where possible. Check the log.";
            d[RouteStatusChecking] = "Line {0} was created; checking stop connections and adjacent PathFailed flags asynchronously.";
            d[RouteStatusCreated] = "Line {0} passed the current stop-connection checks; no PathFailed flags were observed on adjacent segments.";
            d[RouteStatusWarning] = "Line {0} could not be fully validated. Check the log and inspect the route in your city.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - save round trip (writes to your city)";
            d[CheckSpikeArm] = "Arm the create button; every click still asks for confirmation";
            d[ButtonSpikeCreate] = "1. Create a test bus line";
            d[ButtonSpikeReport] = "2. Report test lines";
            d[ButtonSpikeRemove] = "3. Remove test lines (disabled: cannot undo)";
            d[SpikeConfirmTitle] = "Create test bus line?";
            d[SpikeConfirmCreate] = "This creates one APTP Test bus line with 3 to 6 stops on existing roads. The line can later be deleted in the game's public transport controls, but simulation changes while it runs cannot be rolled back. Back up your save before continuing. Continue?";
#endif
            return d;
        }

        // ------------------------------------------------------------- zh_TW 繁體中文

        private static Dictionary<string, string> TraditionalChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "語言";
            d[ModDescription] = "早期預覽版。調查城市的道路與大眾運輸網絡，為自動公車路線規劃作準備。唯讀：不會更動你的城市。";
            d[GroupDiagnostics] = "診斷";
            d[ButtonTestLog] = "將測試路線寫入記錄檔";
            d[ButtonBusPrefabs] = "列出公車 prefab 詳細資料";
            d[ButtonMeasure] = "量測規劃成本（會短暫停頓）";
            d[ButtonSnapshot] = "擷取世界快照";
            d[ButtonGraph] = "建立路線圖";
            d[ButtonPlanningPreview] = "產生唯讀規劃預覽";
            d[LabelPlanningStatus] = "規劃預覽狀態";
            d[SnapshotStaleNote] = "城市變動後，快照可能已過期。";
            d[DemandProxyNote] = "需求依每個市民單位五個容量位置估算，並非實際人數或乘客量。";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - 建立所選路線提案（會寫入城市）";
            d[CheckRouteArm] = "啟用路線建立；每次點擊仍會再次確認";
            d[LabelRouteProposal] = "可建立的路線提案";
            d[NoEligibleRouteProposal] = "目前沒有符合 3–6 個不同站點限制的提案";
            d[RouteProposalFormat] = "提案 {0} · {1} 個站點 · 覆蓋率 {2}%";
            d[ButtonRouteCreate] = "建立所選巴士路線";
            d[RouteConfirmTitle] = "要建立所選路線提案嗎？";
            d[RouteConfirmCreate] = "這會在目前載入的城市建立一條 APTP 路線提案，包含 3–6 個不同站點，並修改存檔。本工具無法復原模擬期間發生的影響。請先備份存檔。要繼續嗎？";
            d[RouteStatusIdle] = "路線建立每次都需確認。選擇提案並啟用控制項。";
            d[RouteStatusQueued] = "所選路線已排入模擬執行緒重新驗證。";
            d[RouteStatusRefused] = "已拒絕建立。請重新產生規劃預覽，並查看記錄了解原因。";
            d[RouteStatusFailed] = "路線建立失敗；可行時已回復新建路線。請查看記錄。";
            d[RouteStatusChecking] = "路線 {0} 已建立；正在非同步檢查站點連線及相鄰路段的 PathFailed 旗標。";
            d[RouteStatusCreated] = "路線 {0} 通過目前的站點連線檢查；相鄰路段未觀察到 PathFailed 旗標。";
            d[RouteStatusWarning] = "路線 {0} 未能完成驗證。請查看記錄並檢查城市中的路線。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 存檔往返測試（會寫入你的城市）";
            d[CheckSpikeArm] = "啟用建立按鈕；每次按下仍會再次要求確認";
            d[ButtonSpikeCreate] = "1. 建立測試公車路線";
            d[ButtonSpikeReport] = "2. 回報測試路線";
            d[ButtonSpikeRemove] = "3. 移除測試路線（已停用：無法還原）";
            d[SpikeConfirmTitle] = "要建立測試公車路線嗎？";
            d[SpikeConfirmCreate] = "這會在現有道路上建立一條有 3 至 6 個站點的 APTP Test 公車路線。稍後可在遊戲大眾運輸介面刪除該路線，但路線運行期間造成的模擬變化無法還原。繼續前請先備份存檔。要繼續嗎？";
#endif
            return d;
        }

        // ------------------------------------------------------------- zh_CN 简体中文

        private static Dictionary<string, string> SimplifiedChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "语言";
            d[ModDescription] = "早期预览版。调查城市的道路与公共交通网络，为自动公交线路规划做准备。只读：不会更改你的城市。";
            d[GroupDiagnostics] = "诊断";
            d[ButtonTestLog] = "将测试线路写入日志";
            d[ButtonBusPrefabs] = "列出公交 prefab 详细信息";
            d[ButtonMeasure] = "测量规划开销（会短暂停顿）";
            d[ButtonSnapshot] = "抓取世界快照";
            d[ButtonGraph] = "构建线路图";
            d[ButtonPlanningPreview] = "生成只读规划预览";
            d[LabelPlanningStatus] = "规划预览状态";
            d[SnapshotStaleNote] = "城市变化后，快照可能已过期。";
            d[DemandProxyNote] = "需求按每个市民单位五个容量位置估算，并非实际人数或客流量。";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - 创建所选路线提案（会写入城市）";
            d[CheckRouteArm] = "启用路线创建；每次点击仍会再次确认";
            d[LabelRouteProposal] = "可创建的路线提案";
            d[NoEligibleRouteProposal] = "当前没有符合 3–6 个不同站点限制的提案";
            d[RouteProposalFormat] = "提案 {0} · {1} 个站点 · 覆盖率 {2}%";
            d[ButtonRouteCreate] = "创建所选公交路线";
            d[RouteConfirmTitle] = "创建所选路线提案？";
            d[RouteConfirmCreate] = "这会在当前加载的城市中创建一条 APTP 路线提案，包含 3–6 个不同站点，并修改存档。本工具无法回滚模拟期间产生的影响。请先备份存档。继续吗？";
            d[RouteStatusIdle] = "路线创建每次都需确认。请选择提案并启用控件。";
            d[RouteStatusQueued] = "所选路线已排入模拟线程重新验证。";
            d[RouteStatusRefused] = "已拒绝创建。请重新生成规划预览，并查看日志了解详情。";
            d[RouteStatusFailed] = "路线创建失败；可行时已回滚新建路线。请查看日志。";
            d[RouteStatusChecking] = "路线 {0} 已创建；正在异步检查站点连接及相邻路段的 PathFailed 标志。";
            d[RouteStatusCreated] = "路线 {0} 通过当前站点连接检查；相邻路段未观察到 PathFailed 标志。";
            d[RouteStatusWarning] = "路线 {0} 未能完成验证。请查看日志并检查城市中的路线。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 存档往返测试（会写入你的城市）";
            d[CheckSpikeArm] = "启用创建按钮；每次点击仍会再次要求确认";
            d[ButtonSpikeCreate] = "1. 创建测试公交线路";
            d[ButtonSpikeReport] = "2. 报告测试线路";
            d[ButtonSpikeRemove] = "3. 移除测试线路（已停用：无法还原）";
            d[SpikeConfirmTitle] = "要创建测试公交线路吗？";
            d[SpikeConfirmCreate] = "这会在现有道路上创建一条包含 3 至 6 个站点的 APTP Test 公交线路。之后可在游戏公共交通界面删除该线路，但线路运行期间造成的模拟变化无法还原。继续前请先备份存档。要继续吗？";
#endif
            return d;
        }

        // ------------------------------------------------------------- 日本語

        private static Dictionary<string, string> Japanese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "言語";
            d[ModDescription] = "早期プレビュー版です。自動バス路線計画の準備として、都市の道路網と公共交通網を調査します。読み取り専用で、都市に変更を加えません。";
            d[GroupDiagnostics] = "診断";
            d[ButtonTestLog] = "テスト路線をログに書き出す";
            d[ButtonBusPrefabs] = "バスのプレハブを詳細表示";
            d[ButtonMeasure] = "計画処理のコストを計測（一時的に停止します）";
            d[ButtonSnapshot] = "ワールドスナップショットを取得";
            d[ButtonGraph] = "路線グラフを構築";
            d[ButtonPlanningPreview] = "読み取り専用の計画プレビューを生成";
            d[LabelPlanningStatus] = "計画プレビューの状態";
            d[SnapshotStaleNote] = "都市の変更後、スナップショットが古くなっている場合があります。";
            d[DemandProxyNote] = "需要は市民ユニットごとに定員5人として推定した値で、実際の人数や利用者数ではありません。";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - 選択したルート案を作成（都市に書き込みます）";
            d[CheckRouteArm] = "ルート作成を有効化（クリックごとに確認します）";
            d[LabelRouteProposal] = "作成可能なルート案";
            d[NoEligibleRouteProposal] = "異なる停留所が3～6か所の条件に合う案はありません";
            d[RouteProposalFormat] = "案 {0}・停留所 {1} か所・カバー率 {2}%";
            d[ButtonRouteCreate] = "選択したバス路線を作成";
            d[RouteConfirmTitle] = "選択したルート案を作成しますか？";
            d[RouteConfirmCreate] = "読み込まれた都市に、異なる停留所が3～6か所あるAPTPのバス路線案を1本作成し、セーブデータを変更します。シミュレーション中の影響はこのツールでは元に戻せません。続行前にセーブデータをバックアップしてください。続行しますか？";
            d[RouteStatusIdle] = "ルート作成ではクリックごとに確認が必要です。案を選んで操作を有効にしてください。";
            d[RouteStatusQueued] = "選択したルートをシミュレーションスレッドで再検証します。";
            d[RouteStatusRefused] = "作成を拒否しました。計画プレビューを更新し、ログで詳細を確認してください。";
            d[RouteStatusFailed] = "ルート作成に失敗しました。可能な場合は新規路線を削除しました。ログを確認してください。";
            d[RouteStatusChecking] = "路線 {0} を作成しました。停留所の接続と隣接セグメントの PathFailed フラグを非同期で確認中です。";
            d[RouteStatusCreated] = "路線 {0} は現在の停留所接続チェックに合格しました。隣接セグメントに PathFailed フラグは見つかりませんでした。";
            d[RouteStatusWarning] = "路線 {0} を完全には検証できませんでした。ログを確認し、都市内の路線を確認してください。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - セーブ往復テスト（都市に書き込みます）";
            d[CheckSpikeArm] = "作成ボタンを有効にします。クリックするたびに確認が表示されます";
            d[ButtonSpikeCreate] = "1. テストバス路線を作成";
            d[ButtonSpikeReport] = "2. テスト路線を報告";
            d[ButtonSpikeRemove] = "3. テスト路線を削除（復元不可のため無効）";
            d[SpikeConfirmTitle] = "テスト用バス路線を作成しますか？";
            d[SpikeConfirmCreate] = "既存の道路に3～6か所の停留所を持つ APTP Test のバス路線を1本作成します。路線は後でゲームの公共交通画面から削除できますが、運行中に起きたシミュレーションの変化は元に戻せません。続行する前にセーブデータをバックアップしてください。続行しますか？";
#endif
            return d;
        }

        // ------------------------------------------------------------- 한국어

        private static Dictionary<string, string> Korean()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "언어";
            d[ModDescription] = "초기 미리보기 버전입니다. 자동 버스 노선 계획을 준비하기 위해 도시의 도로망과 대중교통망을 조사합니다. 읽기 전용이며 도시를 변경하지 않습니다.";
            d[GroupDiagnostics] = "진단";
            d[ButtonTestLog] = "테스트 노선을 로그에 기록";
            d[ButtonBusPrefabs] = "버스 프리팹 상세 목록";
            d[ButtonMeasure] = "계획 비용 측정 (잠시 멈춤)";
            d[ButtonSnapshot] = "월드 스냅샷 캡처";
            d[ButtonGraph] = "노선 그래프 구축";
            d[ButtonPlanningPreview] = "읽기 전용 계획 미리보기 생성";
            d[LabelPlanningStatus] = "계획 미리보기 상태";
            d[SnapshotStaleNote] = "도시가 변경되면 스냅샷이 오래된 정보가 될 수 있습니다.";
            d[DemandProxyNote] = "수요는 시민 단위당 정원 5칸으로 추정한 값이며 실제 인원이나 승객 수가 아닙니다.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - 선택한 경로 제안 생성 (도시에 기록됨)";
            d[CheckRouteArm] = "경로 생성 활성화; 클릭할 때마다 다시 확인합니다";
            d[LabelRouteProposal] = "생성 가능한 경로 제안";
            d[NoEligibleRouteProposal] = "고유 정류장 3~6개 제한을 충족하는 제안이 없습니다";
            d[RouteProposalFormat] = "제안 {0} · 정류장 {1}개 · 커버리지 {2}%";
            d[ButtonRouteCreate] = "선택한 버스 노선 생성";
            d[RouteConfirmTitle] = "선택한 경로 제안을 생성할까요?";
            d[RouteConfirmCreate] = "현재 불러온 도시에 서로 다른 정류장 3~6개가 있는 APTP 버스 노선 하나를 생성하고 저장 데이터를 변경합니다. 시뮬레이션 중 발생한 영향은 이 도구로 되돌릴 수 없습니다. 계속하기 전에 저장 데이터를 백업하세요. 계속할까요?";
            d[RouteStatusIdle] = "경로를 만들 때마다 확인이 필요합니다. 제안을 선택하고 컨트롤을 활성화하세요.";
            d[RouteStatusQueued] = "선택한 경로를 시뮬레이션 스레드에서 다시 검증하도록 대기열에 넣었습니다.";
            d[RouteStatusRefused] = "생성이 거부되었습니다. 계획 미리보기를 새로 만들고 로그에서 자세한 내용을 확인하세요.";
            d[RouteStatusFailed] = "경로 생성에 실패했습니다. 가능한 경우 새 노선을 롤백했습니다. 로그를 확인하세요.";
            d[RouteStatusChecking] = "노선 {0}을(를) 만들었습니다. 정류장 연결과 인접 구간의 PathFailed 플래그를 비동기로 확인 중입니다.";
            d[RouteStatusCreated] = "노선 {0}이(가) 현재 정류장 연결 검사를 통과했습니다. 인접 구간에서 PathFailed 플래그가 관찰되지 않았습니다.";
            d[RouteStatusWarning] = "노선 {0}을(를) 완전히 검증하지 못했습니다. 로그를 확인하고 도시의 노선을 살펴보세요.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 저장 왕복 테스트 (도시에 기록됩니다)";
            d[CheckSpikeArm] = "생성 버튼을 활성화합니다. 클릭할 때마다 다시 확인합니다";
            d[ButtonSpikeCreate] = "1. 테스트 버스 노선 생성";
            d[ButtonSpikeReport] = "2. 테스트 노선 보고";
            d[ButtonSpikeRemove] = "3. 테스트 노선 제거 (복구 불가로 비활성화)";
            d[SpikeConfirmTitle] = "테스트 버스 노선을 생성할까요?";
            d[SpikeConfirmCreate] = "기존 도로에 정류장 3~6개가 있는 APTP Test 버스 노선 한 개를 생성합니다. 노선은 게임의 대중교통 화면에서 나중에 삭제할 수 있지만, 운행 중 발생한 시뮬레이션 변화는 되돌릴 수 없습니다. 계속하기 전에 저장 파일을 백업하세요. 계속할까요?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Deutsch

        private static Dictionary<string, string> German()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Sprache";
            d[ModDescription] = "Frühe Vorschau. Untersucht das Straßen- und Nahverkehrsnetz Ihrer Stadt, um die automatische Buslinienplanung vorzubereiten. Schreibgeschützt: Ihre Stadt wird nicht verändert.";
            d[GroupDiagnostics] = "Diagnose";
            d[ButtonTestLog] = "Testlinie ins Protokoll schreiben";
            d[ButtonBusPrefabs] = "Bus-Prefabs im Detail auflisten";
            d[ButtonMeasure] = "Planungskosten messen (kurze Pause)";
            d[ButtonSnapshot] = "Netzwerk-Momentaufnahme erstellen";
            d[ButtonGraph] = "Routengraph aufbauen";
            d[ButtonPlanningPreview] = "Schreibgeschützte Planungsvorschau erstellen";
            d[LabelPlanningStatus] = "Status der Planungsvorschau";
            d[SnapshotStaleNote] = "Nach Änderungen an der Stadt kann der Schnappschuss veraltet sein.";
            d[DemandProxyNote] = "Die Nachfrage ist anhand von fünf Kapazitätsplätzen pro Bürgereinheit geschätzt, nicht anhand tatsächlicher Personen- oder Fahrgastzahlen.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - ausgewählten Routenvorschlag erstellen (ändert die Stadt)";
            d[CheckRouteArm] = "Routenerstellung aktivieren; jeder Klick erfordert weiterhin eine Bestätigung";
            d[LabelRouteProposal] = "Erstellbarer Routenvorschlag";
            d[NoEligibleRouteProposal] = "Kein Vorschlag erfüllt die Grenze von 3–6 verschiedenen Haltestellen";
            d[RouteProposalFormat] = "Vorschlag {0} · {1} Haltestellen · Abdeckung {2}%";
            d[ButtonRouteCreate] = "Ausgewählte Buslinie erstellen";
            d[RouteConfirmTitle] = "Ausgewählten Routenvorschlag erstellen?";
            d[RouteConfirmCreate] = "Im geladenen Stadtgebiet wird eine APTP-Buslinie mit 3–6 verschiedenen Haltestellen erstellt und der Spielstand geändert. Auswirkungen während der Simulation kann dieses Werkzeug nicht rückgängig machen. Sichere den Spielstand vorher. Fortfahren?";
            d[RouteStatusIdle] = "Für jede Routenerstellung ist eine Bestätigung erforderlich. Wähle einen Vorschlag und aktiviere die Option.";
            d[RouteStatusQueued] = "Die ausgewählte Route wird zur erneuten Prüfung im Simulations-Thread vorgemerkt.";
            d[RouteStatusRefused] = "Erstellung abgelehnt. Erstelle eine neue Planungsvorschau und prüfe das Protokoll.";
            d[RouteStatusFailed] = "Routenerstellung fehlgeschlagen. Die neue Linie wurde, soweit möglich, zurückgerollt. Prüfe das Protokoll.";
            d[RouteStatusChecking] = "Linie {0} wurde erstellt; Haltestellenverbindungen und PathFailed-Flags auf angrenzenden Segmenten werden asynchron geprüft.";
            d[RouteStatusCreated] = "Linie {0} hat die aktuellen Prüfungen der Haltestellenverbindungen bestanden; auf angrenzenden Segmenten wurden keine PathFailed-Flags festgestellt.";
            d[RouteStatusWarning] = "Linie {0} konnte nicht vollständig geprüft werden. Prüfe das Protokoll und kontrolliere die Linie in deiner Stadt.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - Speicher-Rundlauf (schreibt in Ihre Stadt)";
            d[CheckSpikeArm] = "Erstellen-Schaltfläche aktivieren; jeder Klick erfordert erneut eine Bestätigung";
            d[ButtonSpikeCreate] = "1. Testbuslinie erstellen";
            d[ButtonSpikeReport] = "2. Testlinien melden";
            d[ButtonSpikeRemove] = "3. Testlinien entfernen (deaktiviert: nicht wiederherstellbar)";
            d[SpikeConfirmTitle] = "Testbuslinie erstellen?";
            d[SpikeConfirmCreate] = "Es wird eine APTP-Testbuslinie mit 3 bis 6 Haltestellen auf vorhandenen Straßen erstellt. Die Linie kann später in der ÖPNV-Ansicht des Spiels gelöscht werden, aber Simulationsänderungen während des Betriebs lassen sich nicht rückgängig machen. Sichern Sie Ihren Spielstand, bevor Sie fortfahren. Fortfahren?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Français

        private static Dictionary<string, string> French()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Langue";
            d[ModDescription] = "Aperçu préliminaire. Analyse le réseau routier et de transport en commun de votre ville pour préparer la planification automatique des lignes de bus. Lecture seule : votre ville n'est pas modifiée.";
            d[GroupDiagnostics] = "Diagnostic";
            d[ButtonTestLog] = "Écrire une ligne de test dans le journal";
            d[ButtonBusPrefabs] = "Lister les prefabs de bus en détail";
            d[ButtonMeasure] = "Mesurer les coûts de planification (courte pause)";
            d[ButtonSnapshot] = "Capturer un instantané du réseau";
            d[ButtonGraph] = "Construire le graphe des itinéraires";
            d[ButtonPlanningPreview] = "Générer un aperçu de planification en lecture seule";
            d[LabelPlanningStatus] = "État de l'aperçu de planification";
            d[SnapshotStaleNote] = "L'instantané peut être obsolète après des changements dans la ville.";
            d[DemandProxyNote] = "La demande est estimée à cinq places de capacité par unité de citoyens, et non à partir du nombre réel de personnes ou de voyageurs.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - créer une proposition de ligne sélectionnée (modifie la ville)";
            d[CheckRouteArm] = "Activer la création de ligne ; chaque clic demande toujours confirmation";
            d[LabelRouteProposal] = "Proposition de ligne disponible";
            d[NoEligibleRouteProposal] = "Aucune proposition ne respecte la limite de 3 à 6 arrêts distincts";
            d[RouteProposalFormat] = "Proposition {0} · {1} arrêts · couverture {2}%";
            d[ButtonRouteCreate] = "Créer la ligne de bus sélectionnée";
            d[RouteConfirmTitle] = "Créer la proposition de ligne sélectionnée ?";
            d[RouteConfirmCreate] = "Cette action crée dans la ville chargée une ligne de bus APTP avec 3 à 6 arrêts distincts et modifie la sauvegarde. Cet outil ne peut pas annuler les effets survenus pendant la simulation. Sauvegardez votre ville avant de continuer. Continuer ?";
            d[RouteStatusIdle] = "Chaque création de ligne demande une confirmation. Sélectionnez une proposition et activez l’option.";
            d[RouteStatusQueued] = "La ligne sélectionnée attend une nouvelle validation sur le thread de simulation.";
            d[RouteStatusRefused] = "Création refusée. Générez un nouvel aperçu et consultez le journal.";
            d[RouteStatusFailed] = "Échec de la création ; la nouvelle ligne a été annulée lorsque possible. Consultez le journal.";
            d[RouteStatusChecking] = "La ligne {0} a été créée ; vérification asynchrone des arrêts et des indicateurs PathFailed sur les segments adjacents.";
            d[RouteStatusCreated] = "La ligne {0} a réussi les vérifications actuelles de connexion des arrêts ; aucun indicateur PathFailed n’a été observé sur les segments adjacents.";
            d[RouteStatusWarning] = "La ligne {0} n’a pas pu être entièrement vérifiée. Consultez le journal et inspectez la ligne dans votre ville.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - aller-retour de sauvegarde (écrit dans votre ville)";
            d[CheckSpikeArm] = "Activer le bouton de création ; chaque clic demandera une nouvelle confirmation";
            d[ButtonSpikeCreate] = "1. Créer une ligne de bus de test";
            d[ButtonSpikeReport] = "2. Signaler les lignes de test";
            d[ButtonSpikeRemove] = "3. Supprimer les lignes de test (désactivé : irréversible)";
            d[SpikeConfirmTitle] = "Créer une ligne de bus de test ?";
            d[SpikeConfirmCreate] = "Cela créera une ligne de bus APTP Test avec 3 à 6 arrêts sur des routes existantes. La ligne pourra être supprimée dans l'interface de transport du jeu, mais les changements de simulation survenus pendant son fonctionnement ne pourront pas être annulés. Sauvegardez votre partie avant de continuer. Continuer ?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Español

        private static Dictionary<string, string> Spanish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Versión preliminar. Examina la red vial y de transporte público de tu ciudad para preparar la planificación automática de líneas de autobús. Solo lectura: no modifica tu ciudad.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escribir una línea de prueba en el registro";
            d[ButtonBusPrefabs] = "Listar los prefabs de autobús en detalle";
            d[ButtonMeasure] = "Medir los costes de planificación (breve pausa)";
            d[ButtonSnapshot] = "Capturar una instantánea de la red";
            d[ButtonGraph] = "Construir el grafo de rutas";
            d[ButtonPlanningPreview] = "Generar una vista previa de planificación de solo lectura";
            d[LabelPlanningStatus] = "Estado de la vista previa de planificación";
            d[SnapshotStaleNote] = "La instantánea puede quedar obsoleta después de cambios en la ciudad.";
            d[DemandProxyNote] = "La demanda se estima con cinco plazas de capacidad por unidad ciudadana, no con el número real de personas o pasajeros.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - crear una propuesta de ruta seleccionada (modifica la ciudad)";
            d[CheckRouteArm] = "Activar la creación de rutas; cada clic seguirá pidiendo confirmación";
            d[LabelRouteProposal] = "Propuesta de ruta disponible";
            d[NoEligibleRouteProposal] = "Ninguna propuesta cumple el límite de 3 a 6 paradas distintas";
            d[RouteProposalFormat] = "Propuesta {0} · {1} paradas · cobertura {2}%";
            d[ButtonRouteCreate] = "Crear la ruta de autobús seleccionada";
            d[RouteConfirmTitle] = "¿Crear la propuesta de ruta seleccionada?";
            d[RouteConfirmCreate] = "Se creará en la ciudad cargada una línea de autobús APTP con 3 a 6 paradas distintas y se modificará la partida. Esta herramienta no puede revertir los efectos durante la simulación. Haz una copia de seguridad antes de continuar. ¿Continuar?";
            d[RouteStatusIdle] = "Cada creación de ruta requiere confirmación. Selecciona una propuesta y activa el control.";
            d[RouteStatusQueued] = "La ruta seleccionada está en cola para volver a validarse en el hilo de simulación.";
            d[RouteStatusRefused] = "Creación rechazada. Genera una vista previa nueva y consulta el registro.";
            d[RouteStatusFailed] = "No se pudo crear la ruta; se revirtió la línea nueva cuando fue posible. Consulta el registro.";
            d[RouteStatusChecking] = "Se creó la línea {0}; se comprueban de forma asíncrona las conexiones de paradas y los indicadores PathFailed en segmentos adyacentes.";
            d[RouteStatusCreated] = "La línea {0} superó las comprobaciones actuales de conexión de paradas; no se observaron indicadores PathFailed en segmentos adyacentes.";
            d[RouteStatusWarning] = "No se pudo validar por completo la línea {0}. Consulta el registro e inspecciona la ruta en tu ciudad.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - ida y vuelta de guardado (escribe en tu ciudad)";
            d[CheckSpikeArm] = "Activar el botón de creación; cada clic volverá a pedir confirmación";
            d[ButtonSpikeCreate] = "1. Crear una línea de autobús de prueba";
            d[ButtonSpikeReport] = "2. Informar de las líneas de prueba";
            d[ButtonSpikeRemove] = "3. Eliminar líneas de prueba (desactivado: irreversible)";
            d[SpikeConfirmTitle] = "¿Crear una línea de autobús de prueba?";
            d[SpikeConfirmCreate] = "Esto creará una línea de autobús APTP Test con 3 a 6 paradas en carreteras existentes. Más tarde se puede eliminar desde la interfaz de transporte público del juego, pero no se pueden revertir los cambios de simulación producidos mientras funcione. Haz una copia de seguridad de la partida antes de continuar. ¿Continuar?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Português

        private static Dictionary<string, string> Portuguese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Prévia inicial. Analisa a rede viária e de transporte público da sua cidade para preparar o planejamento automático de linhas de ônibus. Somente leitura: não altera sua cidade.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escrever uma linha de teste no log";
            d[ButtonBusPrefabs] = "Listar os prefabs de ônibus em detalhe";
            d[ButtonMeasure] = "Medir os custos de planejamento (breve pausa)";
            d[ButtonSnapshot] = "Capturar um instantâneo da rede";
            d[ButtonGraph] = "Construir o grafo de rotas";
            d[ButtonPlanningPreview] = "Gerar uma prévia de planejamento somente leitura";
            d[LabelPlanningStatus] = "Estado da prévia de planejamento";
            d[SnapshotStaleNote] = "O instantâneo pode ficar desatualizado após mudanças na cidade.";
            d[DemandProxyNote] = "A demanda é estimada em cinco vagas de capacidade por unidade de cidadãos, não pelo número real de pessoas ou passageiros.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - criar uma proposta de rota selecionada (altera a cidade)";
            d[CheckRouteArm] = "Ativar criação de rotas; cada clique ainda exige confirmação";
            d[LabelRouteProposal] = "Proposta de rota disponível";
            d[NoEligibleRouteProposal] = "Nenhuma proposta atende ao limite de 3 a 6 paradas distintas";
            d[RouteProposalFormat] = "Proposta {0} · {1} paradas · cobertura {2}%";
            d[ButtonRouteCreate] = "Criar a rota de ônibus selecionada";
            d[RouteConfirmTitle] = "Criar a proposta de rota selecionada?";
            d[RouteConfirmCreate] = "Isso cria na cidade carregada uma linha de ônibus APTP com 3 a 6 paradas distintas e altera o salvamento. Esta ferramenta não pode reverter os efeitos durante a simulação. Faça backup do salvamento antes de continuar. Continuar?";
            d[RouteStatusIdle] = "Cada criação de rota exige confirmação. Selecione uma proposta e ative o controle.";
            d[RouteStatusQueued] = "A rota selecionada foi enfileirada para nova validação na thread de simulação.";
            d[RouteStatusRefused] = "Criação recusada. Gere uma nova prévia e consulte o log.";
            d[RouteStatusFailed] = "Falha ao criar a rota; a nova linha foi revertida quando possível. Consulte o log.";
            d[RouteStatusChecking] = "A linha {0} foi criada; verificações assíncronas das conexões das paradas e das flags PathFailed em segmentos adjacentes em andamento.";
            d[RouteStatusCreated] = "A linha {0} passou nas verificações atuais de conexão das paradas; nenhuma flag PathFailed foi observada em segmentos adjacentes.";
            d[RouteStatusWarning] = "Não foi possível validar completamente a linha {0}. Consulte o log e inspecione a rota na cidade.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - ida e volta de salvamento (escreve na sua cidade)";
            d[CheckSpikeArm] = "Ativar o botão de criação; cada clique pedirá confirmação novamente";
            d[ButtonSpikeCreate] = "1. Criar uma linha de ônibus de teste";
            d[ButtonSpikeReport] = "2. Relatar as linhas de teste";
            d[ButtonSpikeRemove] = "3. Remover linhas de teste (desativado: irreversível)";
            d[SpikeConfirmTitle] = "Criar uma linha de ônibus de teste?";
            d[SpikeConfirmCreate] = "Isso criará uma linha de ônibus APTP Test com 3 a 6 paradas em vias existentes. Depois, ela poderá ser removida na interface de transporte público do jogo, mas as mudanças na simulação ocorridas enquanto ela funcionar não poderão ser revertidas. Faça backup do seu jogo salvo antes de continuar. Continuar?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Русский

        private static Dictionary<string, string> Russian()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Язык";
            d[ModDescription] = "Ранняя предварительная версия. Изучает дорожную сеть и общественный транспорт города, чтобы подготовить автоматическое планирование автобусных маршрутов. Только чтение: город не изменяется.";
            d[GroupDiagnostics] = "Диагностика";
            d[ButtonTestLog] = "Записать тестовый маршрут в журнал";
            d[ButtonBusPrefabs] = "Подробный список префабов автобусов";
            d[ButtonMeasure] = "Измерить затраты на планирование (короткая пауза)";
            d[ButtonSnapshot] = "Сделать снимок сети";
            d[ButtonGraph] = "Построить граф маршрутов";
            d[ButtonPlanningPreview] = "Создать предварительный план только для чтения";
            d[LabelPlanningStatus] = "Состояние предварительного плана";
            d[SnapshotStaleNote] = "После изменений города снимок может устареть.";
            d[DemandProxyNote] = "Спрос оценивается по пяти местам вместимости на единицу жителей и не отражает фактическое число людей или пассажиров.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - создать выбранное предложение маршрута (изменит город)";
            d[CheckRouteArm] = "Включить создание маршрута; каждый щелчок всё равно требует подтверждения";
            d[LabelRouteProposal] = "Доступное предложение маршрута";
            d[NoEligibleRouteProposal] = "Нет предложений с ограничением от 3 до 6 разных остановок";
            d[RouteProposalFormat] = "Предложение {0} · остановок: {1} · покрытие: {2}%";
            d[ButtonRouteCreate] = "Создать выбранный автобусный маршрут";
            d[RouteConfirmTitle] = "Создать выбранный маршрут?";
            d[RouteConfirmCreate] = "В загруженном городе будет создан один автобусный маршрут APTP с 3–6 разными остановками, и сохранение изменится. Это средство не может отменить последствия работы симуляции. Перед продолжением сделайте резервную копию сохранения. Продолжить?";
            d[RouteStatusIdle] = "Каждое создание маршрута требует подтверждения. Выберите предложение и включите флажок.";
            d[RouteStatusQueued] = "Выбранный маршрут поставлен в очередь для повторной проверки в потоке симуляции.";
            d[RouteStatusRefused] = "Создание отклонено. Обновите предварительный план и проверьте журнал.";
            d[RouteStatusFailed] = "Не удалось создать маршрут; новую линию удалили, если это было возможно. Проверьте журнал.";
            d[RouteStatusChecking] = "Линия {0} создана; асинхронно проверяются соединения остановок и флаги PathFailed на соседних сегментах.";
            d[RouteStatusCreated] = "Линия {0} прошла текущие проверки соединений остановок; на соседних сегментах флаги PathFailed не обнаружены.";
            d[RouteStatusWarning] = "Не удалось полностью проверить линию {0}. Проверьте журнал и осмотрите маршрут в городе.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - цикл сохранения и загрузки (записывает в ваш город)";
            d[CheckSpikeArm] = "Включить кнопку создания; каждое нажатие потребует нового подтверждения";
            d[ButtonSpikeCreate] = "1. Создать тестовый автобусный маршрут";
            d[ButtonSpikeReport] = "2. Отчёт по тестовым маршрутам";
            d[ButtonSpikeRemove] = "3. Удалить тестовые маршруты (отключено: восстановление невозможно)";
            d[SpikeConfirmTitle] = "Создать тестовый автобусный маршрут?";
            d[SpikeConfirmCreate] = "Будет создан один автобусный маршрут APTP Test с 3–6 остановками на существующих дорогах. Позже маршрут можно удалить в меню транспорта игры, но изменения симуляции, произошедшие во время его работы, отменить нельзя. Перед продолжением создайте резервную копию сохранения. Продолжить?";
#endif
            return d;
        }

        // ------------------------------------------------------------- Polski

        private static Dictionary<string, string> Polish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Język";
            d[ModDescription] = "Wczesna wersja podglądowa. Analizuje sieć dróg i transportu publicznego w mieście, aby przygotować automatyczne planowanie linii autobusowych. Tylko do odczytu: nie zmienia miasta.";
            d[GroupDiagnostics] = "Diagnostyka";
            d[ButtonTestLog] = "Zapisz linię testową do dziennika";
            d[ButtonBusPrefabs] = "Wypisz szczegóły prefabrykatów autobusów";
            d[ButtonMeasure] = "Zmierz koszty planowania (krótka pauza)";
            d[ButtonSnapshot] = "Wykonaj migawkę sieci";
            d[ButtonGraph] = "Zbuduj graf tras";
            d[ButtonPlanningPreview] = "Utwórz podgląd planowania tylko do odczytu";
            d[LabelPlanningStatus] = "Stan podglądu planowania";
            d[SnapshotStaleNote] = "Po zmianach w mieście migawka może być nieaktualna.";
            d[DemandProxyNote] = "Popyt oszacowano na podstawie pięciu miejsc na jednostkę mieszkańców; nie jest to faktyczna liczba osób ani pasażerów.";
#if DEVTOOLS
            d[GroupRouteCreation] = "Spike 2 - utwórz wybraną propozycję trasy (zmienia miasto)";
            d[CheckRouteArm] = "Włącz tworzenie tras; każde kliknięcie nadal wymaga potwierdzenia";
            d[LabelRouteProposal] = "Dostępna propozycja trasy";
            d[NoEligibleRouteProposal] = "Brak propozycji spełniającej limit 3–6 różnych przystanków";
            d[RouteProposalFormat] = "Propozycja {0} · przystanki: {1} · pokrycie: {2}%";
            d[ButtonRouteCreate] = "Utwórz wybraną linię autobusową";
            d[RouteConfirmTitle] = "Utworzyć wybraną propozycję trasy?";
            d[RouteConfirmCreate] = "Wczytane miasto otrzyma jedną linię autobusową APTP z 3–6 różnymi przystankami, a zapis zostanie zmieniony. To narzędzie nie może cofnąć skutków działania symulacji. Przed kontynuowaniem wykonaj kopię zapisu. Kontynuować?";
            d[RouteStatusIdle] = "Każde tworzenie trasy wymaga potwierdzenia. Wybierz propozycję i włącz tę opcję.";
            d[RouteStatusQueued] = "Wybrana trasa czeka na ponowną weryfikację w wątku symulacji.";
            d[RouteStatusRefused] = "Odmówiono utworzenia. Wygeneruj nowy podgląd planu i sprawdź dziennik.";
            d[RouteStatusFailed] = "Nie udało się utworzyć trasy; nową linię cofnięto, jeśli było to możliwe. Sprawdź dziennik.";
            d[RouteStatusChecking] = "Utworzono linię {0}; asynchronicznie sprawdzane są połączenia przystanków i flagi PathFailed na sąsiednich segmentach.";
            d[RouteStatusCreated] = "Linia {0} przeszła bieżące sprawdzenia połączeń przystanków; nie wykryto flag PathFailed na sąsiednich segmentach.";
            d[RouteStatusWarning] = "Nie udało się w pełni sprawdzić linii {0}. Sprawdź dziennik i obejrzyj trasę w mieście.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - test zapisu i wczytania (zapisuje w Twoim mieście)";
            d[CheckSpikeArm] = "Aktywuj przycisk tworzenia; każde kliknięcie ponownie wyświetli prośbę o potwierdzenie";
            d[ButtonSpikeCreate] = "1. Utwórz testową linię autobusową";
            d[ButtonSpikeReport] = "2. Zgłoś linie testowe";
            d[ButtonSpikeRemove] = "3. Usuń linie testowe (wyłączone: brak możliwości przywrócenia)";
            d[SpikeConfirmTitle] = "Czy utworzyć testową linię autobusową?";
            d[SpikeConfirmCreate] = "Utworzona zostanie jedna linia autobusowa APTP Test z 3–6 przystankami na istniejących drogach. Później można ją usunąć w interfejsie transportu publicznego gry, ale zmian symulacji, które nastąpią podczas jej działania, nie można cofnąć. Przed kontynuowaniem wykonaj kopię zapasową zapisu gry. Kontynuować?";
#endif
            return d;
        }
    }
}
