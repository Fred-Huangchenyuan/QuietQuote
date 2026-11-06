# [⬇ Download / 下载](https://github.com/Fred-Huangchenyuan/QuietQuote/releases)

Choose the Windows ZIP under **Assets** on the Releases page. The macOS port is an experimental preview; see its status below.

# QuietQuote

**Repository includes:** tested Windows source plus an **experimental native macOS port**. The Mac port has not yet been compiled or run in this Windows environment; its separate CI workflows must pass before an app is available. See [macOS preview](macos/README.md) and [simple Git upload instructions](UPLOAD.zh-CN.md).

A small, bilingual Windows desktop ticker for OKX perpetual futures. A draggable capsule with seven-segment digits, optional glass blur, and a background-aware light/dark theme. No API key required.

[简体中文](README.zh-CN.md) · [Build and publish](docs/LAUNCH.zh-CN.md) · [Release notes](docs/releases/v1.3.0.md)

| Light | Dark |
| --- | --- |
| ![Light capsule](docs/images/light.png) | ![Dark capsule](docs/images/dark.png) |

Previews show example prices, not live quotes.

## Download and run

Open the [**Releases download page**](https://github.com/Fred-Huangchenyuan/QuietQuote/releases) and download `QuietQuote-v1.3.0-windows.zip` from **Assets**. Extract it into a writable folder and run `QuietQuote.exe`. Exit a previous instance from its tray menu before upgrading. You can copy your old `settings.json` into the new folder.

Windows 10/11 with .NET Framework 4.8 is the recommended environment. No installer, administrator privileges, exchange credentials, or separate font installation is needed. Builds are currently unsigned.

## Features

- OKX perpetual instruments and their actual quote currencies; default `KORU-USDT-SWAP`.
- Refresh interval of 1–3600 seconds, measured after each request completes.
- Capsule or circle; adjustable dimensions, background opacity, digit size and always-on-top behavior.
- Seven-segment digits, rendered directly without a font dependency.
- Glass blur, or non-glass Auto / Light / Dark themes.
- Chinese and English settings, tray menus, status labels and tooltips.
- Stale-data and connection indicators, automatic retries, and protection against displaying the previous instrument's response after a switch.

## Controls

| Action | Control |
| --- | --- |
| Move | Left-button drag |
| Settings | Right-click menu or double-click |
| Hide / restore | `Ctrl+Alt+Q` or double-click the tray icon |
| Recover offscreen window | Tray menu → Move to primary screen |
| Quit | Tray menu → Exit |

To enable adaptive colors, open Settings, disable **Glass blur**, and select **Auto background**. Settings language changes immediately; save to apply it to the main window and menus.

## Data and privacy

Market data is fetched directly from OKX's public HTTPS endpoints:

- `/api/v5/public/instruments?instType=SWAP`
- `/api/v5/market/ticker?instId=...`

The app shows the **last traded price**, not the mark or index price. Changing quote currency selects a different actual contract; it is not a currency conversion. Contract availability and connectivity depend on OKX and the user's network/region. System proxy settings are used.

Preferences and the instrument cache are stored beside the executable in `settings.json` and `markets.json`. Auto theme samples up to 16 nearby pixel colors locally once per second while the overlay is visible and non-glass Auto mode is selected. It does not save screenshots or upload screen contents. There is no telemetry, account access, trading, or auto-start installation.

## Build, test, package

Run in a Windows PowerShell terminal from the repository root. The scripts use the Windows .NET Framework C# compiler; no NuGet restore or third-party application dependencies are needed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Build output: `.build/app/QuietQuote.exe`. Release output: `dist/QuietQuote-v1.3.0-windows.zip` and `dist/SHA256SUMS.txt`.

`test.ps1` runs offline core tests in an isolated temporary build directory. Optional `-Desktop` tests open test windows, exercise the native renderer and sample controlled black/white backgrounds; the settings dialog may attempt a public catalog refresh. `-Live` also performs live OKX quote tests. Desktop/live tests require an unlocked interactive Windows session and are intentionally excluded from CI.

In VS Code, `Ctrl+Shift+B` builds. **Terminal → Run Task → Package QuietQuote** makes release assets. GitHub Actions runs core tests and packaging for pushes and pull requests; it uploads an Actions artifact but does not automatically publish a GitHub Release.

## Known limits

- Auto colors estimate the nearby background, not pixels directly hidden by the overlay. Mixed backgrounds may benefit from a fixed theme.
- Native glass uses Windows composition APIs whose visual behavior varies by Windows build and transparency settings. API acceptance has been tested; final compositor blur appearance has not been fully visually verified. A translucent appearance is used when activation is unavailable.
- High DPI, multi-monitor changes, Remote Desktop, and every listed market have not been exhaustively tested.
- The global hotkey may be occupied by another application; the tray remains available.
- No update service, installer or Developer ID/code-signing certificate is included. The macOS source is an unverified experimental port; Linux is not supported.

## Contributing and license

See [CONTRIBUTING.md](CONTRIBUTING.md). MIT licensed; see [LICENSE](LICENSE). QuietQuote is an independent project and is not affiliated with or endorsed by OKX.
