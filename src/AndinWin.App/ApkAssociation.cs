using System;
using System.IO;
using Microsoft.Win32;

namespace AndinWin.App;

/// <summary>
/// Portable kullanim icin tek tikla .apk iliskisi (HKCU, admin gerekmez).
/// Installer zaten aynisini yapar; bu, zip ile calistiranlar icin.
/// </summary>
public static class ApkAssociation
{
    private const string ProgId = "AndinWin.apk";

    public static string LauncherPath()
    {
        var dir = AppContext.BaseDirectory;
        var exe = Path.Combine(dir, "AndinWin.Launcher.exe");
        if (File.Exists(exe)) return exe;
        throw new FileNotFoundException("AndinWin.Launcher.exe bulunamadi: " + dir);
    }

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.apk");
        var v = key?.GetValue("") as string;
        if (!string.Equals(v, ProgId, StringComparison.OrdinalIgnoreCase)) return false;
        using var cmd = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}\shell\open\command");
        var c = cmd?.GetValue("") as string;
        return c?.Contains("AndinWin.Launcher.exe") == true;
    }

    public static void Register()
    {
        var launcher = LauncherPath();
        using (var ext = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.apk"))
            ext?.SetValue("", ProgId);
        using (var cmd = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            cmd?.SetValue("", $"\"{launcher}\" install \"%1\"");
    }
}
