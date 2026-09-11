using System.IO;
using AndinWin.Core.Config;

namespace AndinWin.App;

/// <summary>Faz-2 MVP: Baslat Menusu + Masaustu .lnk uretimi. Hedef: Launcher.exe launch &lt;pkg&gt; (konsol cakmaz).</summary>
public static class ShortcutManager
{
    public static string CreateShortcuts(string packageName, string label, AndinWinOptions? opt = null, bool desktopToo = false)
    {
        opt ??= new AndinWinOptions();
        Directory.CreateDirectory(opt.ShortcutFolder);

        var safe = string.Concat(label.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(safe)) safe = packageName;
        var lnkPath = Path.Combine(opt.ShortcutFolder, safe + ".lnk");

        var launcher = Path.Combine(AppContext.BaseDirectory, "AndinWin.Launcher.exe");
        // gelistirme modunda launcher ayni klasorde olmayabilir -> kendine fallback
        if (!File.Exists(launcher))
            launcher = Environment.ProcessPath ?? launcher;

        CreateLnk(lnkPath, launcher, $"launch {packageName}", FindIcon(packageName, opt));

        if (desktopToo)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateLnk(Path.Combine(desktop, safe + ".lnk"), launcher, $"launch {packageName}", FindIcon(packageName, opt));
        }
        return lnkPath;
    }

    private static string? FindIcon(string packageName, AndinWinOptions opt)
    {
        var ico = Path.Combine(opt.IconsDir, packageName + ".ico");
        return File.Exists(ico) ? ico : null;
    }

    private static void CreateLnk(string lnkPath, string target, string args, string? iconPath)
    {
        // Gec baglama: COM referansi gerektirmez, System32/wshom.ocx kullanir
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell bulunamadi.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic lnk = shell.CreateShortcut(lnkPath);
        lnk.TargetPath = target;
        lnk.Arguments = args;
        lnk.WorkingDirectory = Path.GetDirectoryName(target) ?? "";
        if (iconPath is not null) lnk.IconLocation = iconPath;
        lnk.Save();
    }
}
