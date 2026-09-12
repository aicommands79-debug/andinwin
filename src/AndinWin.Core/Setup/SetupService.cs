using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;

namespace AndinWin.Core.Setup;

public enum SetupStep
{
    CheckWsl,
    EnsureDistro,
    EnsureSystemd,
    InstallWaydroid,
    InitWaydroid,
    EnableMultiWindow,
    Done
}

public sealed record SetupProgress(SetupStep Step, string Message);

/// <summary>
/// Faz-4: Full otomatik kurulum. Sabit distro Ubuntu-24.04.
/// Her adim idempotent: tekrar calistirilabilir.
/// </summary>
public sealed class SetupService
{
    private readonly IProcessRunner _proc;
    private readonly AndinWinOptions _opt;
    private readonly WslRunner _wsl;

    public SetupService(IProcessRunner? proc = null, AndinWinOptions? opt = null)
    {
        _opt = opt ?? new AndinWinOptions();
        _proc = proc ?? new ProcessRunner();
        _wsl = new WslRunner(_proc, _opt);
    }

    public async Task RunAllAsync(IProgress<SetupProgress>? progress = null, CancellationToken ct = default)
    {
        await CheckWslAsync(progress, ct);
        await EnsureDistroAsync(progress, ct);
        await EnsureSystemdAsync(progress, ct);
        await InstallWaydroidAsync(progress, ct);
        await InitWaydroidAsync(progress, ct);
        await EnableMultiWindowAsync(progress, ct);
        progress?.Report(new SetupProgress(SetupStep.Done, "Kurulum tamamlandi. WSL yeniden baslatiliyor..."));
        await _proc.RunAsync("wsl.exe", new[] { "--terminate", _opt.DistroName }, 30_000, ct);
    }

    public async Task<bool> IsDistroInstalledAsync(CancellationToken ct = default)
    {
        var r = await _proc.RunAsync("wsl.exe", new[] { "--list", "--quiet" }, 15_000, ct);
        return (r.StdOut + r.StdErr).Split('\n').Any(l => l.Trim().Equals(_opt.DistroName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsWaydroidInitializedAsync(CancellationToken ct = default)
    {
        var r = await _wsl.RunAsync("waydroid status 2>&1", 20_000, ct);
        return !(r.StdOut + r.StdErr).Contains("not initialized", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Waydroid kurulu ama "not initialized" diyen makine icin: sadece init + container + multiwindow.
    /// Android imajlarini indirir (~1 GB, birkac dakika surer).
    /// </summary>
    public async Task InitOnlyAsync(IProgress<SetupProgress>? progress = null, CancellationToken ct = default)
    {
        await InitWaydroidAsync(progress, ct);
        await EnableMultiWindowAsync(progress, ct);
        progress?.Report(new SetupProgress(SetupStep.Done, "Init tamamlandi. WSL yeniden baslatiliyor..."));
        await _proc.RunAsync("wsl.exe", new[] { "--terminate", _opt.DistroName }, 30_000, ct);
    }

    private async Task CheckWslAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        p?.Report(new SetupProgress(SetupStep.CheckWsl, "WSL kontrol ediliyor (wsl --status)..."));
        var r = await _proc.RunAsync("wsl.exe", new[] { "--status" }, 15_000, ct);
        if (!r.Success && string.IsNullOrWhiteSpace(r.StdOut))
            throw new InvalidOperationException("WSL bulunamadi. Windows Ozellikler'den 'Sanal Makine Platformu' + 'Linux Alt Sistemi' acip yeniden deneyin.");
        p?.Report(new SetupProgress(SetupStep.CheckWsl, "WSL mevcut."));
    }

    private async Task EnsureDistroAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        if (await IsDistroInstalledAsync(ct))
        {
            p?.Report(new SetupProgress(SetupStep.EnsureDistro, $"{_opt.DistroName} zaten kurulu."));
            return;
        }
        p?.Report(new SetupProgress(SetupStep.EnsureDistro, $"{_opt.DistroName} kuruluyor (wsl --install, 5-10 dk surebilir)..."));
        var r = await _proc.RunAsync("wsl.exe", new[] { "--install", "-d", _opt.DistroName, "--no-launch" }, 30 * 60_000, ct);
        if (!r.Success && !await IsDistroInstalledAsync(ct))
            throw new InvalidOperationException("Distro kurulamadi: " + (r.StdErr + r.StdOut).Trim());
    }

    private async Task EnsureSystemdAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        p?.Report(new SetupProgress(SetupStep.EnsureSystemd, "systemd etkinlestiriliyor (/etc/wsl.conf)..."));
        // wsl.conf'a [boot] systemd=true yaz (idempotent)
        await _wsl.RunAsync("sh -c 'grep -q \"^systemd=true\" /etc/wsl.conf 2>/dev/null || (echo \"[boot]\" | sudo tee -a /etc/wsl.conf >/dev/null; echo \"systemd=true\" | sudo tee -a /etc/wsl.conf >/dev/null)' 2>&1", 30_000, ct);
    }

    private async Task InstallWaydroidAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        p?.Report(new SetupProgress(SetupStep.InstallWaydroid, "Waydroid paketi kuruluyor (apt)..."));
        const string script = "export DEBIAN_FRONTEND=noninteractive; " +
            "sudo apt update && sudo apt install -y curl ca-certificates python3 lsb-release weston; " +
            "curl -s https://repo.waydro.id | sudo bash; " +
            "sudo apt install -y waydroid; echo WAYDROID_APT_DONE";
        var r = await _wsl.RunAsync(script, 30 * 60_000, ct);
        if (!(r.StdOut + r.StdErr).Contains("WAYDROID_APT_DONE"))
            throw new InvalidOperationException("Waydroid apt kurulumu basarisiz: " + (r.StdOut + r.StdErr)[..Math.Min(2000, (r.StdOut + r.StdErr).Length)]);
    }

    private async Task InitWaydroidAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        p?.Report(new SetupProgress(SetupStep.InitWaydroid, "waydroid init -s GAPPS (imaj indiriliyor, tek seferlik)..."));
        var r = await _wsl.RunAsync("sudo waydroid init -s GAPPS -c https://ota.waydro.id/system -v https://ota.waydro.id/vendor 2>&1 | tail -n 20", 30 * 60_000, ct);
        p?.Report(new SetupProgress(SetupStep.InitWaydroid, (r.StdOut + r.StdErr).Trim()));
    }

    private async Task EnableMultiWindowAsync(IProgress<SetupProgress>? p, CancellationToken ct)
    {
        p?.Report(new SetupProgress(SetupStep.EnableMultiWindow, "multi_windows aciliyor + container baslatiliyor..."));
        await _wsl.RunAsync("sudo systemctl enable --now waydroid-container 2>&1; waydroid prop set persist.waydroid.multi_windows true 2>&1; waydroid session start >/dev/null 2>&1 & sleep 3; waydroid status 2>&1 | head -n 20", 60_000, ct);
    }
}
