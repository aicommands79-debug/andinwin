namespace AndinWin.Core.Config;

/// <summary>Tek dagitim stratejisi: sabit Ubuntu-24.04 (kullanicinin sectigi model).</summary>
public sealed class AndinWinOptions
{
    public string DistroName { get; set; } = "Ubuntu-24.04";
    public int DefaultTimeoutMs { get; set; } = 60_000;

    public string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndinWin");

    public string IconsDir => Path.Combine(AppDataDir, "Icons");
    public string ApkStagingDir => Path.Combine(AppDataDir, "Staging");
    public string ShortcutFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs\AndinWin Apps");

    public string CacheFile => Path.Combine(AppDataDir, "apps-cache.json");
}
