namespace AndinWin.Core.Models;

/// <summary>WSL icindeki Waydroid'e yuklu bir Android uygulamasi.</summary>
public sealed record AndroidApp(
    string PackageName,
    string Label,
    string? DesktopFile = null,
    string? IconSourcePath = null, // WSL icindeki png yolu
    string? CachedIconPath = null  // Windows tarafindaki .ico yolu
);
