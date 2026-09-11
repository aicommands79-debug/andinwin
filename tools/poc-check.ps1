# AndinWin Faz-0 POC kontrol scripti
# Kullanim: powershell -ExecutionPolicy Bypass -File tools/poc-check.ps1 [-Distro Ubuntu-24.04]
param([string]$Distro = "Ubuntu-24.04")

$ErrorActionPreference = "Continue"
$ok = $true
function Step([string]$name, [scriptblock]$fn) {
  Write-Host "`n== $name ==" -ForegroundColor Cyan
  try { & $fn } catch { Write-Host "HATA: $_" -ForegroundColor Red; $script:ok = $false }
}
function RunOut([string]$exe, [string[]]$argv) {
  $p = Start-Process -FilePath $exe -ArgumentList $argv -NoNewWindow -Wait -PassThru -RedirectStandardOutput "$env:TEMP\andinwin-out.txt" -RedirectStandardError "$env:TEMP\andinwin-err.txt"
  $out = Get-Content "$env:TEMP\andinwin-out.txt" -Raw -ErrorAction SilentlyContinue
  $err = Get-Content "$env:TEMP\andinwin-err.txt" -Raw -ErrorAction SilentlyContinue
  if ($null -eq $out) { $out = "" }
  if ($null -eq $err) { $err = "" }
  return @{ Code = $p.ExitCode; Out = $out; Err = $err }
}

Step "1. wsl.exe mevcut mu" {
  $r = RunOut "wsl.exe" @("--status")
  Write-Host $r.Out
  if ($r.Out -match "kali-linux" -or $r.Code -eq 0) { Write-Host "OK: WSL kurulu" -ForegroundColor Green }
  else { Write-Host "UYARI: wsl --status sifir disi dondu" -ForegroundColor Yellow; $script:ok = $false }
}

Step "2. Dagitim listesi ($Distro bekleniyor)" {
  $r = RunOut "wsl.exe" @("--list", "--verbose")
  Write-Host $r.Out
  if ($r.Out -match [regex]::Escape($Distro)) { Write-Host "OK: $Distro bulundu" -ForegroundColor Green }
  else { Write-Host "EKSIK: $Distro yok. Faz-4 wizard bunu kuracak. Simdilik: wsl --install -d Ubuntu-24.04" -ForegroundColor Yellow }
}

Step "3. Dagitim icinde systemd + binder + waydroid kontrolu" {
  $r = RunOut "wsl.exe" @("-d", $Distro, "-e", "sh", "-c", "systemctl is-system-running 2>&1; echo ---; ls /dev/binder* 2>&1; echo ---; which waydroid 2>&1; waydroid status 2>&1 | head -n 30")
  Write-Host $r.Out
  Write-Host $r.Err
  if ($r.Out -match "waydroid") { Write-Host "Bilgi: waydroid ciktisi alindi" -ForegroundColor Green }
  else { Write-Host "Bilgi: Waydroid henuz kurulu degil (Faz-4'te kurulacak). Sorun degil." -ForegroundColor Yellow }
}

Step "4. WSLg / Wayland soketi" {
  $r = RunOut "wsl.exe" @("-d", $Distro, "-e", "sh", "-c", "echo WAYLAND_DISPLAY=$WAYLAND_DISPLAY; ls -l /mnt/wslg/runtime-dir/wayland-0* 2>&1 | head; ls /tmp/.X11-unix 2>&1 | head")
  Write-Host $r.Out
}

Step "5. waydroid app komutlari (kuruluysa)" {
  $r = RunOut "wsl.exe" @("-d", $Distro, "-e", "waydroid", "app", "--help")
  Write-Host ($r.Out + $r.Err)
}

Step "6. Ikon kaynagi (desktop + png)" {
  $r = RunOut "wsl.exe" @("-d", $Distro, "-e", "sh", "-c", "ls ~/.local/share/applications/waydroid.*.desktop 2>&1 | head; echo ---; ls ~/.local/share/icons/hicolor/*/apps/waydroid.*.png 2>&1 | head")
  Write-Host $r.Out
}

Write-Host "`n================ POC SONUC ================" -ForegroundColor Cyan
if ($ok) { Write-Host "POC kontrolleri calisti. Ciktilari Faz-0 kabul kriterleriyle karsilastir." -ForegroundColor Green }
else { Write-Host "Bazi adimlar basarisiz. Ciktiya bak." -ForegroundColor Yellow }
