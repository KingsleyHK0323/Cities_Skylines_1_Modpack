// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.IO.Compression;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class Program {
    public static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
    [STAThread] static void Main(string[] args) {
        if(args.Length==4&&args[0]=="--recovery-probe"){
            try{Presentation.Load(args[1]);Presentation.Language=args[2];var notice=new RecoveryNotice();var states=new List<object>();
                foreach(object raw in (object[])Json.DeserializeObject(args[3])){notice.Observe((IDictionary<string,object>)raw);states.Add(new{active=notice.Active,text=notice.Active?Presentation.T("Recovery / lock warning: {0}",Presentation.T(notice.Key)):""});}
                var bytes=Encoding.UTF8.GetBytes(Json.Serialize(new{ok=true,states=states})+"\n");using(var stream=Console.OpenStandardOutput()){stream.Write(bytes,0,bytes.Length);}
            }catch{Console.WriteLine("{\"ok\":false}");}return;
        }
        if((args.Length==7||args.Length==8) && args[0]=="--presentation-probe") {
            try { Presentation.Load(args[1]); Presentation.Language=args[2]; var colors=Presentation.Palette(args[3],args[4]=="true",args[5]=="true");
                var formatArgs=args.Length==8?(object[])Json.DeserializeObject(args[7]):new object[0];
                var output=Encoding.UTF8.GetBytes(Json.Serialize(new {ok=true,text=Presentation.T(args[6],formatArgs),background=colors[0].ToArgb(),foreground=colors[1].ToArgb(),buttonBackground=colors[2].ToArgb(),selectionBackground=colors[3].ToArgb(),selectionForeground=colors[4].ToArgb(),dark=Presentation.IsDark(args[3],args[4]=="true")})+"\n");using(var stream=Console.OpenStandardOutput()){stream.Write(output,0,output.Length);}
            }catch { Console.WriteLine("{\"ok\":false}"); } return;
        }
        if (args.Length == 2 && args[0] == "--inspect") {
            try { var info = AssemblyName.GetAssemblyName(args[1]); Console.WriteLine(Json.Serialize(new { ok = true, name = info.Name, version = info.Version.ToString() })); }
            catch { Console.WriteLine("{\"ok\":false}"); } return;
        }
        if (args.Length == 1 && args[0] == "--inspect-stdin") {
            try {
                var input = new StringBuilder(); var buffer = new char[4096]; int count;
                while ((count = Console.In.Read(buffer, 0, buffer.Length)) > 0) { if (input.Length + count > 44739248) throw new Exception(); input.Append(buffer,0,count); }
                var bytes = Convert.FromBase64String(input.ToString()); if (bytes.Length == 0 || bytes.Length > 33554432) throw new Exception();
                // Reflection-only metadata: no module/type initialization or DLL code execution.
                var info = Assembly.ReflectionOnlyLoad(bytes).GetName();
                Console.WriteLine(Json.Serialize(new { ok = true, name = info.Name, version = info.Version.ToString() }));
            } catch { Console.WriteLine("{\"ok\":false}"); } return;
        }
        if (args.Length == 1 && args[0] == "--unzip-stdin") {
            try {
                var input = new StringBuilder(); var buffer = new char[8192]; int count;
                while ((count=Console.In.Read(buffer,0,buffer.Length))>0) { if(input.Length+count>89478488) throw new Exception(); input.Append(buffer,0,count); }
                var bytes=Convert.FromBase64String(input.ToString()); if(bytes.Length>67108864) throw new Exception();
                var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var files=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var entries=new List<object>(); long total=0;
                using(var memory=new MemoryStream(bytes,false)) using(var zip=new ZipArchive(memory,ZipArchiveMode.Read,false)) {
                    if(zip.Entries.Count>512) throw new Exception();
                    foreach(var entry in zip.Entries) {
                        string name=entry.FullName; bool directory=name.EndsWith("/"); string canonical=directory?name.Substring(0,name.Length-1):name;
                        if(canonical.Length==0 || canonical.Length>240 || canonical.StartsWith("/") || canonical.Any(c=>c<32||c==127||"\\:<>\"|?*".Contains(c)) || canonical.Split('/').Any(p=>p.Length==0||p=="."||p==".."||p.EndsWith(".")||p.EndsWith(" ")||System.Text.RegularExpressions.Regex.IsMatch(p,@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) || !names.Add(canonical)) throw new Exception();
                        int type=(entry.ExternalAttributes>>16)&0xF000;
                        if(type==0xA000 || (entry.ExternalAttributes&0x400)!=0 || type!=0&&type!=0x8000&&type!=0x4000) throw new Exception();
                        if(directory) { if(entry.Length!=0) throw new Exception(); continue; }
                        if(entry.Length<1||entry.Length>33554432||(total+=entry.Length)>67108864) throw new Exception(); files.Add(canonical);
                    }
                    foreach(string name in names) { var parts=name.Split('/'); for(int i=1;i<parts.Length;i++) if(files.Contains(String.Join("/",parts.Take(i)))) throw new Exception(); }
                    foreach(var entry in zip.Entries) {
                        if(entry.FullName.EndsWith("/")) continue;
                        using(var source=entry.Open()) using(var output=new MemoryStream()) {
                            var block=new byte[16384]; long received=0;
                            while((count=source.Read(block,0,block.Length))>0) { received+=count; if(received>entry.Length) throw new Exception(); output.Write(block,0,count); }
                            if(received!=entry.Length) throw new Exception(); entries.Add(new { path=entry.FullName, bytes=Convert.ToBase64String(output.ToArray()) });
                        }
                    }
                }
                Program.Json.MaxJsonLength=100*1024*1024; Console.WriteLine(Program.Json.Serialize(new {ok=true,entries=entries}));
            } catch { Console.WriteLine("{\"ok\":false}"); } return;
        }
        if (args.Length == 1 && args[0] == "--game-running") {
            try { Console.WriteLine(Json.Serialize(new { running = Process.GetProcessesByName("Cities").Length > 0 || Process.GetProcessesByName("CitiesSkylines").Length > 0 })); }
            catch { Console.WriteLine("{\"running\":true}"); } return;
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ManagerForm());
    }
}

static class Presentation {
    static IDictionary<string,object> languages=new Dictionary<string,object>();
    public static string Language="en";
    public static void Load(string path) {
        if(new FileInfo(path).Length>1048576)throw new Exception("Translation resource is oversized.");
        var catalog=Program.Json.DeserializeObject(File.ReadAllText(path,Encoding.UTF8)) as IDictionary<string,object>;
        languages=(IDictionary<string,object>)catalog["languages"];
    }
    public static string T(string key,params object[] args) {
        object value; string template=key;
        if(languages.ContainsKey(Language)&&((IDictionary<string,object>)languages[Language]).TryGetValue(key,out value))template=Convert.ToString(value);
        else if(languages.ContainsKey("en")&&((IDictionary<string,object>)languages["en"]).TryGetValue(key,out value))template=Convert.ToString(value);
        return args.Length==0?template:String.Format(System.Globalization.CultureInfo.CurrentCulture,template,args);
    }
    public static bool IsDark(string theme,bool systemDark) { return theme=="dark" || theme=="system"&&systemDark; }
    public static Color[] Palette(string theme,bool systemDark,bool highContrast) {
        if(highContrast)return new[]{SystemColors.Window,SystemColors.WindowText,SystemColors.Control,SystemColors.Highlight,SystemColors.HighlightText,SystemColors.GrayText};
        return IsDark(theme,systemDark)?new[]{Color.FromArgb(28,33,40),Color.FromArgb(235,240,247),Color.FromArgb(45,55,68),Color.FromArgb(36,83,133),Color.White,Color.FromArgb(167,181,199)}:
            new[]{Color.FromArgb(245,247,250),Color.FromArgb(27,36,48),Color.White,Color.FromArgb(29,80,136),Color.White,Color.FromArgb(83,98,118)};
    }
}

sealed class RecoveryNotice {
    public bool Active {get;private set;} public string Key {get;private set;}
    static bool Flag(IDictionary<string,object> value,string key){object item;return value.TryGetValue(key,out item)&&item is bool&&(bool)item;}
    public void Observe(IDictionary<string,object> message){
        object raw;var result=message.TryGetValue("result",out raw)?raw as IDictionary<string,object>:null;
        if(result!=null){
            if(Flag(result,"settingsRecoveryCleared")){Active=false;Key="";}
            if(result.TryGetValue("settingsWarning",out raw)&&raw!=null&&raw.ToString().Length>0){Active=true;Key="Recovery data or a settings lock may remain. Inspect before retrying; no automatic recovery is claimed.";}
        }
        if(message.TryGetValue("error",out raw)){
            if(Convert.ToString(raw)=="LOCAL_STATE_RECOVERY_REQUIRED"){Active=true;Key="Recovery data or a settings lock may remain. Inspect before retrying; no automatic recovery is claimed.";}
            else if(Convert.ToString(raw)=="LOCAL_STATE_BUSY"&&!Active){Active=true;Key="Another App instance is saving settings. Retry after it finishes.";}
        }
    }
}

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
static class SourceFreshness {
    static bool Flag(IDictionary<string,object> row,string key){object value;return row.TryGetValue(key,out value)&&value is bool&&(bool)value;}
    static bool Number(object value,out double number){
        number=0;if(value==null||value is string||value is bool)return false;
        switch(Type.GetTypeCode(value.GetType())){
            case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Int16: case TypeCode.UInt16:
            case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Int64: case TypeCode.UInt64:
            case TypeCode.Single: case TypeCode.Double: case TypeCode.Decimal:
                try{number=Convert.ToDouble(value,System.Globalization.CultureInfo.InvariantCulture);return !Double.IsNaN(number)&&!Double.IsInfinity(number);}catch{return false;}
            default:return false;
        }
    }
    public static bool Expire(IDictionary<string,object> row,double now,string reason){
        object raw;bool hasTime=row.TryGetValue("checkedAtMs",out raw)&&raw!=null;
        object status;row.TryGetValue("status",out status);
        object latest;row.TryGetValue("latestBuild",out latest);
        bool candidate=Flag(row,"canInstall")||Flag(row,"canUpdate")||Flag(row,"canRequestInstall")||Flag(row,"canRequestUpdate")||
            Flag(row,"matchesCandidate")||Convert.ToString(status)=="current"||!String.IsNullOrEmpty(Convert.ToString(latest));
        if(!hasTime&&!candidate)return false;
        double checkedAt;
        if(hasTime&&Number(raw,out checkedAt)&&!Double.IsNaN(now)&&!Double.IsInfinity(now)&&now>=checkedAt&&now-checkedAt<3600000)return false;
        row["canInstall"]=false;row["canUpdate"]=false;row["canRequestInstall"]=false;row["canRequestUpdate"]=false;
        row["matchesCandidate"]=false;row["status"]="stale / unknown";row["reasonText"]=reason;row["reasonDiagnostic"]="";
        return true;
    }
}

sealed class AutomaticRefreshClock {
    DateTime lastSeen; DateTime nextCheck;
    public AutomaticRefreshClock(DateTime now){Reset(now);}
    public void Reset(DateTime now){lastSeen=now;nextCheck=now.AddHours(1);}
    public void DeferUntil(DateTime time){if(time>nextCheck)nextCheck=time;}
    public bool TakeDue(DateTime now,bool ready,bool busy){
        if(now<lastSeen){nextCheck=now;}
        lastSeen=now;
        if(!ready||busy||now<nextCheck)return false;
        nextCheck=now.AddHours(1);return true;
    }
}

sealed class DetailViewState {
    bool showingResults;
    public void ShowResults(){showingResults=true;}
    public void ShowSelection(){showingResults=false;}
    public bool ShouldRefreshSelection(bool selectedChanged){return selectedChanged&&!showingResults;}
}

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
static class SelectionSafety {
    public static int HiddenCount(IEnumerable<KeyValuePair<bool,bool>> rows) { return rows.Count(row => row.Key && !row.Value); }
    public static bool EligibleSelection(bool selected,bool visible,bool eligible) { return visible ? eligible : selected; }
}

sealed class ManagerForm : Form {
    readonly RecoveryNotice recoveryNotice=new RecoveryNotice(); readonly Label recoveryWarning=new Label();TableLayoutPanel mainLayout;
    readonly DetailViewState detailView=new DetailViewState();
    readonly ComboBox languageChoice=new ComboBox(), themeChoice=new ComboBox();
    readonly Dictionary<Control,string> staticText=new Dictionary<Control,string>();
    readonly string[] languageIds={"en","zh-Hant","zh-Hans","ja"}, themeIds={"system","light","dark"};
    bool applyingPreferences; string selectedTheme="system"; bool catalogUnavailable;
    static string T(string key,params object[] args) { return Presentation.T(key,args); }
    string ResourceRoot() { string root=AppDomain.CurrentDomain.BaseDirectory;return File.Exists(Path.Combine(root,"translations.json"))?root:Directory.GetParent(root.TrimEnd(Path.DirectorySeparatorChar)).FullName; }
    readonly DataGridView grid = new DataGridView();
    readonly TextBox search = new TextBox();
    readonly TextBox detail = new TextBox();
    readonly Label target = new Label();
    readonly Label summary = new Label();
    readonly Label progress = new Label();
    readonly FlowLayoutPanel tools = new FlowLayoutPanel();
    readonly ProgressBar activity = new ProgressBar();
    readonly Button refresh = new Button(), install = new Button(), update = new Button(), remove = new Button();
    Process backend; bool busy; bool firstCheckSent; long nextId; string targetPath = ""; string mode = "unconfigured";
    readonly Timer automatic = new Timer(); readonly AutomaticRefreshClock refreshClock = new AutomaticRefreshClock(DateTime.UtcNow);
    static string TextValue(IDictionary<string, object> item, string key) { object v; return item.TryGetValue(key, out v) && v != null ? v.ToString() : ""; }
    static bool Flag(IDictionary<string, object> item, string key) { object v; return item.TryGetValue(key, out v) && v is bool && (bool)v; }
    Button MakeButton(string label, EventHandler click) {
        var button = new Button { Text = label, AutoSize = true, MinimumSize = new Size(108, 40), Margin = new Padding(0, 0, 10, 10), AccessibleName = label };
        button.Click += click; tools.Controls.Add(button); return button;
    }
    public ManagerForm() {
        try { Presentation.Load(Path.Combine(ResourceRoot(),"translations.json")); } catch { catalogUnavailable=true; }
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ShowIcon = true; ShowInTaskbar = true;
        Text = "Cities Skylines 1 Modpack Dev Manager"; Width = 1320; Height = 850; MinimumSize = new Size(980, 680);
        Font = new Font("Segoe UI", 10F); AutoScaleMode = AutoScaleMode.Dpi; BackColor = Color.FromArgb(245,247,250);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, Padding = new Padding(22), BackColor = BackColor };mainLayout=layout;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); Controls.Add(layout);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,0));recoveryWarning.Dock=DockStyle.Fill;recoveryWarning.BorderStyle=BorderStyle.FixedSingle;recoveryWarning.Padding=new Padding(8);recoveryWarning.Font=new Font(Font.FontFamily,10,FontStyle.Bold);layout.Controls.Add(recoveryWarning,0,8);
        var heading=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,44));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,28));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,28));
        heading.Controls.Add(new Label { Text = "Development modules", Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Dock = DockStyle.Fill },0,0);
        var languagePanel=new FlowLayoutPanel{Dock=DockStyle.Fill};languagePanel.Controls.Add(new Label{Text="Language",AutoSize=true,Margin=new Padding(0,8,8,0)});
        languageChoice.DropDownStyle=ComboBoxStyle.DropDownList;languageChoice.Width=150;languageChoice.AccessibleName="Language";
        languageChoice.Items.AddRange(new object[]{"English","\u7e41\u9ad4\u4e2d\u6587","\u7b80\u4f53\u4e2d\u6587","\u65e5\u672c\u8a9e"});languageChoice.SelectedIndex=0;languageChoice.SelectedIndexChanged+=delegate{SavePreferences("language");};languagePanel.Controls.Add(languageChoice);heading.Controls.Add(languagePanel,1,0);
        var themePanel=new FlowLayoutPanel{Dock=DockStyle.Fill};themePanel.Controls.Add(new Label{Text="Theme",AutoSize=true,Margin=new Padding(0,8,8,0)});
        themeChoice.DropDownStyle=ComboBoxStyle.DropDownList;themeChoice.Width=155;themeChoice.AccessibleName="Theme";themeChoice.Items.AddRange(new object[]{"Follow Windows","Light","Dark"});themeChoice.SelectedIndex=0;themeChoice.SelectedIndexChanged+=delegate{SavePreferences("theme");};themePanel.Controls.Add(themeChoice);heading.Controls.Add(themePanel,2,0);layout.Controls.Add(heading,0,0);
        target.Dock = DockStyle.Fill; target.AutoEllipsis = true; target.Text = "Starting local application..."; target.AccessibleName = "Active installation destination"; layout.Controls.Add(target, 0, 1);
        var find = new FlowLayoutPanel { Dock = DockStyle.Fill }; find.Controls.Add(new Label { Text = "Find a module", AutoSize = true, Margin = new Padding(0,8,12,0) });
        search.Width = 340; search.AccessibleName = "Find a module"; search.TextChanged += delegate { ApplyFilter(); }; find.Controls.Add(search);
        var providers = new Label { Text = "Source: Independent Development build branches", AutoSize = true, Margin = new Padding(12,8,0,0), AccessibleName = "Module download source" };
        find.Controls.Add(providers); layout.Controls.Add(find, 0, 2);
        tools.Dock = DockStyle.Fill;
        MakeButton("Select visible", delegate { foreach (DataGridViewRow row in grid.Rows) if (row.Visible) row.Cells[0].Value = true; UpdateButtons(); });
        MakeButton("Clear selection", delegate { foreach (DataGridViewRow row in grid.Rows) row.Cells[0].Value = false; UpdateButtons(); });
        MakeButton("Select install candidates", delegate { SelectEligible("canRequestInstall"); });
        MakeButton("Select update candidates", delegate { SelectEligible("canRequestUpdate"); });
        MakeButton("Select removable", delegate { SelectEligible("canUninstall"); });
        refresh = MakeButton("Check GitHub", delegate { Send("refresh", null); });
        install = MakeButton("Install selected", delegate { RunOperation("install"); });
        update = MakeButton("Update selected", delegate { RunOperation("update"); });
        remove = MakeButton("Remove selected", delegate { RunOperation("uninstall"); });
        MakeButton("Destination...", delegate { ChooseDestination(); }); layout.Controls.Add(tools, 0, 3);
        grid.Dock = DockStyle.Fill; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false; grid.MultiSelect = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.FixedSingle; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.AccessibleName = "Registered mods and per-module source status";
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Select", Width = 50, FillWeight = 35 });
        foreach (string column in new [] { "Module", "Status", "Installed version", "Published version" }) grid.Columns.Add(column, column);
        for (int i=1;i<grid.Columns.Count;i++) grid.Columns[i].ReadOnly = true;
        grid.Columns[1].FillWeight = 190; grid.Columns[2].FillWeight = 115;
        grid.CurrentCellDirtyStateChanged += delegate { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged += delegate { UpdateButtons(); }; grid.SelectionChanged += delegate { ShowDetails(); };
        layout.Controls.Add(grid, 0, 4); summary.Dock = DockStyle.Fill; layout.Controls.Add(summary, 0, 5);
        detail.Multiline = true; detail.ReadOnly = true; detail.ScrollBars = ScrollBars.Vertical; detail.Dock = DockStyle.Fill; detail.AccessibleName = "Source details and operation results"; layout.Controls.Add(detail, 0, 6);
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
        progress.Dock = DockStyle.Fill; progress.Text = "No game folders have been accessed."; activity.Dock = DockStyle.Fill; activity.Style = ProgressBarStyle.Marquee; activity.Visible = false; bottom.Controls.Add(progress,0,0); bottom.Controls.Add(activity,1,0); layout.Controls.Add(bottom,0,7);
        Shown += delegate { StartBackend(); }; FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (busy) { MessageBox.Show(this, T("Wait for the current bounded operation to finish before closing. No automatic retry is scheduled."), T("Operation in progress")); e.Cancel = true; return; }
            if (backend != null && !backend.HasExited) { backend.StandardInput.Close(); }
        };
        automatic.Interval = 60000; automatic.Tick += delegate {
            var now=DateTime.UtcNow;ExpireCachedRows(now);UpdateButtons();
            if (refreshClock.TakeDue(now,firstCheckSent,busy)) Send("refresh",null);
        }; automatic.Start();
        CaptureText(this);ApplyLanguage();ApplyTheme();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged+=SystemPreferencesChanged;
        UpdateButtons();
    }
    void CaptureText(Control root) { foreach(Control control in root.Controls){if((control is Label||control is Button)&&control!=target&&control!=summary&&control!=progress&&control!=recoveryWarning)staticText[control]=control.Text;CaptureText(control);} }
    void RenderRecoveryNotice(){recoveryWarning.Text=recoveryNotice.Active?T("Recovery / lock warning: {0}",T(recoveryNotice.Key)):"";recoveryWarning.AccessibleName=recoveryWarning.Text;mainLayout.RowStyles[8].Height=recoveryNotice.Active?76:0;}
    void ApplyLanguage() {
        applyingPreferences=true;
        foreach(var entry in staticText){entry.Key.Text=T(entry.Value);entry.Key.AccessibleName=T(entry.Value);}
        int themeIndex=Array.IndexOf(themeIds,selectedTheme);themeChoice.Items.Clear();themeChoice.Items.AddRange(new object[]{T("Follow Windows"),T("Light"),T("Dark")});themeChoice.SelectedIndex=Math.Max(0,themeIndex);
        languageChoice.SelectedIndex=Math.Max(0,Array.IndexOf(languageIds,Presentation.Language));languageChoice.AccessibleName=T("Language");themeChoice.AccessibleName=T("Theme");
        string[] headers={"Select","Module","Status","Installed version","Published version"};for(int i=0;i<headers.Length;i++)grid.Columns[i].HeaderText=T(headers[i]);
        grid.AccessibleName=T("Registered mods and per-module source status");detail.AccessibleName=T("Source details and operation results");search.AccessibleName=T("Find a module");target.AccessibleName=T("Active installation destination");
        foreach(DataGridViewRow row in grid.Rows){var item=row.Tag as IDictionary<string,object>;if(item!=null){row.Cells[1].Value=DisplayName(item);row.Cells[2].Value=PlayerStatus(item);}}
        applyingPreferences=false;UpdateButtons();ShowDetails();RenderRecoveryNotice();
    }
    bool WindowsDark() {
        try { using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){return key!=null&&Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;} }catch{return false;}
    }
    void PaintControls(Control control,Color[] colors){
        control.BackColor=control is TextBox||control is ComboBox?colors[2]:colors[0];control.ForeColor=colors[1];
        var button=control as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.BackColor=colors[2];button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=colors[5];}
        foreach(Control child in control.Controls)PaintControls(child,colors);
    }
    void ApplyTheme(){
        var colors=Presentation.Palette(selectedTheme,WindowsDark(),SystemInformation.HighContrast);PaintControls(this,colors);
        grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=colors[0];grid.GridColor=colors[5];grid.DefaultCellStyle.BackColor=colors[2];grid.DefaultCellStyle.ForeColor=colors[1];
        grid.DefaultCellStyle.SelectionBackColor=colors[3];grid.DefaultCellStyle.SelectionForeColor=colors[4];grid.ColumnHeadersDefaultCellStyle.BackColor=colors[2];grid.ColumnHeadersDefaultCellStyle.ForeColor=colors[1];grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=colors[3];grid.ColumnHeadersDefaultCellStyle.SelectionForeColor=colors[4];
        foreach(Form dialog in OwnedForms)PaintControls(dialog,colors);
        Invalidate(true);
    }
    void SystemPreferencesChanged(object sender,Microsoft.Win32.UserPreferenceChangedEventArgs args){try{if(!IsDisposed&&IsHandleCreated)BeginInvoke((Action)(()=>{if(!IsDisposed)ApplyTheme();}));}catch(InvalidOperationException){}}
    protected override void Dispose(bool disposing){if(disposing){Microsoft.Win32.SystemEvents.UserPreferenceChanged-=SystemPreferencesChanged;automatic.Dispose();}base.Dispose(disposing);}
    void SavePreferences(string changed){
        if(applyingPreferences||busy||languageChoice.SelectedIndex<0||themeChoice.SelectedIndex<0||backend==null||backend.HasExited)return;
        string language=languageIds[languageChoice.SelectedIndex],theme=themeIds[themeChoice.SelectedIndex];Presentation.Language=language;selectedTheme=theme;ApplyLanguage();ApplyTheme();
        Send("preferences",new Dictionary<string,object>{{"preferences",new Dictionary<string,object>{{changed,changed=="language"?language:theme}}}});
    }
    string DisplayName(IDictionary<string,object> item){return TextValue(item,"name")+(Flag(item,"engineeringPaused")?T(" (development paused)"):Flag(item,"configurationPending")?T(" (not configured)"):"");}
    void StartBackend() {
        try {
            string root = AppDomain.CurrentDomain.BaseDirectory; string runtime = Path.Combine(root, "runtime", "node.exe");
            if (!File.Exists(Path.Combine(root,"desktop-backend.mjs"))) { runtime = Path.Combine(root,"portable","runtime","node.exe"); root = Directory.GetParent(root.TrimEnd(Path.DirectorySeparatorChar)).FullName; }
            if (!File.Exists(runtime)) throw new Exception(T("This is not a complete portable build. Use the packaged folder with its runtime subfolder."));
            var info = new ProcessStartInfo(runtime, "\"" + Path.Combine(root, "desktop-backend.mjs") + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = root, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
            info.EnvironmentVariables.Remove("NODE_OPTIONS"); info.EnvironmentVariables.Remove("NODE_PATH");
            info.EnvironmentVariables["DEV_MANAGER_NATIVE_HELPER"] = Application.ExecutablePath;
            backend = new Process { StartInfo = info, EnableRaisingEvents = true };
            backend.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null && !IsDisposed) BeginInvoke((Action)(() => Receive(e.Data))); };
            backend.ErrorDataReceived += delegate { }; backend.Exited += delegate { if (!IsDisposed) BeginInvoke((Action)(() => { SetBusy(false); progress.Text = T("Backend stopped. Restart the application before making changes."); tools.Enabled = false;languageChoice.Enabled=false;themeChoice.Enabled=false; })); };
            backend.Start(); backend.BeginOutputReadLine(); backend.BeginErrorReadLine(); Send("status", null);
        } catch (Exception error) { progress.Text = T("Application could not start."); detail.Text = T("Technical details: {0}",error.Message); tools.Enabled = false;languageChoice.Enabled=false;themeChoice.Enabled=false; }
    }
    List<IDictionary<string, object>> Selected() {
        return grid.Rows.Cast<DataGridViewRow>().Where(row => Convert.ToBoolean(row.Cells[0].Value ?? false)).Select(row => row.Tag as IDictionary<string, object>).Where(item => item != null).ToList();
    }
    int HiddenSelectedCount() {
        return SelectionSafety.HiddenCount(grid.Rows.Cast<DataGridViewRow>().Select(row =>
            new KeyValuePair<bool,bool>(Convert.ToBoolean(row.Cells[0].Value ?? false),row.Visible)));
    }
    void SetBusy(bool value) { busy = value; activity.Visible = value; tools.Enabled = !value; search.Enabled = !value; grid.Enabled = !value;languageChoice.Enabled=!value;themeChoice.Enabled=!value; UpdateButtons(); }
    void UpdateButtons() {
        if (install == null || update == null || remove == null) return;
        var items = Selected(); var rows = grid.Rows.Cast<DataGridViewRow>().Select(row => row.Tag as IDictionary<string,object>).Where(row => row != null).ToList();
        int hidden = HiddenSelectedCount();
        summary.Text = hidden > 0 ? T("{0} selected, {1} hidden. Clear search to review or clear selection.",items.Count,hidden) :
            T("{0} selected. {1} not configured; {2} development paused. Only owned files can be changed.",items.Count,rows.Count(row => Flag(row,"configurationPending")),rows.Count(row => Flag(row,"engineeringPaused")));
        bool canAct = !busy && hidden == 0 && items.Count > 0;
        install.Enabled = canAct && items.All(item => Flag(item,"canRequestInstall") || Flag(item,"canInstall"));
        update.Enabled = canAct && items.All(item => Flag(item,"canRequestUpdate") || Flag(item,"canUpdate"));
        remove.Enabled = canAct && items.All(item => Flag(item,"canUninstall")); refresh.Enabled = !busy;
    }
    void ApplyFilter() { foreach (DataGridViewRow row in grid.Rows) row.Visible = Convert.ToString(row.Cells[1].Value).IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0; UpdateButtons(); }
    void ExpireCachedRows(DateTime now) {
        double nowMs=(now-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds;
        bool selectedChanged=false;
        foreach(DataGridViewRow row in grid.Rows){var item=row.Tag as IDictionary<string,object>;
            if(item!=null&&SourceFreshness.Expire(item,nowMs,T("Observation expired. Check GitHub again before installing."))){
                row.Cells[2].Value=PlayerStatus(item);if(Object.ReferenceEquals(row,grid.CurrentRow))selectedChanged=true;
            }
        }
        if(detailView.ShouldRefreshSelection(selectedChanged))ShowDetails();
    }
    void SelectEligible(string flag) { foreach (DataGridViewRow row in grid.Rows) { var item = row.Tag as IDictionary<string,object>;
        bool selected = Convert.ToBoolean(row.Cells[0].Value ?? false); bool next = SelectionSafety.EligibleSelection(selected,row.Visible,item != null && Flag(item,flag));
        if (selected != next) row.Cells[0].Value = next; } UpdateButtons(); }
    string PlayerStatus(IDictionary<string,object> item) {
        string raw = TextValue(item,"status");
        if(raw=="Development paused")return T("Development paused");
        if(raw=="stale / unknown")return T("stale / unknown");
        if (raw.StartsWith("queued")) return T("Verification queued");
        if (raw == "Local files require inspection") return T("Files need attention");
        if (Flag(item,"canRequestUpdate")) return T("Update available (verify before install)");
        if (Flag(item,"canRequestInstall")) return T("Not installed (candidate requires checks)");
        if (raw == "current") return T("Installed (up to date)");
        // Signature: OpenAI-Codex. A matching candidate is not fully verified yet.
        if (Flag(item,"matchesCandidate")) return T("Installed (source matches; verification pending)");
        // Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
        if (TextValue(item,"latestBuild").Length > 0) return T(TextValue(item,"installedBuild").Length > 0 ?
            "Installed (candidate found; update not confirmed)" : "Candidate found (select a destination to check installation)");
        return T(TextValue(item,"installedBuild").Length > 0 ? "Installed (source unavailable)" : "Source unavailable");
    }
    void ShowDetails() {
        if (grid.CurrentRow == null) return; var item = grid.CurrentRow.Tag as IDictionary<string, object>; if (item == null) return;
        detailView.ShowSelection();
        string checkedText = TextValue(item,"checkedAtMs");
        if (checkedText.Length > 0) checkedText = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(Convert.ToDouble(checkedText)).ToLocalTime().ToString("g");
        detail.Text = TextValue(item,"name") + Environment.NewLine + TextValue(item,"configurationText") + Environment.NewLine + TextValue(item,"reasonText") + Environment.NewLine + T("Last check: {0}",checkedText.Length > 0 ? checkedText : T("Not checked yet")) + Environment.NewLine + T("Development evidence: {0}",TextValue(item,"developmentText")) + Environment.NewLine + T("Branch: {0}",TextValue(item,"branch")) + Environment.NewLine + T("Source SHA: {0}",TextValue(item,"sourceSha"));
        if(TextValue(item,"reasonDiagnostic").Length>0)detail.Text+=Environment.NewLine+T("Technical details: {0}",TextValue(item,"reasonDiagnostic"));
    }
    void Send(string command, IDictionary<string, object> extra) {
        if (backend == null || backend.HasExited) return;
        var request = new Dictionary<string, object> { { "id", ++nextId }, { "command", command } };
        if (extra != null) foreach (var item in extra) request[item.Key] = item.Value;
        SetBusy(true); progress.Text = T(command=="preferences"?"Saving preferences...":command == "refresh" ? "Checking GitHub. Missing or unverifiable sources remain unknown." : "Working...");
        backend.StandardInput.WriteLine(Program.Json.Serialize(request)); backend.StandardInput.Flush();
    }
    void RunOperation(string operation) {
        if(operation!="uninstall"){ExpireCachedRows(DateTime.UtcNow);UpdateButtons();}
        if(HiddenSelectedCount()>0){detail.Text=T("Clear search to review all selected modules, or clear selection.");return;}
        var items = Selected(); if (items.Count == 0) return;
        if((operation=="install"&&items.Any(item=>!Flag(item,"canRequestInstall")&&!Flag(item,"canInstall")))||
           (operation=="update"&&items.Any(item=>!Flag(item,"canRequestUpdate")&&!Flag(item,"canUpdate")))){detail.Text=T("Observation expired. Check GitHub again before installing.");return;}
        Send(operation, new Dictionary<string, object> { { "modules", items.Select(item => TextValue(item,"moduleId")).ToArray() } });
    }
    void ChooseDestination() {
        using (var dialog = new Form { Text = T("Choose installation destination"), Width = 820, Height = 380, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, Font = Font,Owner=this }) {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 5, ColumnCount = 1 };
            panel.Controls.Add(new Label { Text = T("Select the game's existing Mods folder, or return to the isolated test workspace."), AutoSize = true, MaximumSize = new Size(750,0) });
            var pathBox = new TextBox { Dock = DockStyle.Fill, Text = mode == "game-mods" ? targetPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Colossal Order", "Cities_Skylines", "Addons", "Mods"), AccessibleName = T("Game Mods folder") }; panel.Controls.Add(pathBox);
            panel.Controls.Add(new Label { Text = T("Each module uses its own direct folder. Metadata and backups stay outside Mods. Unknown or changed files are preserved. Game loading is unverified."), AutoSize = true, MaximumSize = new Size(750,0) });
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            var game = new Button { Text = T("Use this Mods folder"), AutoSize = true, Height = 42 }; var sandbox = new Button { Text = T("Use test workspace"), AutoSize = true, Height = 42 }; var cancel = new Button { Text = T("Cancel"), AutoSize = true, Height = 42 };
            game.Click += delegate { dialog.Close(); Send("destination", new Dictionary<string, object> { { "mode", "game-mods" }, { "path", pathBox.Text }, { "confirmed", true } }); };
            sandbox.Click += delegate { dialog.Close(); Send("destination", new Dictionary<string, object> { { "mode", "sandbox" }, { "confirmed", true } }); }; cancel.Click += delegate { dialog.Close(); };
            buttons.Controls.Add(game); buttons.Controls.Add(sandbox); buttons.Controls.Add(cancel); panel.Controls.Add(buttons); dialog.Controls.Add(panel);PaintControls(dialog,Presentation.Palette(selectedTheme,WindowsDark(),SystemInformation.HighContrast));dialog.ShowDialog(this);
        }
    }
    void Receive(string line) {
        try {
            var message = Program.Json.DeserializeObject(line) as IDictionary<string, object>; if (message == null) return;
            recoveryNotice.Observe(message);RenderRecoveryNotice();
            if (TextValue(message,"event") == "progress") { progress.Text = TextValue(message,"textLocalized"); return; }
            object resultValue;
            if (message.TryGetValue("result", out resultValue) && resultValue is IDictionary<string, object>) {
                var result = (IDictionary<string, object>)resultValue; object value;
                if(result.TryGetValue("preferences",out value)){var prefs=(IDictionary<string,object>)value;Presentation.Language=TextValue(prefs,"language");selectedTheme=TextValue(prefs,"theme");ApplyLanguage();ApplyTheme();}
                if (result.TryGetValue("target", out value)) { var t = (IDictionary<string, object>)value; mode = TextValue(t,"mode"); targetPath = TextValue(t,"path"); target.Text = T(mode == "sandbox" ? "Test workspace: {0}" : mode == "game-mods" ? "Game Mods folder: {0}" : "Destination not selected: {0}",targetPath) + Environment.NewLine + TextValue(t,"scopeText"); }
                if (result.TryGetValue("rows", out value)) {
                    var selected = new HashSet<string>(Selected().Select(item => TextValue(item,"moduleId"))); grid.Rows.Clear();
                    foreach (object raw in (IEnumerable)value) { var row = (IDictionary<string, object>)raw; string commit = TextValue(row,"sourceSha");
                        int index = grid.Rows.Add(selected.Contains(TextValue(row,"moduleId")), DisplayName(row), PlayerStatus(row), TextValue(row,"installedBuild"), TextValue(row,"latestBuild")); grid.Rows[index].Tag = row; }
                    ExpireCachedRows(DateTime.UtcNow);ApplyFilter(); ShowDetails();
                }
                if (result.TryGetValue("results", out value)) { detail.Text = String.Join(Environment.NewLine, ((IEnumerable)value).Cast<IDictionary<string, object>>().Select(item => TextValue(item,"moduleId") + ": " + TextValue(item,"statusText") + " " + TextValue(item,"reasonText")+(TextValue(item,"reasonDiagnostic").Length>0?Environment.NewLine+T("Technical details: {0}",TextValue(item,"reasonDiagnostic")):""))); detailView.ShowResults(); }
                if (result.TryGetValue("rate", out value)) { var rate = (IDictionary<string, object>)value; object retry;
                    if (rate.TryGetValue("retryAtMs",out retry) && Convert.ToDouble(retry) > (DateTime.UtcNow - new DateTime(1970,1,1)).TotalMilliseconds) { var retryTime = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(Convert.ToDouble(retry)); refreshClock.DeferUntil(retryTime); detail.Text += Environment.NewLine + T("GitHub request allowance is unavailable. Retry after {0}. No immediate retry is scheduled.",retryTime.ToLocalTime().ToString("g")); } }
                if(TextValue(result,"settingsWarningText").Length>0)detail.Text+=Environment.NewLine+TextValue(result,"settingsWarningText");
            }
            SetBusy(false);
            progress.Text = Flag(message,"ok") ? T("Ready. Source state is based on the last check; game loading is unverified.") : T("Not completed: {0}",TextValue(message,"errorText"));
            if (!Flag(message,"ok")) {detail.Text = TextValue(message,"errorText") + Environment.NewLine + T("No success is assumed. Inspect the selected source or destination before retrying.")+Environment.NewLine+T("Technical details: {0}",TextValue(message,"error"));detailView.ShowResults();}
            if(catalogUnavailable)detail.Text+=Environment.NewLine+T("Technical details: {0}","TRANSLATION_RESOURCE_UNAVAILABLE; English fallback");
            if (Flag(message,"ok") && !firstCheckSent) { firstCheckSent = true; refreshClock.Reset(DateTime.UtcNow); Send("refresh",null); }
        } catch { SetBusy(false); progress.Text = T("Invalid backend response. Restart before changing files."); tools.Enabled = false;languageChoice.Enabled=false;themeChoice.Enabled=false; }
    }
}
