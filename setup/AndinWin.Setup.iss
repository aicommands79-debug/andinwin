; AndinWin Inno Setup (Faz-5 MVP) - MSIX'e gecmeden once pratik installer
; Kullanim: iscc setup/AndinWin.Setup.iss
#define MyAppName "AndinWin"
#define MyAppVersion "0.1.0"
#define MyAppExe "AndinWin.exe"

[Setup]
AppId={{3A1B2C4D-ANDI-NWIN-0001-000000000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\AndinWin
DefaultGroupName=AndinWin
OutputBaseFilename=AndinWin-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "src\AndinWin.App\bin\Release\net8.0-windows\AndinWin.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\AndinWin.App\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\AndinWin.Launcher\bin\Release\net8.0\AndinWin.Launcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "wsl\andinwin-bridge.sh"; DestDir: "{app}\wsl"; Flags: ignoreversion

[Icons]
Name: "{group}\AndinWin"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\AndinWin"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Registry]
; .apk cift-tik iliskisi
Root: HKCU; Subkey: "Software\Classes\.apk"; ValueType: string; ValueName: ""; ValueData: "AndinWin.apk"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AndinWin.apk\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AndinWin.Launcher.exe"" install ""%1"""

[Tasks]
Name: desktopicon; Description: "Masaustu kisayolu olustur"

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "AndinWin'i baslat"; Flags: nowait postinstall skipifsilent
