#!/usr/bin/env bash
set -e

PI="josh@192.168.0.18"
REMOTE="/home/josh/iot1"

echo "==> Syncing source to ${PI}:${REMOTE} ..."
rsync -avz --exclude 'rust/target/' --exclude 'csharp/bin/' --exclude 'csharp/obj/' \
  /Users/josh/Dev/iot-project-1/ \
  "${PI}:${REMOTE}/"

echo "==> Building libepd.so on Pi ..."
ssh "$PI" "bash -l -c '
  cd ${REMOTE}/c && gcc -shared -fPIC -O2 \
    -DUSE_LGPIO_LIB -DRPI \
    -I lib/Config -I lib/GUI -I lib/e-Paper -I lib/Fonts \
    lib/Config/DEV_Config.c \
    lib/Config/dev_hardware_SPI.c \
    lib/GUI/GUI_Paint.c \
    lib/Fonts/font8.c lib/Fonts/font12.c lib/Fonts/font16.c \
    lib/Fonts/font20.c lib/Fonts/font24.c \
    lib/e-Paper/EPD_2in13_V4.c \
    lib/epd_helpers.c \
    -llgpio -lm \
    -o libepd.so
'"

echo "==> Building and running C# app ..."
ssh "$PI" "bash -l -c '
  cd ${REMOTE}/csharp && LD_LIBRARY_PATH=${REMOTE}/c dotnet run
'"
