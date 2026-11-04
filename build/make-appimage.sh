#!/bin/bash
# Builds LinuxPartsFinder.AppImage from a "dotnet publish" folder. Runs on Linux (GitHub Actions Ubuntu runner).
#
#   build/make-appimage.sh <publish folder> <arch: x86_64 or aarch64> <version, e.g. 1.0.0> <output .AppImage path>
#
# An AppImage is one file holding a folder (the AppDir): AppRun (starts the program), a .desktop launcher entry and
# icon for the desktop's app menu, and the program itself in usr/bin. Both arches are built on an x64 runner: the
# AppImage runtime for the target arch is downloaded and put on the front.
set -euo pipefail

PUBLISH_DIR="$1"
ARCH="$2"
VERSION="$3"
OUTPUT="$4"
HERE="$(cd "$(dirname "$0")" && pwd)"
WORK="$(mktemp -d)"
APPDIR="$WORK/LinuxPartsFinder.AppDir"

mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
cp -R "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"
chmod +x "$APPDIR/usr/bin/LinuxPartsFinder"

cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/LinuxPartsFinder" "$@"
EOF
chmod +x "$APPDIR/AppRun"

cat > "$APPDIR/linuxpartsfinder.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Linux-PartsFinder
Comment=Search electronic component suppliers at once and compare stock and prices
Exec=LinuxPartsFinder
Icon=linuxpartsfinder
Categories=Development;Electronics;
Terminal=false
X-AppImage-Version=${VERSION}
EOF
cp "$APPDIR/linuxpartsfinder.desktop" "$APPDIR/usr/share/applications/"

# Icon: build/AppIcon.png (256 x 256, from WinPartsFinder's icon)
cp "$HERE/AppIcon.png" "$APPDIR/linuxpartsfinder.png"
cp "$HERE/AppIcon.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/linuxpartsfinder.png"
ln -s linuxpartsfinder.png "$APPDIR/.DirIcon"

# appimagetool (x64, run without FUSE) and the runtime for the target arch
curl -fsSL -o "$WORK/appimagetool" https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
curl -fsSL -o "$WORK/runtime" "https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-${ARCH}"
chmod +x "$WORK/appimagetool"
APPIMAGE_EXTRACT_AND_RUN=1 ARCH="$ARCH" VERSION="$VERSION" \
    "$WORK/appimagetool" --no-appstream --runtime-file "$WORK/runtime" "$APPDIR" "$OUTPUT"

echo "Built $OUTPUT (version $VERSION, $ARCH)"
