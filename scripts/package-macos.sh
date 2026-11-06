#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(tr -d '\r\n' < "$ROOT/version.txt")"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid version.txt" >&2; exit 1; }
[[ "$(uname -s)" == Darwin ]] || { echo "This build requires macOS and Xcode Command Line Tools." >&2; exit 1; }
cd "$ROOT/macos"
swift test
swift build -c release
BIN="$(swift build -c release --show-bin-path)"
ARCH="$(uname -m)"
mkdir -p "$ROOT/.build" "$ROOT/dist"
STAGE="$(mktemp -d "$ROOT/.build/macos-package.XXXXXX")"
APP="$STAGE/QuietQuote.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN/QuietQuoteMac" "$APP/Contents/MacOS/QuietQuoteMac"
chmod +x "$APP/Contents/MacOS/QuietQuoteMac"
cat > "$APP/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>QuietQuote</string>
<key>CFBundleDisplayName</key><string>QuietQuote Preview</string>
<key>CFBundleIdentifier</key><string>dev.quietquote.desktop</string>
<key>CFBundleExecutable</key><string>QuietQuoteMac</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>LSUIElement</key><true/>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
EOF
plutil -lint "$APP/Contents/Info.plist"
cp "$ROOT/LICENSE" "$APP/Contents/Resources/LICENSE"
cp "$ROOT/macos/README.md" "$APP/Contents/Resources/README.md"
codesign --force --sign - "$APP"
codesign --verify --strict "$APP"
cp "$ROOT/LICENSE" "$STAGE/LICENSE"
cp "$ROOT/macos/README.md" "$STAGE/README.md"
FILE="QuietQuote-v$VERSION-macos-$ARCH-preview.zip"
ditto -c -k --sequesterRsrc --keepParent "$STAGE/QuietQuote.app" "$ROOT/dist/$FILE"
# Companion notices are also uploaded separately by the workflow.
cp "$ROOT/macos/README.md" "$ROOT/dist/MACOS-PREVIEW.md"
cp "$ROOT/LICENSE" "$ROOT/dist/MACOS-LICENSE.txt"
(cd "$ROOT/dist" && shasum -a 256 "$FILE" > "SHA256SUMS-macos-$ARCH.txt")
echo "Built preview: $ROOT/dist/$FILE"
