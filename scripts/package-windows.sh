#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$(mktemp -d)"
STAGE_DIR="$(mktemp -d)"
trap 'rm -rf "$PUBLISH_DIR" "$STAGE_DIR"' EXIT

echo "Publishing BarengIn.Desktop for win-x64..."
dotnet publish "$ROOT/src/BarengIn.Desktop" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$PUBLISH_DIR"

cp "$PUBLISH_DIR/BarengIn.Desktop.exe" "$STAGE_DIR/BarengIn.Desktop.exe"
cp "$PUBLISH_DIR/appsettings.json" "$STAGE_DIR/appsettings.json"
cp "$ROOT/docs/database/schema.sql" "$STAGE_DIR/schema.sql"
cp "$ROOT/scripts/README-RUN.txt" "$STAGE_DIR/README-RUN.txt"

mkdir -p "$ROOT/dist"
OUT_ZIP="$ROOT/dist/BarengIn-MVP-win-x64.zip"
rm -f "$OUT_ZIP"

(
  cd "$STAGE_DIR"
  zip -q -r "$OUT_ZIP" BarengIn.Desktop.exe appsettings.json schema.sql README-RUN.txt
)

echo "Created bundle:"
ls -lh "$OUT_ZIP"
unzip -l "$OUT_ZIP"
