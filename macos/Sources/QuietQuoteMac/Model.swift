import AppKit
import SwiftUI
import QuietQuoteCore

struct Preferences: Codable, Equatable {
    var instrument = "KORU-USDT-SWAP"
    var interval: Double = 3
    var language = "zh"
    var circle = false
    var width: Double = 156
    var height: Double = 52
    var diameter: Double = 88
    var glass = true
    var theme = "auto"
    var opacity: Double = 0.85
    var showSymbol = false
    var top = true
    mutating func normalize() {
        if Market(instrument) == nil { instrument = "KORU-USDT-SWAP" }
        interval = interval.isFinite ? min(3600, max(1, interval)) : 3
        height = height.isFinite ? min(100, max(36, height)) : 52
        width = width.isFinite ? min(360, max(height + 40, width)) : 156
        diameter = diameter.isFinite ? min(180, max(64, diameter)) : 88
        opacity = opacity.isFinite ? min(1, max(0.3, opacity)) : 0.85
        language = language == "en" ? "en" : "zh"
        if !["auto", "light", "dark"].contains(theme) { theme = "auto" }
    }
}
@MainActor final class QuoteModel: ObservableObject {
    @Published var preferences: Preferences
    @Published var markets = [Market("KORU-USDT-SWAP")!]
    @Published var quote: Quote?
    @Published var now = Date()
    @Published var failed = false
    @Published var catalogFailed = false
    @Published var catalogLoading = false
    var onPreferencesChanged: (() -> Void)?
    private var appliedInstrument: String
    private var generation = 0
    private var busy = false
    private var errors = 0
    private var next = Date.distantPast
    private var timer: Timer?
    private var received = Date.distantPast
    init() {
        var p = UserDefaults.standard.data(forKey: "preferences").flatMap { try? JSONDecoder().decode(Preferences.self, from: $0) } ?? Preferences()
        p.normalize(); preferences = p; appliedInstrument = p.instrument
        if let m = Market(p.instrument) { markets = [m] }
    }
    func t(_ zh: String, _ en: String) -> String { preferences.language == "en" ? en : zh }
    var market: Market { Market(preferences.instrument)! }
    var coins: [String] { Array(Set(markets.map(\.coin))).sorted() }
    var units: [String] { markets.filter { $0.coin == market.coin }.map(\.unit).sorted() }
    var state: String {
        if failed { return t("断线", "Offline") }
        guard let q = quote else { return t("连接中", "Connecting") }
        if q.isStale(now: now, interval: preferences.interval) || now.timeIntervalSince(received) > max(30, preferences.interval + 15) { return t("过期", "Stale") }
        return ""
    }
    func selectCoin(_ coin: String) {
        guard let m = markets.first(where: { $0.coin == coin && $0.unit == "USDT" }) ?? markets.first(where: { $0.coin == coin }) else { return }
        preferences.instrument = m.id
    }
    func selectUnit(_ unit: String) {
        if let m = markets.first(where: { $0.coin == market.coin && $0.unit == unit }) { preferences.instrument = m.id }
    }
    func apply() {
        preferences.normalize()
        if appliedInstrument != preferences.instrument { generation += 1; appliedInstrument = preferences.instrument; quote = nil; failed = false; errors = 0 }
        if let data = try? JSONEncoder().encode(preferences) { UserDefaults.standard.set(data, forKey: "preferences") }
        next = .distantPast; onPreferencesChanged?()
    }
    func start() {
        timer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            Task { @MainActor in guard let self = self else { return }; self.now = Date(); if self.now >= self.next { await self.refresh() } }
        }
        Task { await refresh() }
    }
    func refresh() async {
        guard !busy else { return }; busy = true
        let id = preferences.instrument, token = generation
        defer { busy = false; next = token == generation ? Date().addingTimeInterval(errors == 0 ? preferences.interval : max(preferences.interval, min(60, pow(2, Double(min(errors, 6)))))) : .distantPast }
        do {
            let data = try await PublicAPI.data(path: "/api/v5/market/ticker", query: [URLQueryItem(name: "instId", value: id)])
            let q = try Quote.parse(data, expected: id)
            guard token == generation else { return }
            if let old = quote, q.time < old.time { throw MarketError.invalidTime }
            quote = q; received = Date(); failed = false; errors = 0
        } catch { if token == generation { failed = true; errors += 1 } }
    }
    func loadMarkets() async {
        guard !catalogLoading else { return }; catalogLoading = true; defer { catalogLoading = false }
        do {
            let data = try await PublicAPI.data(path: "/api/v5/public/instruments", query: [URLQueryItem(name: "instType", value: "SWAP")])
            var list = try PublicAPI.catalog(data)
            if !list.contains(where: { $0.id == preferences.instrument }) { list.append(market) }
            markets = list.sorted { $0.id < $1.id }; catalogFailed = false
        } catch { catalogFailed = true }
    }
}
