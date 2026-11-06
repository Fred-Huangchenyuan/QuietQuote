# Launch validation — 2026-11-06

The `QuietQuote-GitHub` folder additionally includes experimental macOS sources. No Swift/macOS toolchain is available here: Swift compilation, XCTest execution, app launch, rendering and signing verification are pending on macOS. Workflow preparation is not evidence of a successful Mac build. See `macos/README.md` for the acceptance checklist.

The v1.3.0 application source matches the locally delivered v1.3 feature build. The launch package adds assembly version metadata, repository structure, documentation and build/test/package scripts.

Performed locally on Windows:

- `scripts/package.ps1`: compilation succeeded; 23 offline core checks passed; versioned ZIP and SHA256 checksum generated.
- `scripts/test.ps1 -Desktop`: 38 checks passed, including rendering, language selection, preference persistence and actual pixel sampling against controlled white/black windows.
- The preceding v1.3 iteration also passed live OKX KORU and BTC-USD quote, refresh and instrument-switch checks; these network checks were not repeated solely for repository packaging.

The packaged application is unsigned. GitHub Actions has been configured and checked against current action documentation but has not run in a GitHub repository yet. Native blur API acceptance does not verify the final compositor appearance on every machine. See the README for other compatibility limits.

Neither a remote repository nor a GitHub Release has been created by this preparation step.
