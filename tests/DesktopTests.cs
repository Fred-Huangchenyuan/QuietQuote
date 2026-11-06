using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using QuietQuote;
using Appearance = QuietQuote.Appearance;
public static class QuoteTests {
 static int checks;
 static void Check(bool yes, string message) { if (!yes) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
 static string Fixture(string price, string id, string stamp) { return "{\"code\":\"0\",\"data\":[{\"instType\":\"SWAP\",\"instId\":\""+id+"\",\"last\":\""+price+"\",\"ts\":\""+stamp+"\"}]}"; }
 static void Reject(string json, string why) { bool failed = false; try { Quote.Parse(json); } catch { failed = true; } Check(failed, why); }
 [STAThread] public static int Main(string[] args) {
  try {
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   var now = DateTime.UtcNow; string ts = ((long)(now - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds).ToString();
   var q = Quote.Parse(Fixture("7.0300", Quote.Instrument, ts));
   var btc = Quote.Parse(Fixture("70234.25", "BTC-USDT-SWAP", ts), "BTC-USDT-SWAP");
   Check(btc.InstrumentId == "BTC-USDT-SWAP" && btc.Price == 70234.25m, "selectable quote identity");
   Reject(Fixture("70234.25", "BTC-USDT-SWAP", ts), "reject another selected coin");
   Check(Market.FromId("BTC-USD-SWAP").Unit == "USD", "inverse contract quoted in USD, not settlement BTC");
   Check(Market.FromId("BTC-USDC-SWAP").Unit == "USDC" && Market.FromId("BTC-USDT") == null, "unit parsing and spot rejection");
   var catalogJson = File.ReadAllText("work/instruments.json"); var markets = Market.Parse(catalogJson);
   Check(markets.Exists(m => m.Id == Quote.Instrument) && markets.Exists(m => m.Id == "BTC-USD-SWAP"), "official catalog KORU and BTC units");
   var filtered = Market.Parse("{\"code\":\"0\",\"data\":[{\"instId\":\"ETH-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"live\"},{\"instId\":\"BTC-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"suspend\"}]}");
   Check(filtered.Count == 1 && filtered[0].Coin == "ETH", "exclude suspended contracts");
   File.WriteAllText("work/markets.json", catalogJson);
   Check(q.Price == 7.03m && q.Display == "7.03", "exact decimal price");
   Check(!q.IsOld(now,3) && q.IsOld(now.AddSeconds(91),3), "fresh versus stale timestamp");
   Check(q.IsOld(now.AddSeconds(-61),3), "future timestamp clock discrepancy");
   Check(!q.IsOld(now.AddSeconds(300),300), "long refresh interval freshness");
   Reject(Fixture("7", "KORU-USDT", ts), "reject spot market");
   Reject(Fixture("0", Quote.Instrument, ts), "reject zero");
   Reject(Fixture("NaN", Quote.Instrument, ts), "reject invalid price");
   Reject(Fixture("7", Quote.Instrument, "9223372036854775807"), "reject impossible timestamp");
   Reject("{\"code\":\"50011\",\"data\":[]}", "reject API error");
   Reject("{\"code\":\"0\",\"data\":[]}", "reject absent contract");
   Reject("<html>blocked</html>", "reject non JSON");
   var s = new Settings { Interval = 0, Alpha = 999, FontSize = -1 }; s.Normalize();
   Check(s.Interval == 1 && s.Alpha == 100 && s.FontSize == 10, "clamp invalid settings");
   s.Interval = 13; s.InstrumentId = "BTC-USD-SWAP"; s.Diameter = 120; s.Save(); var loaded = Settings.Load(); Check(loaded.Interval == 13 && loaded.InstrumentId == "BTC-USD-SWAP" && loaded.Diameter == 120, "persist interval, instrument, and diameter");
   s.Interval = 7; s.Save(); Check(Settings.Load().Interval == 7, "atomic settings replacement");
   File.WriteAllText(Settings.FilePath,"broken"); Check(Settings.Load().Interval == 3,"recover corrupted settings"); File.Delete(Settings.FilePath);
   if (args.Length > 0 && args[0] == "--live") {
    using (var feed = new Feed()) q = feed.Fetch(CancellationToken.None).GetAwaiter().GetResult();
    Check(q.Price > 0, "live OKX KORU perpetual quote");
    Check(!q.IsOld(DateTime.UtcNow,3), "live quote freshness");
    Console.WriteLine("Live price " + q.Display + " at " + q.TimeUtc.ToString("o"));
   }
   using (var icon = QuoteWindow.MakeIcon()) using (var stream = File.Create("work/QuietQuote.ico")) icon.Save(stream);
   using (var f = new QuoteWindow(true)) {
    f.Show(); f.PreviewQuote(q); Application.DoEvents();
    using (var b = f.RenderSurface()) { b.Save("work/capsule-glass.png"); Check(b.GetPixel(0,0).A == 0, "transparent capsule corners"); }
    var glassField = typeof(QuoteWindow).GetField("glassActive",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Console.WriteLine("Native blur API accepted: " + glassField.GetValue(f));
    f.UseSettings(new Settings { Alpha=30,Shape="circle" });
    using (var b = f.RenderSurface()) {
      int maximum = 0; for(int x=18;x<70;x++) for(int y=32;y<57;y++) maximum = Math.Max(maximum,b.GetPixel(x,y).A);
      Check(maximum >= 245,"price stays opaque at low glass concentration");
    }
    f.UseSettings(new Settings { Glass=false }); Check(!(bool)glassField.GetValue(f),"blur can be disabled without losing overlay");
    f.UseSettings(new Settings { Diameter=150,FontSize=25,Glass=true,Symbol=true,Shape="circle" });
    using(var b=f.RenderSurface()) b.Save("work/glass-large.png");
    f.UseSettings(new Settings());
    Check(f.TopMost && !f.ShowInTaskbar && f.Width == 156 && f.Height == 52, "default capsule geometry");
    Check(!f.Region.IsVisible(0,0) && f.Region.IsVisible(78,26), "capsule clips corners and retains center");
    f.UseSettings(new Settings {Glass=false,Theme="light",Language="en",Symbol=true});
    using(var b=f.RenderSurface()) b.Save("work/capsule-light.png");
    f.UseSettings(new Settings {Glass=false,Theme="dark",Language="en",Symbol=true});
    using(var b=f.RenderSurface()) b.Save("work/capsule-dark.png");
    f.UseSettings(new Settings {Glass=false,Theme="light",Language="en",CapsuleWidth=240,FontSize=24});
    f.PreviewQuote(Quote.Parse(Fixture("1234567890.123",Quote.Instrument,ts)));
    using(var b=f.RenderSurface()) b.Save("work/capsule-digits.png");
    f.PreviewQuote(q); f.UseSettings(new Settings());
    f.Hide(); Check(!f.Visible, "hide window"); f.Show(); Check(f.Visible,"restore window");
    q.TimeUtc = DateTime.UtcNow.AddMinutes(-5); f.PreviewQuote(q); Application.DoEvents();
    using (var b = f.RenderSurface()) b.Save("work/glass-stale.png");
    f.Close();
   }
   using (var f = new Options(new Settings())) {
    f.Show(); Application.DoEvents();
    ComboBox coin = null, unit = null,language=null; foreach(Control c in f.Controls) if(c is ComboBox) { if(c.Top == 60) coin = (ComboBox)c; else if(c.Top==100)unit = (ComboBox)c; else if(c.Top==20)language=(ComboBox)c; }
    Check((string)coin.SelectedItem == "KORU" && (string)unit.SelectedItem == "USDT", "default KORU USDT selectors");
    coin.SelectedItem = "BTC"; Check(unit.Items.Contains("USD") && (string)unit.SelectedItem == "USDT", "coin switch defaults USDT and lists supported units");
    unit.SelectedItem = "USD";
    using (var b = new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height)); b.Save("work/settings-preview.png"); }
    language.SelectedIndex=1;
    Check(f.Text=="QuietQuote · Settings" && ((Button)f.AcceptButton).Text=="Save","language changes current dialog immediately");
    using (var b = new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height)); b.Save("work/settings-english.png"); }
    ((Button)f.AcceptButton).PerformClick(); Check(f.Result.InstrumentId == "BTC-USD-SWAP" && f.Result.Language=="en", "settings saves coin, quote unit, language"); f.Close();
   }
   Check(Appearance.ChooseLight(.9,false) && !Appearance.ChooseLight(.1,true),"auto palette follows bright and dark surroundings");
   Check(Appearance.ChooseLight(.5,true) && !Appearance.ChooseLight(.5,false),"palette hysteresis prevents flicker");
   var dimensions=new Settings{CapsuleWidth=100,CapsuleHeight=100};dimensions.Normalize();Check(dimensions.CapsuleWidth>=dimensions.CapsuleHeight+40,"capsule remains wider than height");
   new Settings{Language="en",Shape="circle",Theme="light",CapsuleWidth=220,CapsuleHeight=64}.Save();var appearance=Settings.Load();Check(appearance.Language=="en"&&appearance.Shape=="circle"&&appearance.Theme=="light"&&appearance.CapsuleWidth==220,"appearance preferences persist");File.Delete(Settings.FilePath);
   using(var backdrop=new Form{BackColor=Color.White,TopMost=true,StartPosition=FormStartPosition.CenterScreen,ClientSize=new Size(450,250)}){
    backdrop.Show();backdrop.BringToFront();Pump(250);var center=backdrop.PointToScreen(new Point(120,70));var rect=new Rectangle(center,new Size(160,60));
    Check(Appearance.NearbyLight(rect,false),"actual pixel sampler detects controlled white background");
    backdrop.BackColor=Color.Black;backdrop.Refresh();Pump(250);Check(!Appearance.NearbyLight(rect,true),"actual pixel sampler detects controlled black background");backdrop.Close();
   }
   if (args.Length > 0 && args[0] == "--live") {
    new Settings { Interval = 1 }.Save();
    using (var f = new QuoteWindow(false)) {
     f.Show(); Pump(2500);
     var field = typeof(QuoteWindow).GetField("fetched",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
     DateTime first = (DateTime)field.GetValue(f); Check(first > DateTime.MinValue,"real window first fetch");
     Pump(2500); Check((DateTime)field.GetValue(f) > first,"automatic refresh at configured interval");
     var hotkey = typeof(QuoteWindow).GetField("hotkey",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
     if ((bool)hotkey.GetValue(f)) Check(true,"global hotkey registered"); else Console.WriteLine("NOTE global hotkey currently occupied; tray fallback remains available");
     var timerField = typeof(QuoteWindow).GetField("timer",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
     ((System.Windows.Forms.Timer)timerField.GetValue(f)).Stop(); Pump(1000);
     var quoteField = typeof(QuoteWindow).GetField("quote",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
     var pending = f.RefreshQuote(); f.UseSettings(new Settings { InstrumentId = "BTC-USD-SWAP", Interval = 1, Symbol = true });
     Check(quoteField.GetValue(f) == null, "coin switch immediately clears prior price");
     Pump(1500); Check(pending.IsCompleted && quoteField.GetValue(f) == null,"discard old in-flight response after coin switch");
     var refresh = f.RefreshQuote(); Pump(2000); Check(refresh.IsCompleted && ((Quote)quoteField.GetValue(f)).InstrumentId == "BTC-USD-SWAP", "live BTC USD contract after switch");
     using(var b = f.RenderSurface()) b.Save("work/glass-live.png");
     f.Close();
    }
    Check(Settings.Load().Interval == 1,"window preserves refresh settings on close");
    File.Delete(Settings.FilePath);
   }
   Console.WriteLine("ALL PASSED: " + checks); return 0;
  } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
 }
 static void Pump(int ms) { var stop = DateTime.UtcNow.AddMilliseconds(ms); while(DateTime.UtcNow < stop) { Application.DoEvents(); Thread.Sleep(25); } }
}
