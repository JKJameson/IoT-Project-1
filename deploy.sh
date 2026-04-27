#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCAL_C_LIB="${SCRIPT_DIR}/c/lib"
LOCAL_CSHARP="${SCRIPT_DIR}/csharp"

PI="josh@192.168.0.18"
REMOTE="/home/josh/iot1"

echo "==> Syncing C sources to Pi ..."
ssh "$PI" "rm -rf ${REMOTE}/c/lib && mkdir -p ${REMOTE}/c/lib"
scp -r "${LOCAL_C_LIB}/." "${PI}:${REMOTE}/c/lib/"

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
dotnet publish "${LOCAL_CSHARP}/EpdApp.csproj" \
  -r linux-arm64 --self-contained -c Release \
  -o "${LOCAL_CSHARP}/bin/pi" \
  -p:PublishSingleFile=true \
  --nologo -v quiet

echo "==> Deploying binary to Pi ..."
ssh "$PI" "sudo systemctl stop epdapp.service 2>/dev/null || true"
scp "${LOCAL_CSHARP}/bin/pi/EpdApp" "${PI}:${REMOTE}/csharp/EpdApp.tmp"
ssh "$PI" "mv -f ${REMOTE}/csharp/EpdApp.tmp ${REMOTE}/csharp/EpdApp"
ssh "$PI" "chmod +x ${REMOTE}/csharp/EpdApp"

echo "==> Deploying web files to Pi ..."
ssh "$PI" "mkdir -p ${REMOTE}/csharp/web"
scp "${LOCAL_CSHARP}/web/index.html" "${PI}:${REMOTE}/csharp/web/index.html"

FOLLOW_LOGS="${FOLLOW_LOGS:-0}"

echo "==> Installing system service ..."
scp "${LOCAL_CSHARP}/epdapp.service" "${PI}:/tmp/epdapp.service"
ssh "$PI" "FOLLOW_LOGS=${FOLLOW_LOGS} bash -s" <<'REMOTE'
sudo mv -f /tmp/epdapp.service /etc/systemd/system/epdapp.service
sudo systemctl daemon-reload
sudo systemctl enable epdapp.service
sudo systemctl restart epdapp.service
sudo systemctl status epdapp.service
if [ "${FOLLOW_LOGS}" = "1" ]; then
  sudo journalctl -f -u epdapp.service
else
  sudo journalctl -n 30 --no-pager -u epdapp.service
fi
REMOTE
