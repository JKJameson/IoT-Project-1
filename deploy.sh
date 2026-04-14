#!/usr/bin/env bash
set -e

PI="josh@192.168.0.18"
REMOTE="/home/josh/iot1"

echo "==> Publishing C# app for linux-arm64 ..."
dotnet publish /Users/josh/Dev/iot-project-1/csharp/EpdApp.csproj \
  -r linux-arm64 --self-contained -c Release \
  -o /Users/josh/Dev/iot-project-1/csharp/bin/pi \
  /p:PublishSingleFile=true \
  --nologo -v quiet

echo "==> Deploying binary to Pi ..."
rsync -az --progress /Users/josh/Dev/iot-project-1/csharp/bin/pi/EpdApp "${PI}:${REMOTE}/csharp/EpdApp.tmp"
ssh "$PI" "mv -f ${REMOTE}/csharp/EpdApp.tmp ${REMOTE}/csharp/EpdApp"

echo "==> Installing user service ..."
scp /Users/josh/Dev/iot-project-1/csharp/epdapp.service "${PI}:/tmp/epdapp.service"
ssh "$PI" bash -s <<'REMOTE'
mkdir -p ~/.config/systemd/user
mv -f /tmp/epdapp.service ~/.config/systemd/user/epdapp.service
systemctl --user daemon-reload
systemctl --user enable epdapp.service
systemctl --user restart epdapp.service
systemctl --user status epdapp.service
REMOTE
