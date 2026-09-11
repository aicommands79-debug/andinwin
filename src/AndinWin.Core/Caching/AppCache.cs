using System.Text.Json;
using AndinWin.Core.Config;
using AndinWin.Core.Models;

namespace AndinWin.Core.Caching;

/// <summary>WSL her acilista sorgulanmasin diye JSON dosya cache (Faz-3'te SQLite'a gecebilir).</summary>
public sealed class AppCache
{
    private readonly AndinWinOptions _opt;
    public AppCache(AndinWinOptions? opt = null) => _opt = opt ?? new AndinWinOptions();

    public async Task SaveAsync(IReadOnlyList<AndroidApp> apps, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_opt.AppDataDir);
        var json = JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_opt.CacheFile, json, ct);
    }

    public async Task<IReadOnlyList<AndroidApp>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_opt.CacheFile)) return Array.Empty<AndroidApp>();
        var json = await File.ReadAllTextAsync(_opt.CacheFile, ct);
        return JsonSerializer.Deserialize<List<AndroidApp>>(json) ?? new List<AndroidApp>();
    }
}
