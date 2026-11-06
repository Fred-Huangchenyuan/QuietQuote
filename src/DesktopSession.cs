using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuietQuote {
// Owns one tray/hotkey and all overlays. Only the session writes the shared settings file.
public sealed class DesktopSession : ApplicationContext {
    Settings settings;
    readonly List<QuoteWindow> windows = new List<QuoteWindow>();
    readonly bool preview;
    bool quitting, applying, editing;
    internal bool ManagingClose { get { return quitting || applying; } }
    internal IList<QuoteWindow> Windows { get { return windows.AsReadOnly(); } }
    public DesktopSession() : this(Settings.Load(), false) { }
    internal DesktopSession(Settings initial, bool previewMode) { preview=previewMode; Apply(initial); }
    internal void Apply(Settings value) {
        value.EnsureWindows(); settings=value; applying=true;
        try {
            while (windows.Count > settings.Windows.Count) {
                var last=windows[windows.Count-1]; windows.RemoveAt(windows.Count-1); last.Close(); last.Dispose();
            }
            for (int i=0; i<settings.Windows.Count; i++) {
                var spec=settings.Windows[i];
                if (i < windows.Count) windows[i].UseSettings(settings.ForWindow(spec));
                else {
                    var w=new QuoteWindow(preview, settings.ForWindow(spec), this, i==0, null); windows.Add(w);
                    if (spec.X == -32000) {
                        var r=Screen.PrimaryScreen.WorkingArea;
                        w.Location=DefaultPosition(r, w.Size, i);
                    }
                    w.Show();
                }
            }
        } finally { applying=false; }
        Save();
    }
    internal void Capture() {
        for (int i=0; i<windows.Count; i++) { settings.Windows[i].X=windows[i].Left; settings.Windows[i].Y=windows[i].Top; }
        // Keep legacy fields aligned with window 1 for older versions and easy migration.
        var first=settings.Windows[0]; settings.Exchange=first.Exchange; settings.Kind=first.Kind;
        settings.InstrumentId=first.InstrumentId; settings.X=first.X; settings.Y=first.Y;
    }
    internal void Save() {
        if (applying || preview || quitting) return;
        Capture();
        try { settings.Save(); }
        catch { if (!editing) { editing=true; MessageBox.Show(settings.Language=="en" ? "Could not save settings. Put QuietQuote in a writable folder." : "设置未能保存，请把 QuietQuote 放在可写入的文件夹。", "QuietQuote"); editing=false; } }
    }
    internal void OpenOptions(QuoteWindow sender) {
        if (editing || quitting) return; editing=true; Capture();
        try {
            using (var dialog=new Options(settings, Math.Max(0,windows.IndexOf(sender)))) {
                dialog.TopMost=settings.Top;
                if (dialog.ShowDialog()==DialogResult.OK) Apply(dialog.Result);
            }
        } finally { editing=false; }
    }
    internal void Toggle() {
        bool hide=windows.Any(w=>w.Visible);
        foreach (var w in windows) { if (hide) w.Hide(); else w.Show(); }
    }
    internal void Arrange() {
        var r=Screen.PrimaryScreen.WorkingArea;
        for(int i=0;i<windows.Count;i++) {
            var w=windows[i]; w.Location=DefaultPosition(r,w.Size,i); w.Show();
        }
        Save();
    }
    static Point DefaultPosition(Rectangle area, Size size, int index) {
        int rows=Math.Max(1,(area.Height-22)/(size.Height+12));
        return new Point(Math.Max(area.Left,area.Right-(index/rows+1)*(size.Width+12)-10),
            Math.Max(area.Top,area.Bottom-(index%rows+1)*(size.Height+12)-10));
    }
    internal void Quit() {
        if(quitting)return; Save(); quitting=true;
        foreach(var w in windows.ToArray()) { w.Close(); w.Dispose(); } windows.Clear(); ExitThread();
    }
    protected override void Dispose(bool disposing) { if(disposing && !quitting)Quit(); base.Dispose(disposing); }
}
}
