import AppKit
import SwiftUI

struct GlassView: NSViewRepresentable {
    func makeNSView(context: Context) -> NSVisualEffectView {
        let v = NSVisualEffectView(); v.material = .hudWindow; v.blendingMode = .behindWindow; v.state = .active; return v
    }
    func updateNSView(_ nsView: NSVisualEffectView, context: Context) {}
}
struct DigitalPrice: View {
    let text: String
    let color: Color
    private let masks = [63, 6, 91, 79, 102, 109, 125, 7, 127, 111]
    var body: some View {
        Canvas { context, size in
            let count: CGFloat = text.reduce(CGFloat(0)) { $0 + ($1 == "." ? 4 : 12) }
            let scale = min(size.height / 20, size.width / max(1, count))
            var c = context; c.translateBy(x: (size.width - count * scale) / 2, y: (size.height - 20 * scale) / 2); c.scaleBy(x: scale, y: scale)
            let starts: [CGPoint] = [.init(x: 2,y: 1), .init(x: 9,y: 2), .init(x: 9,y: 10), .init(x: 2,y: 18), .init(x: 1,y: 10), .init(x: 1,y: 2), .init(x: 2,y: 9.5)]
            let ends: [CGPoint] = [.init(x: 8,y: 1), .init(x: 9,y: 9), .init(x: 9,y: 17), .init(x: 8,y: 18), .init(x: 1,y: 17), .init(x: 1,y: 9), .init(x: 8,y: 9.5)]
            for char in text {
                if char == "." { c.fill(Path(ellipseIn: CGRect(x: 0,y: 16,width: 2.1,height: 2.1)), with: .color(color)); c.translateBy(x: 4,y: 0); continue }
                let mask = char.wholeNumberValue.map { masks[$0] } ?? 64
                for i in 0..<7 where mask & (1 << i) != 0 {
                    var p = Path(); p.move(to: starts[i]); p.addLine(to: ends[i]); c.stroke(p, with: .color(color), style: StrokeStyle(lineWidth: 1.25, lineCap: .square))
                }
                c.translateBy(x: 12, y: 0)
            }
        }
    }
}
@MainActor struct OverlayView: View {
    @ObservedObject var model: QuoteModel
    @Environment(\.colorScheme) var systemScheme
    var dark: Bool { model.preferences.glass || model.preferences.theme == "dark" || (model.preferences.theme == "auto" && systemScheme == .dark) }
    var body: some View {
        let p = model.preferences
        let w = p.circle ? p.diameter : p.width, h = p.circle ? p.diameter : p.height
        ZStack {
            if p.glass { GlassView().environment(\.colorScheme, .dark) }
            RoundedRectangle(cornerRadius: h / 2).fill((dark ? Color(red: 0.08,green: 0.14,blue: 0.2) : Color(red: 0.94,green: 0.98,blue: 0.98)).opacity(p.opacity * (p.glass ? 0.55 : 1)))
            DigitalPrice(text: model.quote?.text ?? "—", color: dark ? Color(red: 0.82,green: 0.98,blue: 0.94) : Color(red: 0.1,green: 0.26,blue: 0.27))
                .frame(width: w - 30, height: min(28, max(12, h - (p.showSymbol || !model.state.isEmpty ? 30 : 16))))
            if p.showSymbol { VStack { Text("\(model.market.coin) / \(model.market.unit)").font(.system(size: 9)); Spacer() }.padding(.top, p.circle ? h * 0.18 : 4) }
            if !model.state.isEmpty { VStack { Spacer(); Text(model.state).font(.system(size: 9)).foregroundColor(dark ? .orange : .brown) }.padding(.bottom, p.circle ? h * 0.17 : 4) }
        }
        .foregroundColor(dark ? .white : .black)
        .frame(width: w, height: h)
        .clipShape(RoundedRectangle(cornerRadius: h / 2))
        .overlay(RoundedRectangle(cornerRadius: h / 2).stroke(Color.white.opacity(0.5), lineWidth: 1).padding(1))
        .help("OKX · \(p.instrument)\n\(model.t("最新成交价", "Last traded price")) · \(Int(p.interval))s")
    }
}
@MainActor struct SettingsView: View {
    @ObservedObject var model: QuoteModel
    var body: some View {
        Form {
            Picker(model.t("语言", "Language"), selection: $model.preferences.language) { Text("中文").tag("zh"); Text("English").tag("en") }
            Picker(model.t("币种", "Coin"), selection: Binding(get: { model.market.coin }, set: { model.selectCoin($0) })) { ForEach(model.coins, id: \.self) { Text($0).tag($0) } }
            Picker(model.t("计价单位", "Quote currency"), selection: Binding(get: { model.market.unit }, set: { model.selectUnit($0) })) { ForEach(model.units, id: \.self) { Text($0).tag($0) } }
            HStack {
                Button(model.t("更新合约列表", "Refresh instruments")) { Task { await model.loadMarkets() } }.disabled(model.catalogLoading)
                if model.catalogLoading { ProgressView().controlSize(.small) }
                if model.catalogFailed { Text(model.t("读取失败，可重试", "Unavailable; retry")).foregroundColor(.orange) }
            }
            Stepper(value: $model.preferences.interval, in: 1...3600, step: 1) { Text(model.t("刷新间隔", "Refresh interval") + ": \(Int(model.preferences.interval))s") }
            Toggle(model.t("圆形（关闭为胶囊）", "Circle (off = capsule)"), isOn: $model.preferences.circle)
            if model.preferences.circle {
                Slider(value: $model.preferences.diameter, in: 64...180, step: 1) { Text(model.t("直径", "Diameter")) }
            } else {
                Slider(value: $model.preferences.width, in: 140...360, step: 1) { Text(model.t("宽度", "Width")) }
                Slider(value: $model.preferences.height, in: 36...100, step: 1) { Text(model.t("高度", "Height")) }
            }
            Slider(value: $model.preferences.opacity, in: 0.3...1) { Text(model.t("背景浓度", "Background opacity")) }
            Toggle(model.t("毛玻璃", "Glass blur"), isOn: $model.preferences.glass)
            Picker(model.t("非玻璃配色", "Non-glass theme"), selection: $model.preferences.theme) {
                Text(model.t("跟随系统", "Follow system")).tag("auto"); Text(model.t("浅色", "Light")).tag("light"); Text(model.t("深色", "Dark")).tag("dark")
            }.disabled(model.preferences.glass)
            Toggle(model.t("显示币种和单位", "Show coin and currency"), isOn: $model.preferences.showSymbol)
            Toggle(model.t("始终置顶", "Always on top"), isOn: $model.preferences.top)
            Text(model.t("设置自动保存。预览版自动配色跟随系统，不读取屏幕。", "Settings save automatically. Auto theme follows the system; no screen capture.")).font(.footnote).foregroundColor(.secondary)
        }
        .formStyle(.grouped).padding().frame(width: 440, height: 640)
        .onChange(of: model.preferences) { _ in model.apply() }
        .task { await model.loadMarkets() }
    }
}
