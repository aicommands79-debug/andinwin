using AndinWin.Core.Models;

namespace AndinWin.Core;

/// <summary>
/// waydroid CLI cikti parser'lari. Saf fonksiyonlar -> unit test ile dogrulanir, WSL gerekmez.
/// </summary>
public static class WaydroidParsers
{
    /// <summary>
    /// `waydroid app list` ciktisini parse eder. Bilinen formatlar:
    ///  - satir basi paket adi (com.foo.bar)
    ///  - "packageName: com.foo.bar" formundaki satirlar
    /// Gecici cozum: icinde nokta olan ve bosluk icermeyen token'lari paket say.
    /// </summary>
    public static IReadOnlyList<string> ParseAppList(string output)
    {
        var pkgs = new List<string>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#')) continue;
            // "package: com.foo.bar" / "packageName=com.foo.bar" varyantlari
            var m = global::System.Text.RegularExpressions.Regex.Match(line, @"([a-zA-Z_][\w]*(\.[\w]+)+)");
            if (!m.Success) continue;
            var pkg = m.Groups[1].Value.Trim().ToLowerInvariant();
            if (pkg.Contains('.') && !pkg.Contains(' ') && !pkgs.Contains(pkg))
                pkgs.Add(pkg);
        }
        return pkgs;
    }

    public static WaydroidStatus ParseStatus(string output)
    {
        var lower = output.ToLowerInvariant();
        bool session = lower.Contains("session") && lower.Contains("running");
        bool container = lower.Contains("container") && lower.Contains("running");
        // multi_windows prop ciktisi ayri sorgulanir; burada ham metinde "true" aranir
        bool multi = lower.Contains("persist.waydroid.multi_windows") && lower.Contains("true")
                     || lower.Contains("multi_windows") && lower.Contains("[true]");
        return new WaydroidStatus(session, container, multi, output);
    }

    public static string LabelFromPackage(string packageName)
    {
        var last = packageName.Split('.').LastOrDefault() ?? packageName;
        if (last.Length == 0) return packageName;
        return char.ToUpperInvariant(last[0]) + last[1..];
    }
}
