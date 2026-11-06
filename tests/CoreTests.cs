using System;
using System.IO;
using System.Drawing;
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
  Console.WriteLine("ALL PASSED: "+count);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
