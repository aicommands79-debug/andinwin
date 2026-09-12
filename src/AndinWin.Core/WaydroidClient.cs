using AndinWin.Core.Config;
using AndinWin.Core.Models;

namespace AndinWin.Core;

/// <summary>Faz-1: Waydroid islemleri icin yuksek seviye API. Konsol cakmasini engellemek icin tumu WslRunner uzerinden.</summary>
public sealed class WaydroidClient
{
    private readonly WslRunner _wsl;
    private readonly AndinWinOptions _opt;

    public WaydroidClient(WslRunner wsl, AndinWinOptions? opt = null)
    {
        _wsl = wsl;
        _opt = opt ?? new AndinWinOptions();
    }

    public async Task<WaydroidStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var r = await _wsl.RunAsync("waydroid status 2>&1; echo ---PROP---; waydroid prop get persist.waydroid.multi_windows 2>&1", 20_000, ct);
        return WaydroidParsers.ParseStatus(r.StdOut + "\n" + r.StdErr);
    }

    public async Task<IReadOnlyList<AndroidApp>> ListAppsAsync(CancellationToken ct = default)
    {
        var r = await _wsl.RunAsync("waydroid app list 2>&1", 30_000, ct);
        var raw = r.StdOut + "\n" + r.StdErr;
        var pkgs = WaydroidParsers.ParseAppList(raw);
        if (pkgs.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Waydroid hic yanit vermedi (bos cikti). Ubuntu dagitimi veya waydroid kurulu olmayabilir. Cozum: Kurulum > Kurulum Sihirbazi.");
            if (WaydroidParsers.LooksLikeNotRunning(raw))
            {
                var snippet = raw.Trim();
                if (snippet.Length > 400) snippet = snippet[..400] + "...";
                throw new InvalidOperationException(
                    $"Waydroid session calismiyor gibi (liste bos). Ham cikti: {snippet} Cozum: Waydroid > Session Baslat'a basin, duzelmezse Tanı'ya bakin.");
            }
        }
        return pkgs.Select(p => new AndroidApp(p, WaydroidParsers.LabelFromPackage(p))).ToList();
    }

    public async Task EnsureSessionAsync(CancellationToken ct = default)
    {
        var st = await GetStatusAsync(ct);
        if (st.SessionRunning) return;
        await _wsl.RunAsync("waydroid session start >/dev/null 2>&1 & sleep 3; waydroid status 2>&1", 30_000, ct);
    }

    public async Task LaunchAsync(string packageName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentException("paket adi bos", nameof(packageName));
        await EnsureSessionAsync(ct);
        var r = await _wsl.RunAsync($"waydroid app launch {Escape(packageName)} 2>&1", 30_000, ct);
        if (!r.Success && !string.IsNullOrWhiteSpace(r.StdErr))
            throw new InvalidOperationException($"launch basarisiz ({packageName}): {r.StdErr.Trim()} {r.StdOut.Trim()}".Trim());
    }

    public async Task InstallApkAsync(string windowsApkPath, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!File.Exists(windowsApkPath)) throw new FileNotFoundException("apk bulunamadi", windowsApkPath);
        progress?.Report("Session kontrol ediliyor...");
        await EnsureSessionAsync(ct);
        Directory.CreateDirectory(_opt.ApkStagingDir);
        // WSL icine \\wsl$\ yerine /mnt/c uzerinden eris: daha guvenilir
        var fileName = Path.GetFileName(windowsApkPath);
        progress?.Report("WSL'e kopyalaniyor...");
        var wslCp = await _wsl.RunAsync($"mkdir -p /tmp/andinwin && cp \"$(wslpath -u {EscapePs(windowsApkPath)})\" /tmp/andinwin/{Escape(fileName)} 2>&1", 120_000, ct);
        if (!wslCp.Success) throw new InvalidOperationException("apk WSL'e kopyalanamadi: " + (wslCp.StdErr + wslCp.StdOut));
        progress?.Report("waydroid app install calisiyor...");
        var inst = await _wsl.RunAsync($"waydroid app install /tmp/andinwin/{Escape(fileName)} 2>&1", 180_000, ct);
        progress?.Report((inst.StdOut + inst.StdErr).Trim());
        if (!inst.Success) throw new InvalidOperationException("install basarisiz: " + (inst.StdOut + inst.StdErr));
    }

    public Task<ProcessResult> EnableMultiWindowAsync(CancellationToken ct = default)
        => _wsl.RunAsync("waydroid prop set persist.waydroid.multi_windows true 2>&1", 15_000, ct);

    private static string Escape(string s) => "'" + s.Replace("'", "'\\''") + "'";
    private static string EscapePs(string s) => "'" + s.Replace("'", "''") + "'";
}
