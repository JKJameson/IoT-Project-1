#!/usr/bin/env bash
set -e

PI="josh@pi1"
REMOTE="/home/josh/iot1"

echo "==> Syncing source to ${PI}:${REMOTE} ..."
rsync -avz --exclude 'rust/target/' \
  /Users/josh/Dev/iot-project-1/ \
  "${PI}:${REMOTE}/"

echo "==> Building on Pi ..."
ssh "$PI" "bash -l -c 'cd ${REMOTE}/rust && cargo build --release' 2>&1"

echo "==> Done. Binary at ${REMOTE}/rust/target/release/epd-app"
ssh "$PI" "/home/josh/iot1/rust/target/release/epd-app"
