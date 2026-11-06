import AppKit
import SwiftUI

final class FloatingPanel: NSPanel {
    override var canBecomeKey: Bool { true }
    override func mouseDown(with event: NSEvent) { performDrag(with: event) }
}
@MainActor final class DraggableHostingView: NSHostingView<OverlayView> {
    var showSettings: (() -> Void)?
    override var mouseDownCanMoveWindow: Bool { true }
    override func mouseDown(with event: NSEvent) {
        if event.clickCount > 1 { showSettings?() } else { window?.performDrag(with: event) }
    }
    override func rightMouseDown(with event: NSEvent) { showSettings?() }
}
@MainActor final class AppDelegate: NSObject, NSApplicationDelegate {
    private let model = QuoteModel()
    private var panel: FloatingPanel!
    private var settingsWindow: NSWindow?
    private var statusItem: NSStatusItem!
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)
        panel = FloatingPanel(contentRect: NSRect(x: 80,y: 120,width: 156,height: 52), styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        panel.isOpaque = false; panel.backgroundColor = .clear; panel.hasShadow = true
        panel.hidesOnDeactivate = false; panel.isMovableByWindowBackground = true
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        let hosting = DraggableHostingView(rootView: OverlayView(model: model))
        hosting.showSettings = { [weak self] in self?.openSettings() }
        panel.contentView = hosting
        panel.setFrameAutosaveName("QuietQuoteOverlay")
        if !panel.setFrameUsingName("QuietQuoteOverlay") { moveToMainScreen() }
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.title = "QQ"
        model.onPreferencesChanged = { [weak self] in self?.applyPreferences() }
        applyPreferences(); panel.orderFrontRegardless(); model.start()
    }
    private func applyPreferences() {
        let p = model.preferences
        panel.setContentSize(NSSize(width: p.circle ? p.diameter : p.width, height: p.circle ? p.diameter : p.height))
        panel.level = p.top ? .floating : .normal
        let menu = NSMenu()
        func item(_ zh: String,_ en: String,_ action: Selector,_ key: String = "") {
            let i = NSMenuItem(title: model.t(zh,en), action: action, keyEquivalent: key); i.target = self; menu.addItem(i)
        }
        item("显示 / 隐藏", "Show / hide", #selector(toggle))
        item("设置…", "Settings…", #selector(openSettings), ",")
        item("立即刷新", "Refresh now", #selector(refresh))
        item("移回主屏幕", "Move to main screen", #selector(moveToMainScreen))
        menu.addItem(.separator()); item("退出", "Quit", #selector(quit), "q")
        statusItem.menu = menu
        settingsWindow?.title = model.t("QuietQuote 设置（预览）", "QuietQuote Settings (Preview)")
    }
    @objc private func toggle() { if panel.isVisible { panel.orderOut(nil) } else { panel.orderFrontRegardless() } }
    @objc private func refresh() { Task { await model.refresh() } }
    @objc private func openSettings() {
        if settingsWindow == nil {
            let w = NSWindow(contentRect: NSRect(x: 0,y: 0,width: 440,height: 640), styleMask: [.titled,.closable], backing: .buffered, defer: false)
            w.isReleasedWhenClosed = false; w.contentView = NSHostingView(rootView: SettingsView(model: model)); w.center(); settingsWindow = w
        }
        settingsWindow?.title = model.t("QuietQuote 设置（预览）", "QuietQuote Settings (Preview)")
        NSApp.activate(ignoringOtherApps: true); settingsWindow?.makeKeyAndOrderFront(nil)
    }
    @objc private func moveToMainScreen() {
        guard let screen = NSScreen.main else { return }
        let r = screen.visibleFrame; panel.setFrameOrigin(NSPoint(x: r.maxX - panel.frame.width - 24,y: r.minY + 24))
    }
    @objc private func quit() { NSApp.terminate(nil) }
}
@main struct Launcher {
    @MainActor static func main() {
        let app = NSApplication.shared
        let delegate = AppDelegate(); app.delegate = delegate
        withExtendedLifetime(delegate) { app.run() }
    }
}
