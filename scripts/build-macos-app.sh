#!/usr/bin/env bash
#
# Builds a self-contained macOS .app bundle (and a distributable .zip) for the
# Avalonia GUI. Run on macOS.
#
# Usage:
#   scripts/build-macos-app.sh --version v2.3.0 [--arch arm64|x64|both] [--config Release]
#
# Produces, under scripts/output/:
#   OpenLCE-Converter-<version>-osx-arm64-app.zip   (contains "OpenLCE Converter.app")
#   OpenLCE-Converter-<version>-osx-x64-app.zip
#
# Notes:
#   - The bundle is unsigned. On first launch macOS Gatekeeper will block it;
#     right-click the app -> Open (or `xattr -dr com.apple.quarantine "<app>"`).
#   - Code signing / notarization (requires an Apple Developer ID) is left as a
#     follow-up; see the TODO at the bottom of this script.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GUI_PROJECT="$REPO_ROOT/LceWorldConverter.Gui/LceWorldConverter.Gui.csproj"
OUTPUT_DIR="$SCRIPT_DIR/output"
ICON_SOURCE="$REPO_ROOT/Resources/AppIcon.icns"

APP_DISPLAY_NAME="OpenLCE Converter"
BUNDLE_ID="com.banditvault.openlceconverter"
EXE_NAME="LceWorldConverter.Gui"

VERSION=""
ARCH_ARG="both"
CONFIG="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) VERSION="${2:-}"; shift 2 ;;
    --arch)    ARCH_ARG="${2:-}"; shift 2 ;;
    --config)  CONFIG="${2:-}"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$VERSION" ]]; then
  echo "Missing required --version <tag>. Example: --version v2.3.0" >&2
  exit 1
fi

# Normalize version: VERSION_LABEL keeps a leading 'v'; SHORT_VERSION is numeric.
VERSION_LABEL="v${VERSION#v}"
SHORT_VERSION="${VERSION_LABEL#v}"
SHORT_VERSION="${SHORT_VERSION%%[-+]*}"
if [[ ! "$SHORT_VERSION" =~ ^[0-9]+(\.[0-9]+){0,3}$ ]]; then
  echo "Version '$SHORT_VERSION' must be 1 to 4 numeric components (e.g. 2.3.0)." >&2
  exit 1
fi

case "$ARCH_ARG" in
  arm64) RUNTIMES=("osx-arm64") ;;
  x64)   RUNTIMES=("osx-x64") ;;
  both)  RUNTIMES=("osx-arm64" "osx-x64") ;;
  *) echo "Invalid --arch '$ARCH_ARG' (use arm64, x64, or both)." >&2; exit 1 ;;
esac

mkdir -p "$OUTPUT_DIR"

build_one() {
  local runtime="$1"
  local publish_dir app_dir contents zip_path
  publish_dir="$(mktemp -d)"
  app_dir="$OUTPUT_DIR/$APP_DISPLAY_NAME.app"
  contents="$app_dir/Contents"
  zip_path="$OUTPUT_DIR/OpenLCE-Converter-$VERSION_LABEL-$runtime-app.zip"

  echo ">> Publishing $runtime ($CONFIG) ..."
  dotnet publish "$GUI_PROJECT" \
    -c "$CONFIG" \
    -r "$runtime" \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:Version="$SHORT_VERSION" \
    -p:AssemblyVersion="$SHORT_VERSION.0" \
    -p:FileVersion="$SHORT_VERSION.0" \
    -p:InformationalVersion="$VERSION_LABEL" \
    -o "$publish_dir"

  echo ">> Assembling $app_dir ..."
  rm -rf "$app_dir"
  mkdir -p "$contents/MacOS" "$contents/Resources"
  cp -R "$publish_dir/." "$contents/MacOS/"
  chmod +x "$contents/MacOS/$EXE_NAME"

  local icon_entry=""
  if [[ -f "$ICON_SOURCE" ]]; then
    cp "$ICON_SOURCE" "$contents/Resources/AppIcon.icns"
    icon_entry="    <key>CFBundleIconFile</key>
    <string>AppIcon</string>"
  fi

  cat > "$contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_DISPLAY_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_DISPLAY_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleVersion</key>
    <string>$SHORT_VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$SHORT_VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$EXE_NAME</string>
$icon_entry
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
</dict>
</plist>
PLIST

  echo ">> Zipping $zip_path ..."
  rm -f "$zip_path"
  ditto -c -k --sequesterRsrc --keepParent "$app_dir" "$zip_path"

  rm -rf "$publish_dir"
  echo ">> Done: $zip_path"
}

for runtime in "${RUNTIMES[@]}"; do
  build_one "$runtime"
done

echo ""
echo "Artifacts in $OUTPUT_DIR:"
ls -1 "$OUTPUT_DIR"/OpenLCE-Converter-"$VERSION_LABEL"-osx-*-app.zip 2>/dev/null || true

# TODO (signing/notarization, requires an Apple Developer ID certificate):
#   codesign --deep --force --options runtime --timestamp \
#     --sign "Developer ID Application: <Your Name> (<TEAMID>)" "<app>"
#   xcrun notarytool submit "<zip>" --apple-id <id> --team-id <TEAMID> --password <app-pw> --wait
#   xcrun stapler staple "<app>"
