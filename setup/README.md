# Setup / Dagitim taslagi (Faz-5)

Hedef: MSIX/Installer + GitHub Release + WinGet birlikte.

1. WiX v5 ile MSI:
   - `AndinWin.exe`, `AndinWin.Launcher.exe`, `AndinWin.Core.dll` -> `ProgramFiles/AndinWin`
   - Dosya iliskisi: `.apk` -> `AndinWin.exe "%1"` + `Launcher.exe install "%1"`
   - StartMenu klasoru: `AndinWin`
2. MSIX sarma (opsiyonel Store disi): `MakeMsix` ile ayni payload.
3. Oto-update: Velopack (`vpk pack -u AndinWin -v 0.1.0 -p win`) + GitHub Release'e `Releases.nupkg` yukle.
4. WinGet: `wingetcreate new https://github.com/<org>/andinwin/releases/download/v0.1.0/AndinWin-Setup.msi` ile manifest PR.

Kod imzasizsa SmartScreen uyarir - ilk surumde README'ye not dus.
