#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="$ROOT_DIR/build"
APP="$BUILD_DIR/Focusberry.app"
DMG="$BUILD_DIR/Focusberry.dmg"

cd "$ROOT_DIR"

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "Error: macOS is required to build the Focusberry macOS app."
  exit 1
fi

if ! command -v swiftc >/dev/null 2>&1; then
  echo "Error: swiftc was not found. Install Xcode or the Xcode Command Line Tools."
  exit 1
fi

rm -rf "$BUILD_DIR"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

xcrun swiftc \
  macos/Sources/FocusberryMacOS/main.swift \
  -o "$APP/Contents/MacOS/Focusberry" \
  -framework SwiftUI \
  -framework WebKit \
  -target arm64-apple-macos13.0

cp index.html "$APP/Contents/Resources/index.html"
cp manifest.json "$APP/Contents/Resources/manifest.json"
cp sw.js "$APP/Contents/Resources/sw.js"
cp assets/focusberry-icon.png "$APP/Contents/Resources/focusberry-icon.png"

cat > "$APP/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDisplayName</key>
    <string>Focusberry</string>
    <key>CFBundleExecutable</key>
    <string>Focusberry</string>
    <key>CFBundleIconFile</key>
    <string>focusberry-icon.png</string>
    <key>CFBundleIdentifier</key>
    <string>com.amiralisa5.focusberry</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Focusberry</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>0.1.0</string>
    <key>CFBundleVersion</key>
    <string>3</string>
    <key>LSMinimumSystemVersion</key>
    <string>13.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

chmod +x "$APP/Contents/MacOS/Focusberry"
codesign --deep --force --sign - "$APP"

mkdir -p "$BUILD_DIR/dmg-root"
cp -R "$APP" "$BUILD_DIR/dmg-root/Focusberry.app"
ln -s /Applications "$BUILD_DIR/dmg-root/Applications"
hdiutil create -volname Focusberry -srcfolder "$BUILD_DIR/dmg-root" -ov -format UDZO "$DMG" >/dev/null

codesign --verify --deep --strict "$APP"
test -f "$DMG"

echo
echo "Focusberry macOS build complete."
echo "App: $APP"
echo "DMG: $DMG"
echo
echo "To launch: open \"$APP\""
echo "To install: open \"$DMG\""
