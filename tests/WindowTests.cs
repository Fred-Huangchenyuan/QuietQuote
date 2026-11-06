using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuietQuote;

public static class WindowTests {
 static int checks;
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
 static Quote Current(QuoteWindow w){return (Quote)typeof(QuoteWindow).GetField("quote",Private).GetValue(w);}
 static string State(QuoteWindow w){return (string)typeof(QuoteWindow).GetMethod("State",Private).Invoke(w,null);}
 static void Pump(Task task){var until=DateTime.UtcNow.AddSeconds(3);while(!task.IsCompleted&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}Check(task.IsCompleted,"controlled request completes");task.GetAwaiter().GetResult();}
 static T Control<T>(Form f,string name)where T:System.Windows.Forms.Control{return (T)f.Controls.Find(name,true)[0];}
 sealed class PendingHandler:HttpMessageHandler {
  public List<TaskCompletionSource<HttpResponseMessage>> Calls=new List<TaskCompletionSource<HttpResponseMessage>>();
  public List<string> Urls=new List<string>();
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
   Urls.Add(request.RequestUri.AbsoluteUri);var source=new TaskCompletionSource<HttpResponseMessage>();Calls.Add(source);return source.Task;
  }
  public void Reply(int index,string text,HttpStatusCode status=HttpStatusCode.OK){Calls[index].SetResult(new HttpResponseMessage(status){Content=new StringContent(text)});}
 }
 public static int Run(){
  var handler=new PendingHandler();
  using(var w=new QuoteWindow(true,new Settings{InstrumentId="BTC-USDT-SWAP",Language="en"},null,true,new Feed(handler))){
   w.Show();var pending=w.RefreshQuote();
   w.UseSettings(new Settings{Exchange="Binance",Kind="SWAP",InstrumentId="BTC-USDT-SWAP",Language="en"});
   Check(Current(w)==null,"exchange switch clears previous price immediately");
   handler.Reply(0,"{\"code\":\"0\",\"data\":[{\"instId\":\"BTC-USDT-SWAP\",\"instType\":\"SWAP\",\"last\":\"111\",\"ts\":\"1790000000000\"}]}");Pump(pending);
   Check(Current(w)==null,"late OKX response discarded after switching to Binance with same pair");
   pending=w.RefreshQuote();Check(handler.Urls[1].Contains("fapi.binance.com/fapi/v2/ticker/price"),"overlay uses Binance perpetual endpoint");
   handler.Reply(1,"{\"symbol\":\"BTCUSDT\",\"price\":\"222\",\"time\":1790000000000}");Pump(pending);Check(Current(w).Price==222,"new exchange quote accepted");
   w.UseSettings(new Settings{Exchange="Binance",Kind="SPOT",InstrumentId="BTC-USDT",Language="en"});Check(Current(w)==null,"spot switch clears perpetual price");
   pending=w.RefreshQuote();handler.Reply(2,"{\"symbol\":\"BTCUSDT\",\"price\":\"333\"}");Pump(pending);
   Check(Current(w).Price==333&&Current(w).ReceiptTime&&State(w)=="","spot shows valid price with receipt timestamp");
   pending=w.RefreshQuote();handler.Reply(3,"blocked",HttpStatusCode.Forbidden);Pump(pending);
   Check(State(w)=="Offline"&&Current(w).Price==333,"network failure keeps last quote and marks offline");
   pending=w.RefreshQuote();handler.Reply(4,"{\"symbol\":\"ETHUSDT\",\"price\":\"444\"}");Pump(pending);
   Check(State(w)=="Offline"&&Current(w).Price==333,"mismatched payload never replaces displayed coin");
   pending=w.RefreshQuote();handler.Reply(5,"{\"symbol\":\"BTCUSDT\",\"price\":\"335\"}");Pump(pending);Check(State(w)==""&&Current(w).Price==335,"refresh recovers after network failure");w.Close();
  }
  var prefs=new Settings{Glass=false,Language="en",Symbol=true};prefs.EnsureWindows();
  prefs.Windows.Add(new WindowSpec{Exchange="Binance",Kind="SPOT",InstrumentId="BTC-USDT"});
  using(var desktop=new DesktopSession(prefs,true)){
   Check(desktop.Windows.Count==2,"session creates requested number of windows");
   Check(desktop.Windows[0].CurrentSettings.Exchange=="OKX"&&desktop.Windows[1].CurrentSettings.Exchange=="Binance","windows use independent exchange profiles");
   desktop.Windows[0].PreviewQuote(new Quote{Price=7.25m,TimeUtc=DateTime.UtcNow});desktop.Windows[1].PreviewQuote(new Quote{Price=65000.5m,TimeUtc=DateTime.UtcNow,ReceiptTime=true});
   Check(Current(desktop.Windows[0]).Price==7.25m&&Current(desktop.Windows[1]).Price==65000.5m,"independent window prices do not overwrite each other");
   desktop.Windows[0].Location=new Point(100,150);desktop.Windows[1].Location=new Point(300,250);desktop.Capture();
   Check(prefs.Windows[0].X==100&&prefs.Windows[1].X==300&&prefs.Windows[1].Y==250,"individual drag positions captured");
   desktop.Toggle();Check(!desktop.Windows[0].Visible&&!desktop.Windows[1].Visible,"global toggle hides all windows");
   desktop.Toggle();Check(desktop.Windows[0].Visible&&desktop.Windows[1].Visible,"global toggle restores all windows");
   var smaller=prefs.Copy();smaller.Windows.RemoveAt(1);var removed=desktop.Windows[1];desktop.Apply(smaller);
   Check(desktop.Windows.Count==1&&removed.IsDisposed,"reducing window count disposes removed window");
   Check(Current(desktop.Windows[0]).Price==7.25m,"unchanged window retains quote after settings update");
   var maximum=smaller.Copy();while(maximum.Windows.Count<8)maximum.Windows.Add(new WindowSpec{Exchange="Binance",Kind="SPOT",InstrumentId="ETH-USDT"});desktop.Apply(maximum);
   Check(desktop.Windows.Count==8,"session supports eight parallel overlays");desktop.Quit();Check(desktop.Windows.Count==0,"quit disposes all windows");
  }
  using(var dialog=new Options(prefs,0,new Feed(),false)){
   dialog.Show();Application.DoEvents();Control<NumericUpDown>(dialog,"count").Value=3;
   Control<ComboBox>(dialog,"window").SelectedIndex=1;Control<ComboBox>(dialog,"exchange").SelectedItem="Binance";Control<ComboBox>(dialog,"kind").SelectedIndex=1;
   Control<ComboBox>(dialog,"window").SelectedIndex=2;Control<ComboBox>(dialog,"exchange").SelectedItem="OKX";Control<ComboBox>(dialog,"kind").SelectedIndex=1;
   Control<ComboBox>(dialog,"window").SelectedIndex=0;Check((string)Control<ComboBox>(dialog,"coin").SelectedItem=="KORU","switching settings slot preserves window one");
   Control<ComboBox>(dialog,"language").SelectedIndex=0;
   using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save("work/settings-multi-zh.png");}
   Control<ComboBox>(dialog,"language").SelectedIndex=1;
   using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save("work/settings-multi-en.png");}
   ((Button)dialog.AcceptButton).PerformClick();
   Check(dialog.Result.Windows.Count==3&&dialog.Result.Windows[1].Exchange=="Binance"&&dialog.Result.Windows[1].Kind=="SPOT"&&dialog.Result.Windows[2].Exchange=="OKX"&&dialog.Result.Windows[2].Kind=="SPOT","settings save independent sources and parallel count");
   Check(prefs.Windows.Count==2,"settings dialog operates on isolated draft");
  }
  // Catalog requests can finish out of order after a user changes the active window/source.
  handler=new PendingHandler();
  using(var dialog=new Options(new Settings(),0,new Feed(handler),false)){
   dialog.Show();Control<Button>(dialog,"retry").PerformClick();
   Control<ComboBox>(dialog,"exchange").SelectedItem="Binance";Control<ComboBox>(dialog,"kind").SelectedIndex=1;
   Control<Button>(dialog,"retry").PerformClick();
   handler.Reply(1,"{\"symbols\":[{\"symbol\":\"ETHUSDT\",\"baseAsset\":\"ETH\",\"quoteAsset\":\"USDT\",\"status\":\"TRADING\",\"isSpotTradingAllowed\":true}]}");
   var until=DateTime.UtcNow.AddSeconds(3);while(!Control<ComboBox>(dialog,"coin").Items.Contains("ETH")&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}
   Check(Control<ComboBox>(dialog,"coin").Items.Contains("ETH"),"new source catalog populates selectors");
   handler.Reply(0,"{\"code\":\"0\",\"data\":[{\"instId\":\"KORU-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"live\"}]}");Application.DoEvents();
   Check(!Control<ComboBox>(dialog,"coin").Items.Contains("KORU")&&(string)Control<ComboBox>(dialog,"exchange").SelectedItem=="Binance","late catalog cannot overwrite a different source");dialog.Close();
  }
  return checks;
 }
}
