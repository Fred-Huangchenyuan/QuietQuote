using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace QuietQuote {
public sealed class Settings {
    public int Interval = 3;
    public int Alpha = 85;
    public int FontSize = 15;
    public bool Symbol = false;
    public bool Top = true;
    public string InstrumentId = Quote.Instrument;
    public string Exchange = "OKX", Kind = "SWAP";
    public List<WindowSpec> Windows;
    public Settings Copy() { return new JavaScriptSerializer().Deserialize<Settings>(new JavaScriptSerializer().Serialize(this)); }
    public void EnsureWindows() {
        Normalize();
        if (Windows == null || Windows.Count == 0) Windows = new List<WindowSpec> { new WindowSpec { Exchange=Exchange, Kind=Kind, InstrumentId=InstrumentId, X=X, Y=Y } };
        Windows = Windows.Take(8).Select(x => x ?? new WindowSpec()).ToList();
        foreach (var window in Windows) window.Normalize();
    }
    public Settings ForWindow(WindowSpec w) {
        var s = (Settings)MemberwiseClone(); s.Windows=null; s.Exchange=w.Exchange; s.Kind=w.Kind; s.InstrumentId=w.InstrumentId; s.X=w.X; s.Y=w.Y; return s;
    }
    public int Diameter = 88;
    public bool Glass = true;
    public string Language = "zh";
    public string Shape = "capsule";
    public string Theme = "auto";
    public int CapsuleWidth = 156, CapsuleHeight = 52;
    public int X = -32000, Y = -32000;
    public void Normalize() {
        Interval = Math.Max(1, Math.Min(3600, Interval)); Alpha = Math.Max(30, Math.Min(100, Alpha)); FontSize = Math.Max(10, Math.Min(28, FontSize)); Diameter = Math.Max(64, Math.Min(180, Diameter));
        CapsuleHeight = Math.Max(36, Math.Min(100, CapsuleHeight)); CapsuleWidth = Math.Max(CapsuleHeight + 40, Math.Max(100, Math.Min(360, CapsuleWidth)));
        if (Language != "en") Language = "zh"; if (Shape != "circle") Shape = "capsule"; if (Theme != "light" && Theme != "dark") Theme = "auto";
        var source = new WindowSpec { Exchange=Exchange, Kind=Kind, InstrumentId=InstrumentId }; source.Normalize();
        Exchange=source.Exchange; Kind=source.Kind; InstrumentId=source.InstrumentId;
    }
    public static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"); } }
    public static Settings Load() { try { var s = new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(FilePath)); s.Normalize(); return s; } catch { return new Settings(); } }
    public void Save() {
        string tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(this));
        if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null); else File.Move(tmp, FilePath);
    }
}
public sealed class QuoteWindow : Form {
    Settings settings;
    readonly Feed feed;
    readonly DesktopSession session;
    readonly bool primary;
    internal Settings CurrentSettings { get { return settings; } }
    readonly CancellationTokenSource cancel = new CancellationTokenSource();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 500 };
    readonly NotifyIcon tray;
    readonly ToolTip tip = new ToolTip { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 15000 };
    readonly ContextMenuStrip menu = new ContextMenuStrip();
    Quote quote;
    string failure = "";
    string lastTip = "";
    bool busy, closing, hotkey;
    int errors;
    int generation;
    DateTime next = DateTime.MinValue, fetched = DateTime.MinValue, started = DateTime.MinValue;
    Point dragStart, windowStart;
    bool dragging;
    Font ownedFont;
    readonly bool preview;
    bool glassActive;
    string lastRender = "";
    bool lightTheme;
    DateTime nextTheme = DateTime.MinValue;
    string T(string zh, string en) { return settings.Language == "en" ? en : zh; }
    void TranslateMenu() {
        menu.Items[0].Text = T("显示 / 隐藏    Ctrl+Alt+Q", "Show / hide    Ctrl+Alt+Q");
        menu.Items[1].Text = T("设置…", "Settings…"); menu.Items[2].Text = T("立即刷新", "Refresh now");
        menu.Items[3].Text = T("移回主屏幕", "Move to primary screen"); menu.Items[5].Text = T("退出", "Exit");
    }
    protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x80000; return p; } }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if (settings != null) { glassActive = GlassComposition.Enable(Handle, settings.Glass); lastRender = ""; } }
    public QuoteWindow(bool previewMode) : this(previewMode, null, null, true, null) { }
    internal QuoteWindow(bool previewMode, Settings initial, DesktopSession owner, bool isPrimary, Feed source) {
        preview = previewMode; session=owner; primary=isPrimary; feed=source ?? new Feed();
        settings = initial ?? (preview ? new Settings() : Settings.Load()); settings.Normalize();
        Text = "QuietQuote"; ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual; BackColor = Color.FromArgb(241, 243, 246);
        DoubleBuffered = true; AutoScaleMode = AutoScaleMode.None;
        Icon = MakeIcon();
        tray = new NotifyIcon { Icon = Icon, Text = "QuietQuote · KORU", Visible = !preview && primary };
        menu.Items.Add("显示 / 隐藏    Ctrl+Alt+Q", null, delegate { Toggle(); });
        menu.Items.Add("设置…", null, delegate { OpenOptions(); });
        menu.Items.Add("立即刷新", null, async delegate { if ((DateTime.UtcNow - started).TotalSeconds >= 1) await RefreshQuote(); });
        menu.Items.Add("移回主屏幕", null, delegate { if (session != null) session.Arrange(); else { PlaceDefault(); Show(); Save(); } });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, delegate { if (session != null) session.Quit(); else Close(); });
        ContextMenuStrip = menu; tray.ContextMenuStrip = menu;
        tray.DoubleClick += delegate { Toggle(); };
        Apply();
        var saved = new Rectangle(settings.X, settings.Y, Width, Height);
        bool visible = false;
        foreach (var screen in Screen.AllScreens) if (screen.WorkingArea.Contains(saved)) visible = true;
        if (visible) Location = saved.Location; else PlaceDefault();
        MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragStart = Cursor.Position; windowStart = Location; dragging = true; Capture = true; } };
        MouseMove += delegate { if (dragging) { Point p = Cursor.Position; Location = new Point(windowStart.X + p.X - dragStart.X, windowStart.Y + p.Y - dragStart.Y); } };
        MouseUp += delegate { if (dragging) { dragging = false; Capture = false; Save(); } };
        MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) OpenOptions(); };
        timer.Tick += async delegate { UpdateTheme(); UpdateStatus(); if (!busy && DateTime.UtcNow >= next) await RefreshQuote(); };
        Shown += async delegate {
            if (preview) return;
            hotkey = primary && RegisterHotKey(Handle, 1, 0x4003, (uint)Keys.Q);
            if (primary && !hotkey) tray.ShowBalloonTip(5000, "QuietQuote", T("Ctrl+Alt+Q 已被占用。可双击托盘图标显示或隐藏。", "Ctrl+Alt+Q is in use. Double-click the tray icon to show or hide."), ToolTipIcon.Info);
            timer.Start(); await RefreshQuote();
        };
    }
    public static Icon MakeIcon() {
        using (var b = new Bitmap(32, 32)) using (var g = Graphics.FromImage(b)) {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
            using (var brush = new SolidBrush(Color.FromArgb(102, 116, 134))) g.FillEllipse(brush, 3, 3, 26, 26);
            using (var pen = new Pen(Color.White, 2)) g.DrawLines(pen, new Point[] { new Point(9, 20), new Point(14, 13), new Point(18, 17), new Point(24, 10) });
            IntPtr h = b.GetHicon(); try { using (var i = Icon.FromHandle(h)) return (Icon)i.Clone(); } finally { DestroyIcon(h); }
        }
    }
    void PlaceDefault() { var r = Screen.PrimaryScreen.WorkingArea; Location = new Point(r.Right - Width - 22, r.Bottom - Height - 22); }
    void Save() {
        if (preview) return;
        settings.X = Left; settings.Y = Top;
        if (session != null) { session.Save(); return; }
        try { settings.Save(); } catch { tray.ShowBalloonTip(3000, "QuietQuote", T("设置未能保存，请将程序放在可写入的文件夹。", "Could not save settings. Use a writable folder."), ToolTipIcon.Info); }
    }
    void Apply() {
        var old = ownedFont; ownedFont = new Font("Segoe UI", settings.FontSize, FontStyle.Regular); Font = ownedFont; if (old != null) old.Dispose();
        TopMost = settings.Top;
        if (IsHandleCreated) glassActive = GlassComposition.Enable(Handle, settings.Glass);
        TranslateMenu(); nextTheme = DateTime.MinValue; UpdateTheme(); lastRender = ""; UpdateStatus();
    }
    void UpdateTheme() {
        if (settings.Glass) { lightTheme=false;return; }
        if (settings.Theme != "auto") { lightTheme=settings.Theme=="light";return; }
        if (Visible && !preview && DateTime.UtcNow>=nextTheme) { nextTheme=DateTime.UtcNow.AddSeconds(1);lightTheme=Appearance.NearbyLight(Bounds,lightTheme); }
    }
    void Toggle() { if (session != null) { session.Toggle(); return; } if (Visible) Hide(); else { Show(); UpdateStatus(); } }
    void OpenOptions() {
        if (session != null) { session.OpenOptions(this); return; }
        using (var dialog = new Options(settings)) {
            dialog.TopMost = settings.Top;
            if (dialog.ShowDialog() == DialogResult.OK) { UseSettings(dialog.Result); Save(); }
        }
    }
    internal void UseSettings(Settings value) {
        value.Normalize();
        if (value.InstrumentId != settings.InstrumentId || value.Exchange != settings.Exchange || value.Kind != settings.Kind) { generation++; quote = null; fetched = DateTime.MinValue; failure = ""; errors = 0; }
        settings = value; Apply(); next = DateTime.UtcNow;
    }
    public async Task RefreshQuote() {
        if (busy || closing) return;
        busy = true; started = DateTime.UtcNow;
        int requestGeneration = generation;
        string instrument = settings.InstrumentId;
        try {
            var q = await feed.Fetch(settings.Exchange, settings.Kind, instrument, cancel.Token);
            if (closing || requestGeneration != generation) return;
            if (quote != null && q.TimeUtc < quote.TimeUtc) throw new InvalidDataException("收到较旧行情，等待更新");
            quote = q; fetched = DateTime.UtcNow; failure = ""; errors = 0;
        } catch (Exception ex) {
            if (closing || requestGeneration != generation) return;
            errors++;
            failure = ex is InvalidDataException ? "data" : ex is TaskCanceledException ? "timeout" : "network";
        } finally {
            busy = false;
            int delay = errors == 0 ? settings.Interval : Math.Max(settings.Interval, Math.Min(60, (int)Math.Pow(2, Math.Min(errors, 6))));
            next = requestGeneration == generation ? DateTime.UtcNow.AddSeconds(delay) : DateTime.UtcNow;
            if (!closing) UpdateStatus();
        }
    }
    string State() {
        if (failure != "") return T("断线", "Offline");
        if (quote == null) return T("连接中", "Connecting");
        if (quote.IsOld(DateTime.UtcNow, settings.Interval) || (DateTime.UtcNow - fetched).TotalSeconds > Math.Max(30, settings.Interval + 15)) return T("过期", "Stale");
        return "";
    }
    string Caption() {
        return quote == null ? "—" : quote.Display;
    }
    void UpdateStatus() {
        int w=settings.Shape=="circle"?settings.Diameter:settings.CapsuleWidth, h=settings.Shape=="circle"?settings.Diameter:settings.CapsuleHeight;
        if (Width != w || Height != h || Region == null) {
            ClientSize = new Size(w, h);
            using (var p = Appearance.Outline(new RectangleF(0,0,Width-1,Height-1),settings.Shape=="circle")) { var old = Region; Region = new Region(p); if (old != null) old.Dispose(); }
        }
        var market = Market.FromId(settings.InstrumentId, settings.Kind);
        string info = settings.Exchange + " · " + (settings.Kind == "SPOT" ? T("现货", "Spot") : T("永续", "Perpetual")) + " · " + market.Id + "\n" + T("最新成交价", "Last trade") + " (" + market.Unit + ") · " + settings.Interval + T(" 秒刷新\n", "s refresh\n");
        if (quote != null) info += (quote.ReceiptTime ? T("接收时间（接口无成交时间）：", "Received (no trade timestamp): ") : T("行情时间：", "Quote time: ")) + quote.TimeUtc.ToLocalTime().ToString("MM-dd HH:mm:ss") + "\n";
        if (failure!="") info+=T("行情暂不可用，自动重试中。\n","Market data unavailable. Retrying automatically.\n");
        else if(State()!="" && quote!=null)info+=T("行情过期或系统时钟偏差，请核对。\n","Stale data or clock discrepancy. Please check.\n");
        info += T("拖动移动 · 右键设置 · Ctrl+Alt+Q 隐藏", "Drag to move · Right-click settings · Ctrl+Alt+Q to hide");
        if (settings.Glass && !glassActive) info += T("\n当前使用半透明兼容外观", "\nUsing translucent compatibility appearance");
        if (info != lastTip) { tip.SetToolTip(this, info); lastTip = info; }
        string trayText = settings.Exchange + " " + market.Coin + " / " + market.Unit + " " + Caption() + " " + State();
        tray.Text = trayText.Substring(0, Math.Min(63, trayText.Length));
        string render = Caption() + State() + settings.Symbol + settings.Exchange + settings.Kind + settings.InstrumentId + Width + Height + settings.FontSize + settings.Alpha + glassActive + lightTheme + settings.Language + settings.Shape;
        if (render != lastRender && IsHandleCreated) { RenderGlass(); lastRender = render; }
    }
    void RenderGlass() { using (var bitmap = RenderSurface()) GlassComposition.Present(Handle, bitmap, Location); }
    internal Bitmap RenderSurface() {
        var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        bool light = !settings.Glass && lightTheme, circle=settings.Shape=="circle";
        using (var g = Graphics.FromImage(bitmap)) {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            int tint=settings.Glass?(glassActive?22:95)+settings.Alpha:Math.Min(250,110+settings.Alpha);
            Color top=light?Color.FromArgb(tint,251,253,253):Color.FromArgb(tint,54,67,86);
            Color bottom=light?Color.FromArgb(Math.Min(250,tint+25),227,237,237):Color.FromArgb(Math.Min(235,tint+35),13,23,39);
            using(var path=Appearance.Outline(new RectangleF(0,0,Width-1,Height-1),circle))
            using(var brush=new LinearGradientBrush(ClientRectangle,top,bottom,75f))g.FillPath(brush,path);
            using(var path=Appearance.Outline(new RectangleF(1.5f,1.5f,Width-4,Height-4),circle)) {
                using(var pen=new Pen(Color.FromArgb(light?110:40,255,255,255),3))g.DrawPath(pen,path);
                using(var brush=new LinearGradientBrush(ClientRectangle,Color.FromArgb(185,255,255,255),light?Color.FromArgb(80,80,110,120):Color.FromArgb(35,214,235,255),65f))
                using(var pen=new Pen(brush,1))g.DrawPath(pen,path);
            }
            int small=circle?Math.Max(12,Height/5):11;
            int spaceTop=settings.Symbol?small+5:5, spaceBottom=State()!=""?small+5:5;
            float numberHeight=Math.Min(Font.Size*1.6f,Height-2*Math.Max(spaceTop,spaceBottom));
            numberHeight=Math.Max(12,numberHeight);
            var rect=new RectangleF(circle?10:Height*.28f,(Height-numberHeight)/2,Width-(circle?20:Height*.56f),numberHeight);
            Color digit=light?Color.FromArgb(27,67,68):Color.FromArgb(210,250,240);
            SegmentDigits.Draw(g,Caption(),rect,numberHeight,digit);
            if(settings.Symbol) {
                var m=Market.FromId(settings.InstrumentId, settings.Kind);
                DrawCentered(g,m.Coin+" / "+m.Unit,Math.Min(8,Font.Size),new Rectangle(Width/7,circle?Height/6:4,Width*5/7,small),light?Color.FromArgb(74,102,103):Color.FromArgb(210,225,240));
            }
            if(State()!="")DrawCentered(g,State(),Math.Min(8,Font.Size),new Rectangle(Width/7,circle?Height*2/3:Height-small-4,Width*5/7,small),light?Color.FromArgb(137,79,28):Color.FromArgb(255,211,153));
        }
        return bitmap;
    }
    static void DrawCentered(Graphics graphics, string text, float size, Rectangle bounds, Color color) {
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
        for (float points = size; points >= 4; points -= .5f) using (var font = new Font("Segoe UI", points)) {
            var measured = graphics.MeasureString(text, font, Int32.MaxValue, format);
            if ((measured.Width <= bounds.Width && measured.Height <= bounds.Height) || points <= 4) {
                var shadow = bounds; shadow.Offset(0, 1);
                using (var b = new SolidBrush(Color.FromArgb(140, 3, 9, 20))) graphics.DrawString(text, font, b, shadow, format);
                using (var b = new SolidBrush(color)) graphics.DrawString(text, font, b, bounds, format); return;
            }
        }
    }
    protected override void OnPaint(PaintEventArgs e) { }
    protected override void OnPaintBackground(PaintEventArgs e) { }
    public void PreviewQuote(Quote q) { quote = q; fetched = DateTime.UtcNow; UpdateStatus(); }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1) Toggle();
        base.WndProc(ref m);
        if ((m.Msg == 0x031E || m.Msg == 0x001A) && settings != null && IsHandleCreated) { glassActive = GlassComposition.Enable(Handle, settings.Glass); lastRender = ""; UpdateStatus(); }
    }
    protected override void OnFormClosing(FormClosingEventArgs e) {
        if (session != null && !session.ManagingClose && e.CloseReason == CloseReason.UserClosing) {
            e.Cancel = true; BeginInvoke(new Action(session.Quit)); return;
        }
        closing = true; timer.Stop(); cancel.Cancel(); if (hotkey) UnregisterHotKey(Handle, 1); Save();
        tray.Visible = false; base.OnFormClosing(e);
    }
    protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); tray.Dispose(); tip.Dispose(); menu.Dispose(); feed.Dispose(); cancel.Dispose(); if (Icon != null) Icon.Dispose(); } base.Dispose(disposing); if (disposing && ownedFont != null) { ownedFont.Dispose(); ownedFont = null; } }
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
}
internal static class GlassComposition {
    [StructLayout(LayoutKind.Sequential)] struct Accent { public int State, Flags; public uint Color; public int Animation; }
    [StructLayout(LayoutKind.Sequential)] struct Attribute { public int Kind; public IntPtr Data; public int Size; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct Blend { public byte Operation, Flags, Alpha, Format; }
    [DllImport("user32.dll")] static extern int SetWindowCompositionAttribute(IntPtr window, ref Attribute attribute);
    [DllImport("user32.dll", SetLastError = true)] static extern bool UpdateLayeredWindow(IntPtr window, IntPtr dst, ref Point location, ref Size size, IntPtr src, ref Point origin, int key, ref Blend blend, int flags);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    public static bool Enable(IntPtr window, bool enabled) {
        var accent = new Accent { State = enabled ? 3 : 0, Flags = 2 };
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(accent));
        try { Marshal.StructureToPtr(accent, ptr, false); var data = new Attribute { Kind = 19, Data = ptr, Size = Marshal.SizeOf(accent) }; return SetWindowCompositionAttribute(window, ref data) != 0 && enabled; }
        catch (EntryPointNotFoundException) { return false; } catch (DllNotFoundException) { return false; }
        finally { Marshal.FreeHGlobal(ptr); }
    }
    public static void Present(IntPtr window, Bitmap bitmap, Point location) {
        IntPtr dc = CreateCompatibleDC(IntPtr.Zero), image = IntPtr.Zero, old = IntPtr.Zero;
        try {
            image = bitmap.GetHbitmap(Color.FromArgb(0)); old = SelectObject(dc, image);
            var size = bitmap.Size; var origin = Point.Empty; var blend = new Blend { Alpha = 255, Format = 1 };
            if (!UpdateLayeredWindow(window, IntPtr.Zero, ref location, ref size, dc, ref origin, 0, ref blend, 2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        } finally { if (old != IntPtr.Zero) SelectObject(dc, old); if (image != IntPtr.Zero) DeleteObject(image); if (dc != IntPtr.Zero) DeleteDC(dc); }
    }
}
static class Program {
    [STAThread] static void Main() {
        bool created;
        using (var mutex = new Mutex(true, "Local\\QuietQuote.KORU.Price.v1", out created)) {
            if (!created) { MessageBox.Show(Settings.Load().Language == "en" ? "QuietQuote is already running. Use Ctrl+Alt+Q or the tray icon to restore it." : "QuietQuote 已在运行。请使用 Ctrl+Alt+Q 或托盘图标恢复窗口。", "QuietQuote"); return; }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DesktopSession());
        }
    }
}
}
