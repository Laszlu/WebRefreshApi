#!/usr/bin/env bash
set -euo pipefail

COMMIT_HASH=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

cat > "$(dirname "$0")/../version.json" <<EOF
{
  "commitHash": "${COMMIT_HASH}",
  "buildDate": "${BUILD_DATE}"
}
EOF