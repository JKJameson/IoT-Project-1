#!/usr/bin/env bash
set -e

PI="josh@192.168.0.18"
REMOTE="/home/josh/iot1"

echo "==> Deploying index.html to Pi ..."
ssh "$PI" "mkdir -p ${REMOTE}/csharp/web"
rsync -az /Users/josh/Dev/iot-project-1/csharp/web/index.html "${PI}:${REMOTE}/csharp/web/index.html"
echo "Done."
