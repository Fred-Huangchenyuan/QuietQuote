using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using QuietQuote;
public static class CoreTests {
 static int count;
 static void Check(bool condition,string description){if(!condition)throw new Exception(description);count++;Console.WriteLine("PASS "+description);}
 static void Reject(string json,string description){bool rejected=false;try{Quote.Parse(json);}catch{rejected=true;}Check(rejected,description);}
 static string Fixture(string price,string id,string stamp){return "{\"code\":\"0\",\"data\":[{\"instId\":\""+id+"\",\"instType\":\"SWAP\",\"last\":\""+price+"\",\"ts\":\""+stamp+"\"}]}";}
 public static int Main(){try{
  DateTime now=DateTime.UtcNow;string stamp=((long)(now-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds).ToString();
  var q=Quote.Parse(Fixture("7.0300",Quote.Instrument,stamp));Check(q.Display=="7.03"&&q.Price==7.03m,"exact decimal parsing");
  var btc=Quote.Parse(Fixture("75000.25","BTC-USD-SWAP",stamp),"BTC-USD-SWAP");Check(btc.InstrumentId=="BTC-USD-SWAP","selected instrument identity");
  Reject(Fixture("1","BTC-USDT-SWAP",stamp),"reject mismatched instrument");
  Reject(Fixture("0",Quote.Instrument,stamp),"reject zero price");Reject(Fixture("NaN",Quote.Instrument,stamp),"reject invalid price");
  Reject(Fixture("1",Quote.Instrument,"9223372036854775807"),"reject impossible timestamp");
  Reject("{\"code\":\"50011\",\"data\":[]}","reject API errors");Reject("{\"code\":\"0\",\"data\":[]}","reject missing contract");Reject("<html>error</html>","reject non-JSON");
  Check(!q.IsOld(now,3)&&q.IsOld(now.AddSeconds(91),3),"stale quote boundary");Check(q.IsOld(now.AddSeconds(-61),3),"future quote clock discrepancy");Check(!q.IsOld(now.AddSeconds(300),300),"long refresh interval");
  Check(Market.FromId("BTC-USD-SWAP").Unit=="USD","quote currency differs from settlement coin");Check(Market.FromId("BTC-USDT")==null,"spot market excluded");
  var markets=Market.Parse("{\"code\":\"0\",\"data\":[{\"instId\":\"BTC-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"live\"},{\"instId\":\"ETH-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"suspend\"}]}");Check(markets.Count==1&&markets[0].Coin=="BTC","suspended markets excluded");
  var s=new Settings{Language="en",Theme="light",Shape="circle",CapsuleWidth=220,CapsuleHeight=64,Interval=7};s.Save();var loaded=Settings.Load();Check(loaded.Language=="en"&&loaded.CapsuleWidth==220&&loaded.Interval==7,"settings round trip");
  s.Interval=13;s.Save();Check(Settings.Load().Interval==13,"atomic settings replacement");File.WriteAllText(Settings.FilePath,"broken");Check(Settings.Load().Interval==3,"corrupted settings recovery");File.Delete(Settings.FilePath);
  s=new Settings{CapsuleHeight=100,CapsuleWidth=100,Alpha=999,Interval=0};s.Normalize();Check(s.CapsuleWidth>=s.CapsuleHeight+40&&s.Alpha==100&&s.Interval==1,"settings constraints");
  Check(Appearance.ChooseLight(.9,false)&&!Appearance.ChooseLight(.1,true),"background theme switching");Check(Appearance.ChooseLight(.5,true)&&!Appearance.ChooseLight(.5,false),"theme hysteresis");
  using(var p=Appearance.Outline(new RectangleF(0,0,156,52),false)){Check(!p.IsVisible(0,0)&&p.IsVisible(78,26),"capsule path geometry");}
  using(var b=new Bitmap(240,52))using(var g=Graphics.FromImage(b)){SegmentDigits.Draw(g,"0123456789.123",new RectangleF(5,5,230,42),30,Color.White);int painted=0;for(int x=0;x<b.Width;x++)for(int y=0;y<b.Height;y++)if(b.GetPixel(x,y).A>0)painted++;Check(painted>100,"all decimal digits render");}
  NewMarkets(now,stamp);
  Console.WriteLine("ALL PASSED: "+count);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static void MustFail(Action action,string message){bool failed=false;try{action();}catch{failed=true;}Check(failed,message);}
 static void NewMarkets(DateTime now,string stamp){
  Check(Market.FromId("ETH-BTC","SPOT").Unit=="BTC","spot supports actual non-stablecoin quote currencies");
  Check(Market.FromId("BTC-USDT-SWAP","SPOT")==null && Market.FromId("BTC-USDT","SWAP")==null,"spot/perpetual identities stay distinct");
  MustFail(()=>Feed.TickerUrl("Injected","SPOT","BTC-USDT"),"reject unsupported source");
  MustFail(()=>Feed.TickerUrl("Binance","SPOT","BTC-USDT&symbol=ETHUSDT"),"reject invalid instrument in request");
  Check(Feed.TickerUrl("Binance","SPOT","BTC-USDT")=="https://data-api.binance.vision/api/v3/ticker/price?symbol=BTCUSDT","Binance public spot route");
  Check(Feed.TickerUrl("Binance","SWAP","ETH-USDC-SWAP")=="https://fapi.binance.com/fapi/v2/ticker/price?symbol=ETHUSDC","Binance USD-M perpetual route");
  Check(Feed.CatalogUrl("OKX","SPOT").EndsWith("instType=SPOT"),"OKX spot catalog route");
  var spot=Quote.Parse(Fixture("12.34","BTC-USDT",stamp).Replace("SWAP","SPOT"),"BTC-USDT","SPOT");
  Check(spot.Price==12.34m&&!spot.ReceiptTime,"OKX spot validates exchange timestamp");
  MustFail(()=>Quote.Parse(Fixture("12","BTC-USDT",stamp),"BTC-USDT","SPOT"),"OKX wrong market type rejected");
  string ticker="{\"symbol\":\"BTCUSDT\",\"price\":\"65000.1200\",\"time\":"+stamp+"}";
  var futures=Quote.ParseBinance(ticker,"BTC-USDT-SWAP","SWAP",now);
  Check(futures.Display=="65000.12"&&!futures.ReceiptTime&&!futures.IsOld(now,3),"Binance futures price and timestamp");
  var cash=Quote.ParseBinance("{\"symbol\":\"BTCUSDT\",\"price\":\"0.00000120\"}","BTC-USDT","SPOT",now);
  Check(cash.Display=="0.0000012"&&cash.ReceiptTime&&cash.TimeUtc==now,"Binance spot explicitly uses receipt time");
  MustFail(()=>Quote.ParseBinance(ticker,"ETH-USDT-SWAP","SWAP",now),"Binance mismatched symbol rejected");
  MustFail(()=>Quote.ParseBinance(ticker.Replace("65000.1200","0"),"BTC-USDT","SPOT",now),"Binance zero rejected");
  MustFail(()=>Quote.ParseBinance(ticker.Replace("65000.1200","NaN"),"BTC-USDT","SPOT",now),"Binance invalid price rejected");
  MustFail(()=>Quote.ParseBinance("{\"code\":-1121,\"msg\":\"Invalid symbol\"}","BTC-USDT","SPOT",now),"Binance API error rejected");
  MustFail(()=>Quote.ParseBinance("{\"symbol\":\"BTCUSDT\",\"price\":\"1\"}","BTC-USDT-SWAP","SWAP",now),"Binance futures missing timestamp rejected");
  MustFail(()=>Quote.ParseBinance(ticker.Replace(stamp,"9223372036854775807"),"BTC-USDT-SWAP","SWAP",now),"Binance impossible timestamp rejected");
  string catalog="{\"symbols\":[{\"symbol\":\"BTCUSDT\",\"baseAsset\":\"BTC\",\"quoteAsset\":\"USDT\",\"status\":\"TRADING\",\"contractType\":\"PERPETUAL\",\"isSpotTradingAllowed\":true},{\"symbol\":\"ETHUSDT\",\"baseAsset\":\"ETH\",\"quoteAsset\":\"USDT\",\"status\":\"BREAK\",\"contractType\":\"PERPETUAL\",\"isSpotTradingAllowed\":true},{\"symbol\":\"SOLUSDT\",\"baseAsset\":\"SOL\",\"quoteAsset\":\"USDT\",\"status\":\"TRADING\",\"contractType\":\"CURRENT_QUARTER\",\"isSpotTradingAllowed\":false}]}";
  Check(Market.Parse(catalog,"Binance","SPOT").Count==1,"Binance spot excludes suspended and disallowed markets");
  var list=Market.Parse(catalog,"Binance","SWAP");Check(list.Count==1&&list[0].Id=="BTC-USDT-SWAP","Binance excludes delivery contracts and halted symbols");
  MustFail(()=>Market.Parse(catalog.Replace("BTCUSDT","FAKEUSDT"),"Binance","SWAP"),"Binance rejects inconsistent native symbol");
  Check(Market.Parse("{\"code\":\"0\",\"data\":[{\"instId\":\"ETH-BTC\",\"instType\":\"SPOT\",\"state\":\"live\"}]}","OKX","SPOT")[0].Unit=="BTC","OKX spot catalog supports quote selection");
  var legacy=new JavaScriptSerializer().Deserialize<Settings>("{\"InstrumentId\":\"ETH-USD-SWAP\",\"Interval\":7,\"X\":123,\"Y\":456}");legacy.EnsureWindows();
  Check(legacy.Windows.Count==1&&legacy.Windows[0].InstrumentId=="ETH-USD-SWAP"&&legacy.Windows[0].X==123&&legacy.Interval==7,"v1.3 settings migrate without losing selected coin or position");
  legacy.Windows.Add(new WindowSpec{Exchange="Binance",Kind="SPOT",InstrumentId="BTC-USDT",X=321,Y=654});legacy.Save();var loaded=Settings.Load();loaded.EnsureWindows();
  Check(loaded.Windows.Count==2&&loaded.Windows[1].Exchange=="Binance"&&loaded.Windows[1].Kind=="SPOT"&&loaded.Windows[1].Y==654,"independent window choices and positions survive restart");
  var copy=loaded.Copy();copy.Windows[1].InstrumentId="ETH-USDT";Check(loaded.Windows[1].InstrumentId=="BTC-USDT","cancelled settings edits do not mutate active configuration");
  for(int i=0;i<10;i++)loaded.Windows.Add(null);loaded.EnsureWindows();Check(loaded.Windows.Count==8&&loaded.Windows[7]!=null,"window count bounded and malformed slots repaired");
  File.Delete(Settings.FilePath);
 }

}
