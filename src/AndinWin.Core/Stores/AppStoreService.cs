using System.Text.Json;

namespace AndinWin.Core.Stores;

/// <summary>
/// Play Store'a erisilemeyen cihazlar icin acik kaynak alternatif: Aurora Store.
/// APK, F-Droid reposundan cekilir (sabit API deseni, surum otomatik bulunur).
/// Not: AAB dosyalari dogrudan kurulamaz; Aurora/Play Store AAB'yi kendi icinde halleder.
/// Bu yuzden dogru akis: Aurora Store'u kur -> uygulamayi oradan indir.
/// </summary>
public sealed class AppStoreService : IDisposable
{
    public const string AuroraPackage = "com.aurora.store";
    public const string PlayStorePackage = "com.android.vending";

    private readonly HttpClient _http;
    private bool _disposed;

    public AppStoreService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public static string BuildApkUrl(string packageName, long versionCode)
        => $"https://f-droid.org/repo/{packageName}_{versionCode}.apk";

    public async Task<(string VersionName, long VersionCode, string ApkUrl)> GetAuroraStoreInfoAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"https://f-droid.org/api/v1/packages/{AuroraPackage}", ct);
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        var code = root.GetProperty("suggestedVersionCode").GetInt64();
        var name = "?";
        foreach (var p in root.GetProperty("packages").EnumerateArray())
        {
            if (p.TryGetProperty("versionCode", out var vc) && vc.GetInt64() == code
                && p.TryGetProperty("versionName", out var vn))
            {
                name = vn.GetString() ?? "?";
                break;
            }
        }
        return (name, code, BuildApkUrl(AuroraPackage, code));
    }

    public async Task<string> DownloadApkAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(destPath);
        var buf = new byte[81920];
        long done = 0;
        int n;
        while ((n = await src.ReadAsync(buf, ct)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, n), ct);
            done += n;
            if (total > 0) progress?.Report(done * 100.0 / total.Value);
        }
        return destPath;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
