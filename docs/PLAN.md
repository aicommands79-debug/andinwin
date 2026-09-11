# AndinWin - Plan (Secimler: .NET 8 WPF, Ubuntu-24.04 sabit, full otomatik kurulum, MSIX+WinGet)

## Faz-0 POC (tamamlandi, dogrulandi)
- `tools/poc-check.ps1` calistirildi: WSL kurulu (kali-linux mevcut), Ubuntu-24.04 yok -> Faz-4 wizard kuracak.
- Kabul: wsl.exe erisimi + `\\wsl$\` ikon yolu varsayimi dogrulandi.

## Faz-1 Core Bridge (tamamlandi)
- `WslRunner`, `WaydroidClient (list/launch/install/status)`, `WaydroidParsers`, `AppCache (JSON)`, `WslManager`, `AndinWin.Launcher (WinExe)`
- Test: `dotnet test` 7/7 yesil. `dotnet build AndinWin.slnx` basarili.

## Faz-2 Entegrasyon (MVP - bu turda iskelet bitti)
- `ShortcutManager`: `%AppData%/.../AndinWin Apps/*.lnk` -> hedef `AndinWin.Launcher.exe launch <pkg>`
- `IconPipeline`: PNG->ICO (bagsiz, PNG-sikistirmali ICO), `FindWslIcon` via `\\wsl$\Ubuntu-24.04`
- S Sonraki: `MainWindow.Shortcut_Click` icinde ikon fetch+convert'i bagla (2 satir), `.apk` dosya iliskisi (registry) installer'da.
- Kabul: Spotify ikonu Baslat menusunde native gibi, cift tik apk yukluyor.

## Faz-3 Tray + Kaynak (sonraki tur)
- `ResourceMonitor.SampleAsync` hazir (free/loadavg). UI: WinForms `NotifyIcon` ile (ek paket yok) + 5sn timer.
- Menu: Durum (RAM/CPU), Session Baslat/Durdur, WSL Kapat, Ana pencereyi Ac.
- Kabul: Tray'den tek tikla `wsl --shutdown`.

## Faz-4 Otomatik kurulum sihirbazi (2 hafta)
Sira: 1) Windows ozellik kontrolu (VirtualMachinePlatform) 2) `wsl --install -d Ubuntu-24.04` 3) `apt install waydroid weston` + `waydroid init -s GAPPS` 4) `systemd=true` yaz + `enable-multiwindow` 5) GAPPS sertifika uyarisi.
- Script: `wsl/andinwin-bridge.sh status|session-start|install|launch|enable-multiwindow` hazir, wizard bunu cagiracak.
- Risk: binder kernel modulu (`/dev/binder` yoksa custom kernel), banka uygulamalari Play Integrity'de acilmayabilir -> F-Droid ile test.

## Faz-5 Dagitim
- WiX v5 MSI (dosya iliskisi `.apk`, StartMenu shortcut) -> MSIX sarma -> Velopack oto-update (GitHub Release) -> `wingetcreate` ile WinGet PR.
- Kod imzasizsa SmartScreen cikar, README'ye yaz.

## Komutlar
```
powershell -ExecutionPolicy Bypass -File tools/poc-check.ps1
dotnet build AndinWin.slnx
dotnet test tests/AndinWin.Core.Tests/AndinWin.Core.Tests.csproj
```
