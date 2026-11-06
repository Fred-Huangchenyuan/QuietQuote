using System;
using System.Threading;
using QuietQuote;
public static class LiveFeedTests {
 public static int Main() {
  try {
   using(var feed=new Feed()) foreach(var exchange in new[]{"OKX","Binance"}) foreach(var kind in new[]{"SPOT","SWAP"}) {
    var json=feed.Catalog(exchange,kind,CancellationToken.None).GetAwaiter().GetResult();
    var list=Market.Parse(json,exchange,kind);
    string id=kind=="SPOT"?"BTC-USDT":"BTC-USDT-SWAP";
    if(!list.Exists(x=>x.Id==id))throw new Exception("BTC missing from official catalog");
    var quote=feed.Fetch(exchange,kind,id,CancellationToken.None).GetAwaiter().GetResult();
    if(quote.Price<=0 || quote.IsOld(DateTime.UtcNow,3))throw new Exception("Invalid or stale live quote");
    Console.WriteLine("PASS "+exchange+" "+kind+": "+list.Count+" instruments; quote "+quote.Display+"; "+(quote.ReceiptTime?"received ":"exchange time ")+quote.TimeUtc.ToString("o"));
   }
   Console.WriteLine("ALL FOUR LIVE MARKETS PASSED");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
