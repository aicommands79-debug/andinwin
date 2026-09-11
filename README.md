# AndinWin - Windows Android Koprusu (WSL2 + Waydroid)

WSL2/Ubuntu-24.04 icindeki Waydroid uygulamalarini Windows'ta native gibi calistir.

## Ozellikler (gerceklenen)
- **App listesi:** `waydroid app list` -> WPF liste, JSON cache ile hizli acilis
- **APK kurulum:** pencereye surukle-birak veya Dosya->Sec; `waydroid app install`
- **Baslat:** cift-tik -> konsolsuz `AndinWin.Launcher.exe launch <paket>`
- **Kisayol:** Baslat Menusu `AndinWin Apps/*.lnk` + WSL ikonundan `.ico` (PNG-gomulu ICO, paketsiz)
- **Multi-window:** `persist.waydroid.multi_windows=true` (kurulumda otomatik)
- **Tray:** WinForms NotifyIcon, 5sn RAM/CPU (`free -m`/`loadavg`), Session Baslat/Durdur, `wsl --shutdown`
- **Kurulum sihirbazi:** Kurulum menusunden: WSL kontrol -> Ubuntu-24.04 -> systemd -> Waydroid apt -> `init -s GAPPS` -> multi-window

## Hizli baslangic
```powershell
powershell -ExecutionPolicy Bypass -File tools/poc-check.ps1
dotnet build AndinWin.slnx -c Release
dotnet test tests/AndinWin.Core.Tests/AndinWin.Core.Tests.csproj
# Calistir: src/AndinWin.App/bin/Release/net8.0-windows/AndinWin.exe
```

## Mimari
- `src/AndinWin.Core`: `WslRunner`, `WaydroidClient`, `WaydroidParsers`, `AppCache`, `WslManager`, `ResourceMonitor`, `IconPipeline`, `SetupService`
- `src/AndinWin.Launcher` (WinExe): kisayol hedefi
- `src/AndinWin.App` (WPF+WinForms tray): `MainWindow`, `SetupWizardWindow`, `Services/TrayIconManager`, `ShortcutManager`
- `wsl/andinwin-bridge.sh`, `tools/poc-check.ps1`
- `setup/AndinWin.Setup.iss` (Inno), `manifests/` (WinGet)

## Bilinen kisitlar
- Sabit distro `Ubuntu-24.04`. `/dev/binder` yoksa custom WSL kernel gerekir.
- GAPPS sonrasi sertifika adimi gerekir; banka/Play Integrity uygulamalari acilmayabilir.
- Imzasiz installer -> SmartScreen uyarir.
