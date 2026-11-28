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
if [ -f "$ROOT/docs/database/seed-demo.sql" ]; then
  cp "$ROOT/docs/database/seed-demo.sql" "$STAGE_DIR/seed-demo.sql"
fi
cp "$ROOT/scripts/README-RUN.txt" "$STAGE_DIR/README-RUN.txt"

# If BARENGIN_SUBMISSION_DB is set (or .env.neon exists), inject the live cloud connection string
# into the staged bundle's appsettings.json without modifying the tracked git file.
SUBMISSION_CS="${BARENGIN_SUBMISSION_DB:-}"
if [ -z "$SUBMISSION_CS" ] && [ -f "$ROOT/.env.neon" ]; then
  SUBMISSION_CS="$(grep -v '^[[:space:]]*#' "$ROOT/.env.neon" | sed -n 's/^BARENGIN_DB=//p' | head -n 1 | sed 's/^"//;s/"$//')"
fi
if [ -z "$SUBMISSION_CS" ] && [ -f "$ROOT/.env.local" ]; then
  SUBMISSION_CS="$(grep -v '^[[:space:]]*#' "$ROOT/.env.local" | sed -n 's/^DATABASE_URL=//p' | head -n 1 | sed 's/^"//;s/"$//')"
fi

if [ -n "$SUBMISSION_CS" ]; then
  echo "Injecting live cloud database connection string into staged appsettings.json..."
  python3 -c '
import json, sys
path, cs = sys.argv[1], sys.argv[2]
with open(path, "w", encoding="utf-8") as f:
    json.dump({"ConnectionStrings": {"Default": cs}}, f, indent=2)
    f.write("\n")
' "$STAGE_DIR/appsettings.json" "$SUBMISSION_CS"
fi

mkdir -p "$ROOT/dist"
OUT_ZIP="$ROOT/dist/BarengIn-MVP-win-x64.zip"
rm -f "$OUT_ZIP"

(
  cd "$STAGE_DIR"
  zip -q -r "$OUT_ZIP" BarengIn.Desktop.exe appsettings.json schema.sql README-RUN.txt $( [ -f seed-demo.sql ] && echo seed-demo.sql )
)

echo "Created bundle:"
ls -lh "$OUT_ZIP"
unzip -l "$OUT_ZIP"
