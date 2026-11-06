# Windows v1.4.0 validation — 2026-11-06

- Core suite: 48 offline checks covering decimal prices, instrument/market identity, invalid responses, source routing, catalog filtering, timestamp handling, v1.3 migration and independent window persistence.
- Desktop suite: 68 checks covering native rendering, EN/ZH settings, controlled background sampling, multiple overlays, hide/restore, resize of the window collection, and deterministic HTTP race/error scenarios. Test files and preferences live in isolated .build/tests directories.
- Four-market live suite: the app's own Feed client successfully loaded catalogs and fetched fresh BTC quotes for OKX spot/perpetual and Binance spot/USDⓈ-M perpetual. Observed catalog sizes: 1210, 543, 1366 and 555 respectively; these counts change over time.
- Binance spot full catalog exceeded the buffer limit when including permission sets. The final implementation requests showPermissionSets=false and symbolStatus=TRADING; the corrected request passed live validation with the bounded buffer intact.
- Settings form renderings inspected in Chinese and English. Glass compositor appearance, all DPI/monitor combinations, every instrument, and regional network restrictions are not exhaustively verified.

Binance spot price tickers lack a trade timestamp. The UI labels local receipt time explicitly; this is not proof of recent trading activity.

macOS sources are unchanged in feature scope: one OKX perpetual overlay. No Swift/macOS toolchain is available in this Windows environment; no Mac compilation, execution, signing or new-feature parity is claimed.

Build scripts produce an unsigned executable and Windows ZIP/checksum. This local update does not itself push commits, create a GitHub tag, run remote CI or publish a Release.
