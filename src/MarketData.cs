using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace QuietQuote {
public sealed class WindowSpec {
    public string Exchange = "OKX", Kind = "SWAP", InstrumentId = Quote.Instrument;
    public int X = -32000, Y = -32000;
    public string Key { get { return Exchange + ":" + Kind + ":" + InstrumentId; } }
    public void Normalize() {
        if (Exchange != "Binance") Exchange = "OKX";
        if (Kind != "SPOT") Kind = "SWAP";
        if (Market.FromId(InstrumentId, Kind) == null) InstrumentId = Kind == "SPOT" ? "BTC-USDT" : Exchange == "OKX" ? Quote.Instrument : "BTC-USDT-SWAP";
    }
    public WindowSpec Copy() { return (WindowSpec)MemberwiseClone(); }
}
public sealed class Market {
    public string Id, Coin, Unit;
    public string Symbol { get { return Coin + Unit; } }
    public static Market FromId(string id) { return FromId(id, "SWAP"); }
    public static Market FromId(string id, string kind) {
        if (kind != "SPOT" && kind != "SWAP") return null;
        string pattern = kind == "SPOT" ? @"^[A-Z0-9]{1,60}-[A-Z0-9]{1,20}$" : @"^[A-Z0-9]{1,60}-[A-Z0-9]{1,20}-SWAP$";
        if (String.IsNullOrEmpty(id) || !System.Text.RegularExpressions.Regex.IsMatch(id, pattern)) return null;
        var p = id.Split('-'); return new Market { Id = id, Coin = p[0], Unit = p[1] };
    }
    public static List<Market> Parse(string json) { return Parse(json, "OKX", "SWAP"); }
    public static List<Market> Parse(string json, string exchange, string kind) {
        if ((exchange != "OKX" && exchange != "Binance") || (kind != "SPOT" && kind != "SWAP")) throw new InvalidDataException("Invalid market source");
        var serializer = new JavaScriptSerializer { MaxJsonLength = 16777216 };
        IEnumerable<Market> items;
        if (exchange == "OKX") {
            var r = serializer.Deserialize<CatalogEnvelope>(json);
            if (r == null || r.code != "0" || r.data == null) throw new InvalidDataException("Catalog unavailable");
            items = r.data.Where(x => x != null && x.instType == kind && x.state == "live").Select(x => FromId(x.instId, kind));
        } else {
            var r = serializer.Deserialize<BinanceCatalog>(json);
            if (r == null || r.symbols == null) throw new InvalidDataException("Catalog unavailable");
            items = r.symbols.Where(x => x != null && x.status == "TRADING" &&
                (kind == "SPOT" ? x.isSpotTradingAllowed == true : x.contractType == "PERPETUAL" && (x.quoteAsset == "USDT" || x.quoteAsset == "USDC")) &&
                x.symbol == x.baseAsset + x.quoteAsset)
                .Select(x => FromId(x.baseAsset + "-" + x.quoteAsset + (kind == "SWAP" ? "-SWAP" : ""), kind));
        }
        var list = items.Where(x => x != null).GroupBy(x => x.Id).Select(x => x.First()).OrderBy(x => x.Coin).ThenBy(x => x.Unit).ToList();
        if (list.Count == 0) throw new InvalidDataException("No available instruments"); return list;
    }
}
public sealed class CatalogEnvelope { public string code; public CatalogRow[] data; }
public sealed class CatalogRow { public string instId, instType, state; }
public sealed class BinanceCatalog { public BinanceInstrument[] symbols; }
public sealed class BinanceInstrument { public string symbol, baseAsset, quoteAsset, status, contractType; public bool? isSpotTradingAllowed; }
public sealed class TickerEnvelope { public string code, msg; public TickerRow[] data; }
public sealed class TickerRow { public string instId, instType, last, ts; }
public sealed class BinanceTicker { public string symbol, price; public long time; }
public sealed class Quote {
    public const string Instrument = "KORU-USDT-SWAP";
    public decimal Price;
    public string InstrumentId;
    public DateTime TimeUtc;
    public bool ReceiptTime;
    public string Display { get { return Price.ToString("0.####################", CultureInfo.InvariantCulture); } }
    static decimal ParsePrice(string value) {
        decimal price;
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out price) || price <= 0) throw new InvalidDataException("Invalid price");
        return price;
    }
    static DateTime ParseTime(long stamp) {
        if (stamp <= 0) throw new InvalidDataException("Invalid quote time");
        try { return new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(stamp); }
        catch (ArgumentOutOfRangeException) { throw new InvalidDataException("Invalid quote time"); }
    }
    public static Quote Parse(string json, string expected = Instrument) { return Parse(json, expected, "SWAP"); }
    public static Quote Parse(string json, string expected, string kind) {
        var r = new JavaScriptSerializer().Deserialize<TickerEnvelope>(json);
        if (r == null || r.code != "0" || r.data == null || r.data.Length != 1) throw new InvalidDataException("Quote unavailable");
        var row = r.data[0]; long stamp;
        if (Market.FromId(expected, kind) == null || row == null || row.instId != expected || row.instType != kind) throw new InvalidDataException("Mismatched instrument");
        if (!long.TryParse(row.ts, out stamp)) throw new InvalidDataException("Invalid quote time");
        return new Quote { InstrumentId = expected, Price = ParsePrice(row.last), TimeUtc = ParseTime(stamp) };
    }
    public static Quote ParseBinance(string json, string expected, string kind, DateTime receivedUtc) {
        var market = Market.FromId(expected, kind);
        var row = new JavaScriptSerializer().Deserialize<BinanceTicker>(json);
        if (market == null || row == null || row.symbol != market.Symbol) throw new InvalidDataException("Mismatched instrument");
        // Binance spot price ticker has no exchange timestamp. Label receipt time explicitly.
        return new Quote { InstrumentId = expected, Price = ParsePrice(row.price), ReceiptTime = kind == "SPOT",
            TimeUtc = kind == "SPOT" ? receivedUtc : ParseTime(row.time) };
    }
    public bool IsOld(DateTime now, int interval) { return (now - TimeUtc).TotalSeconds > Math.Max(90, interval + 30) || (TimeUtc - now).TotalSeconds > 60; }
}
public sealed class Feed : IDisposable {
    readonly HttpClient client;
    public Feed() : this(new HttpClientHandler { UseProxy = true, DefaultProxyCredentials = CredentialCache.DefaultCredentials }) { }
    internal Feed(HttpMessageHandler handler) {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10), MaxResponseContentBufferSize = 16777216 };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("QuietQuote/1.4");
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true, NoStore = true };
    }
    public static string CatalogUrl(string exchange, string kind) {
        Validate(exchange, kind);
        if (exchange == "OKX") return "https://www.okx.com/api/v5/public/instruments?instType=" + kind;
        return kind == "SPOT" ? "https://data-api.binance.vision/api/v3/exchangeInfo?permissions=SPOT&showPermissionSets=false&symbolStatus=TRADING" : "https://fapi.binance.com/fapi/v1/exchangeInfo";
    }
    static void Validate(string exchange, string kind) {
        if ((exchange != "OKX" && exchange != "Binance") || (kind != "SPOT" && kind != "SWAP")) throw new InvalidDataException("Invalid source");
    }
    public static string TickerUrl(string exchange, string kind, string instrument) {
        Validate(exchange, kind);
        var m = Market.FromId(instrument, kind); if (m == null) throw new InvalidDataException("Invalid instrument");
        if (exchange == "OKX") return "https://www.okx.com/api/v5/market/ticker?instId=" + Uri.EscapeDataString(instrument);
        return (kind == "SPOT" ? "https://data-api.binance.vision/api/v3/ticker/price?symbol=" : "https://fapi.binance.com/fapi/v2/ticker/price?symbol=") + Uri.EscapeDataString(m.Symbol);
    }
    async Task<string> Get(string url, CancellationToken token) {
        using (var r = await client.GetAsync(url, token)) { r.EnsureSuccessStatusCode(); return await r.Content.ReadAsStringAsync(); }
    }
    public Task<Quote> Fetch(CancellationToken token) { return Fetch(Quote.Instrument, token); }
    public Task<Quote> Fetch(string instrument, CancellationToken token) { return Fetch("OKX", "SWAP", instrument, token); }
    public async Task<Quote> Fetch(string exchange, string kind, string instrument, CancellationToken token) {
        string json = await Get(TickerUrl(exchange, kind, instrument), token);
        return exchange == "OKX" ? Quote.Parse(json, instrument, kind) : Quote.ParseBinance(json, instrument, kind, DateTime.UtcNow);
    }
    public Task<string> Catalog(CancellationToken token) { return Catalog("OKX", "SWAP", token); }
    public Task<string> Catalog(string exchange, string kind, CancellationToken token) { return Get(CatalogUrl(exchange, kind), token); }
    public void Dispose() { client.Dispose(); }
}
}
