# QuietQuote for macOS — experimental preview

**Status: native source and CI configuration are included. This implementation has not been compiled or run on macOS in the Windows preparation environment. It is not a verified release binary.** First require successful macOS CI builds, then perform the manual acceptance checks below before publishing it to end users.

The Windows version remains the tested release. This macOS implementation uses Swift, SwiftUI and AppKit instead of Windows Forms.

## Implemented in source

- Borderless floating capsule/circle, draggable via its hosting view.
- Seven-segment price digits and optional symbol/currency label.
- Native `NSVisualEffectView` glass with a translucent tint.
- Chinese/English settings, instrument/quote-currency selection and configurable polling.
- Menu-bar `QQ` menu: show/hide, settings, refresh, recover position, quit.
- Right-click or double-click overlay to open settings.
- Quote identity validation, stale detection, backoff, and discarding responses after an instrument switch.
- Preferences in macOS UserDefaults; no API key, no account access.

## Differences from Windows

Auto theme follows **macOS system light/dark appearance**, not pixels around the overlay. There is no screen sampling and no screen-recording permission request. A global keyboard shortcut is not included; use the menu-bar menu. Digit height scales with the overlay layout rather than offering a separate font-size setting. macOS preferences are separate from Windows `settings.json`.

## Build on a Mac

Requires macOS 13+ and a Swift 5.9+ toolchain supplied by a suitable Xcode/Command Line Tools installation. From the repository root:

```bash
bash scripts/package-macos.sh
```

The script runs `swift test`, builds the native executable, creates `QuietQuote.app`, adds an **ad-hoc signature**, and packages an architecture-specific preview ZIP with a SHA256 checksum in `dist/`.

- Apple Silicon: `QuietQuote-v1.3.0-macos-arm64-preview.zip`
- Intel: `QuietQuote-v1.3.0-macos-x86_64-preview.zip`

This is not Developer ID signing or Apple notarization. Downloaded builds may be blocked by Gatekeeper. The project does not disable macOS security settings or remove quarantine attributes. For broad public distribution, configure Developer ID signing and notarization, or have a developer build locally from reviewed source.

## GitHub build

Push `main` to GitHub. The **macOS preview build** workflow runs separately on Apple Silicon and Intel runners. Download the corresponding artifact from the successful workflow run. Artifacts are not automatically published as a Release.

If a job fails, the Mac preview remains unavailable until the reported compiler/test/package error is fixed. No build success is claimed before those jobs execute.

## Manual acceptance before releasing

1. Launch on a real Mac of the target architecture; confirm the QQ menu and overlay appear.
2. Verify the displayed OKX instrument and last price against the exchange.
3. Change coin and quote currency while polling; ensure the old price clears and cannot reappear.
4. Check interval changes, stale state, offline recovery, and quitting while a request is pending.
5. Test dragging, settings via menu/right-click/double-click, hide/show, position recovery, Spaces and full-screen windows.
6. Change system light/dark appearance and the explicit Light/Dark choices, glass, size and language.
7. Relaunch to confirm preferences and position persist. Check scaled Retina and external displays.

This list is pending, not a record of completed tests. MIT licensed; see the bundled LICENSE. Independent project, not endorsed by OKX.
