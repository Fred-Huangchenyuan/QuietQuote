import Foundation

public struct Market: Hashable, Identifiable {
    public let id: String
    public let coin: String
    public let unit: String
    public init?(_ id: String) {
        let parts = id.split(separator: "-", omittingEmptySubsequences: false)
        guard parts.count == 3, parts[2] == "SWAP", ["USDT", "USDC", "USD"].contains(String(parts[1])),
              !parts[0].isEmpty, parts[0].count <= 60,
              parts[0].unicodeScalars.allSatisfy({ (65...90).contains(Int($0.value)) || (48...57).contains(Int($0.value)) }) else { return nil }
        self.id = id; coin = String(parts[0]); unit = String(parts[1])
    }
}
public enum MarketError: Error { case invalidResponse, invalidInstrument, invalidPrice, invalidTime }
private struct Envelope<Row: Decodable>: Decodable { let code: String; let data: [Row] }
private struct CatalogRow: Decodable { let instId: String; let instType: String; let state: String }
private struct TickerRow: Decodable { let instId: String; let instType: String; let last: String; let ts: String }

public struct Quote {
    public let instrument: String
    public let price: Decimal
    public let time: Date
    public var text: String {
        let f = NumberFormatter(); f.locale = Locale(identifier: "en_US_POSIX")
        f.numberStyle = .decimal; f.usesGroupingSeparator = false; f.maximumFractionDigits = 20
        return f.string(from: NSDecimalNumber(decimal: price)) ?? "—"
    }
    public func isStale(now: Date = Date(), interval: Double) -> Bool {
        now.timeIntervalSince(time) > max(90, interval + 30) || time.timeIntervalSince(now) > 60
    }
    public static func parse(_ data: Data, expected: String) throws -> Quote {
        let r = try JSONDecoder().decode(Envelope<TickerRow>.self, from: data)
        guard r.code == "0", r.data.count == 1 else { throw MarketError.invalidResponse }
        let row = r.data[0]
        guard Market(expected) != nil, row.instId == expected, row.instType == "SWAP" else { throw MarketError.invalidInstrument }
        guard row.last.range(of: #"^[0-9]+(?:\.[0-9]+)?$"#, options: .regularExpression) != nil,
              let price = Decimal(string: row.last, locale: Locale(identifier: "en_US_POSIX")), !price.isNaN, price > 0 else { throw MarketError.invalidPrice }
        guard let millis = Double(row.ts), millis.isFinite, millis > 0, millis < 253402300799000 else { throw MarketError.invalidTime }
        return Quote(instrument: row.instId, price: price, time: Date(timeIntervalSince1970: millis / 1000))
    }
}
public enum PublicAPI {
    public static func catalog(_ data: Data) throws -> [Market] {
        let r = try JSONDecoder().decode(Envelope<CatalogRow>.self, from: data)
        guard r.code == "0" else { throw MarketError.invalidResponse }
        let list = Set(r.data.filter { $0.instType == "SWAP" && $0.state == "live" }.compactMap { Market($0.instId) })
        guard !list.isEmpty else { throw MarketError.invalidResponse }
        return list.sorted { $0.id < $1.id }
    }
    public static func data(path: String, query: [URLQueryItem]) async throws -> Data {
        var url = URLComponents(string: "https://www.okx.com")!; url.path = path; url.queryItems = query
        var request = URLRequest(url: url.url!, cachePolicy: .reloadIgnoringLocalCacheData, timeoutInterval: 12)
        request.setValue("QuietQuote-macOS-preview/1.3.0", forHTTPHeaderField: "User-Agent")
        let (data, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse, http.statusCode == 200, data.count <= 4_194_304 else { throw MarketError.invalidResponse }
        return data
    }
}
