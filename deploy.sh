#!/usr/bin/env bash
set -e

PI="josh@192.168.0.18"
REMOTE="/home/josh/iot1"

echo "==> Syncing C sources to Pi ..."
rsync -az --delete /Users/josh/Dev/iot-project-1/c/lib/ "${PI}:${REMOTE}/c/lib/"

echo "==> Rebuilding libepd.so on Pi ..."
ssh "$PI" bash -s <<'BUILD'
cd /home/josh/iot1/c
gcc -shared -fPIC -O2 -DUSE_LGPIO_LIB -DRPI \
  -I lib/Config -I lib/GUI -I lib/e-Paper -I lib/Fonts \
  lib/Config/DEV_Config.c lib/Config/dev_hardware_SPI.c \
  lib/e-Paper/EPD_2in13_V4.c \
  lib/GUI/GUI_Paint.c \
  $(find lib/Fonts -name '*.c') \
  lib/epd_helpers.c \
  -o libepd.so -llgpio -lm
BUILD

echo "==> Publishing C# app for linux-arm64 ..."
dotnet publish /Users/josh/Dev/iot-project-1/csharp/EpdApp.csproj \
  -r linux-arm64 --self-contained -c Release \
  -o /Users/josh/Dev/iot-project-1/csharp/bin/pi \
  /p:PublishSingleFile=true \
  --nologo -v quiet

echo "==> Deploying binary to Pi ..."
ssh "$PI" "sudo systemctl stop epdapp.service 2>/dev/null || true"
rsync -az --progress /Users/josh/Dev/iot-project-1/csharp/bin/pi/EpdApp "${PI}:${REMOTE}/csharp/EpdApp.tmp"
ssh "$PI" "mv -f ${REMOTE}/csharp/EpdApp.tmp ${REMOTE}/csharp/EpdApp"

echo "==> Deploying web files to Pi ..."
ssh "$PI" "mkdir -p ${REMOTE}/csharp/web"
rsync -az /Users/josh/Dev/iot-project-1/csharp/web/index.html "${PI}:${REMOTE}/csharp/web/index.html"

echo "==> Installing system service ..."
scp /Users/josh/Dev/iot-project-1/csharp/epdapp.service "${PI}:/tmp/epdapp.service"
ssh "$PI" bash -s <<'REMOTE'
sudo mv -f /tmp/epdapp.service /etc/systemd/system/epdapp.service
sudo systemctl daemon-reload
sudo systemctl enable epdapp.service
sudo systemctl restart epdapp.service
sudo systemctl status epdapp.service
sudo journalctl -f -u epdapp.service
REMOTE
