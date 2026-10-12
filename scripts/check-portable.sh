#!/usr/bin/env bash
# Smoke-test a portable archive from scripts/package-portable.py: verify its
# checksum, extract it to a temporary directory, start the packaged daemon on
# an isolated data directory with clipboard watching off, and drive it through
# the packaged CLI and web UI. Never touches real clipboard history.
#
# Usage: scripts/check-portable.sh path/to/clipwell-VERSION-linux-x64.tar.gz
set -euo pipefail

archive=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
(cd "$(dirname "$archive")" && sha256sum --check --quiet "$(basename "$archive").sha256")

work=$(mktemp -d)
daemon_pid=
cleanup() {
  if [ -n "$daemon_pid" ]; then kill "$daemon_pid" 2>/dev/null || true; wait "$daemon_pid" 2>/dev/null || true; fi
  rm -rf "$work"
}
trap cleanup EXIT

tar -xzf "$archive" -C "$work"
root=$(find "$work" -mindepth 1 -maxdepth 1 -type d)

export CLIPWELL_DATA_DIR="$work/data" CLIPWELL_NO_WATCH=1 CLIPWELL_NO_SWEEP=1
export CLIPWELL_URL=http://127.0.0.1:18787 CLIPWELL_API=http://127.0.0.1:18787

"$root/bin/clipwell" help | grep -q 'clipwell list'

"$root/bin/clipwell-daemon" > "$work/daemon.log" 2>&1 &
daemon_pid=$!
for _ in $(seq 1 100); do
  curl -fsS "$CLIPWELL_URL/health" > /dev/null 2>&1 && break
  kill -0 "$daemon_pid" 2>/dev/null || { cat "$work/daemon.log"; echo "packaged daemon exited" >&2; exit 1; }
  sleep 0.2
done
curl -fsS "$CLIPWELL_URL/health" | grep -q '"status":"ok"'
"$root/bin/clipwell" list | grep -q 'no history yet'
curl -fsSL "$CLIPWELL_URL/app/" | grep -qi '<!doctype html'
echo "portable package OK: $(basename "$archive")"
