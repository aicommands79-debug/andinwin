#!/usr/bin/env bash
# AndinWin WSL bridge - Ubuntu-24.04 icinde root/user olarak calisir
# Kullanim: andinwin-bridge.sh [status|init|session-start|session-stop|install <apk>|launch <pkg>|list|enable-multiwindow]
set -euo pipefail

CMD="${1:-status}"

log() { echo "[andinwin] $*" >&2; }

case "$CMD" in
  status)
    echo "--- systemd ---"
    systemctl is-system-running 2>&1 || true
    echo "--- binder ---"
    ls /dev/binder* 2>&1 || echo "no-binder"
    echo "--- waydroid-status ---"
    waydroid status 2>&1 || true
    echo "--- multiwindow-prop ---"
    waydroid prop get persist.waydroid.multi_windows 2>&1 || true
    ;;
  enable-multiwindow)
    waydroid prop set persist.waydroid.multi_windows true
    log "multi_windows=true yapildi"
    ;;
  session-start)
    waydroid session start &
    sleep 2
    waydroid status || true
    ;;
  session-stop)
    waydroid session stop || true
    ;;
  container-start)
    sudo systemctl start waydroid-container || sudo service waydroid-container start || true
    ;;
  container-stop)
    sudo systemctl stop waydroid-container || true
    ;;
  list)
    # Makinece okunabilir liste: her satir bir paket adi
    waydroid app list 2>/dev/null || waydroid app list 2>&1
    ;;
  install)
    APK="${2:?apk yolu gerekli}"
    waydroid app install "$APK"
    ;;
  launch)
    PKG="${2:?paket adi gerekli}"
    # Session kapaliysa once baslat
    if ! waydroid status 2>&1 | grep -qi "Session.*RUNNING"; then
      log "session kapali, baslatiliyor..."
      waydroid session start &
      sleep 3
    fi
    waydroid app launch "$PKG"
    ;;
  icons)
    # .desktop + ikon yollari (Windows tarafi \\wsl$\ uzerinden kopyalar)
    ls -1 "$HOME/.local/share/applications/waydroid."*.desktop 2>/dev/null || true
    echo "---"
    ls -1 "$HOME/.local/share/icons/hicolor/"*/apps/waydroid.*.png 2>/dev/null || true
    ;;
  *)
    echo "bilinmeyen komut: $CMD" >&2
    echo "kullanim: $0 [status|init|session-start|session-stop|list|install <apk>|launch <pkg>|enable-multiwindow|icons]" >&2
    exit 2
    ;;
esac
