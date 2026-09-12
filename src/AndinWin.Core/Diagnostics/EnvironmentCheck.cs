namespace AndinWin.Core.Diagnostics;

public sealed record EnvCheck(string Name, bool Ok, string Detail);

/// <summary>
/// Waydroid zincirinin 6 halkasini tek WSL cagrisiyla kontrol eder.
/// Cikti parse saf fonksiyondur (unit testli); calistirma WaydroidClient uzerinden.
/// </summary>
public static class EnvironmentCheck
{
    public const string ProbeScript =
        "echo @@WHICH; which waydroid 2>&1; " +
        "echo @@STATUS; waydroid status 2>&1; " +
        "echo @@BINDER; ls /dev/binder* /dev/anbox* 2>&1; " +
        "echo @@SYSTEMD; systemctl is-system-running 2>&1; " +
        "echo @@CONTAINER; systemctl is-active waydroid-container 2>&1; " +
        "echo @@KERNEL; uname -r 2>&1; echo @@END";

    public static IReadOnlyList<EnvCheck> ParseProbe(string output)
    {
        var sections = new Dictionary<string, string>();
        string? current = null;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim().TrimEnd('\r');
            if (line.StartsWith("@@"))
            {
                current = line;
                sections[current] = "";
            }
            else if (current is not null)
                sections[current] += line + "\n";
        }
        string Get(string m) => sections.TryGetValue(m, out var v) ? v.Trim() : "";

        var list = new List<EnvCheck>();
        var which = Get("@@WHICH");
        list.Add(new EnvCheck("Waydroid kurulu", which.Contains("/waydroid"),
            which.Contains("/waydroid") ? which.Split('\n')[0].Trim() : "bulunamadi (Kurulum Sihirbazi gerekli)"));

        var status = Get("@@STATUS");
        bool initialized = which.Contains("/waydroid") && !status.Contains("not initialized", StringComparison.OrdinalIgnoreCase);
        list.Add(new EnvCheck("Init yapilmis", initialized,
            !which.Contains("/waydroid") ? "waydroid yok"
            : initialized ? "tamam"
            : "eksik (Kurulum > Eksik Kurulumu Tamamla)"));

        var binder = Get("@@BINDER");
        // ls hatasi da yolu tirnak icinde gecirir ("cannot access '/dev/binder*'"), o yuzden satir basi aranir
        bool binderOk = binder.Split('\n').Any(l => l.TrimStart().StartsWith("/dev/"));
        list.Add(new EnvCheck("Binder surucusu (/dev/binder)", binderOk,
            binderOk ? "tamam"
            : "YOK: WSL cekirdegi binder icermiyor, ozel cekirdek gerekir (asagidaki nota bak)"));

        var systemd = Get("@@SYSTEMD").Split('\n').FirstOrDefault()?.Trim() ?? "";
        bool systemdOk = systemd.Equals("running", StringComparison.OrdinalIgnoreCase)
            || systemd.Equals("degraded", StringComparison.OrdinalIgnoreCase);
        list.Add(new EnvCheck("Systemd calisiyor", systemdOk,
            string.IsNullOrEmpty(systemd) ? "yanit yok" : systemd));

        var container = Get("@@CONTAINER").Split('\n').FirstOrDefault()?.Trim() ?? "";
        list.Add(new EnvCheck("Waydroid container servisi", container == "active",
            string.IsNullOrEmpty(container) ? "yanit yok" : container));

        var slow = status.ToLowerInvariant();
        bool sessionRunning = slow.Contains("session") && slow.Contains("running");
        list.Add(new EnvCheck("Waydroid session", sessionRunning,
            !initialized ? "init eksik oldugu icin bakilmadi"
            : sessionRunning ? "RUNNING" : "durmus (Waydroid > Session Baslat)"));

        var kernel = Get("@@KERNEL").Split('\n').FirstOrDefault()?.Trim() ?? "?";
        list.Add(new EnvCheck("Cekirdek", true, kernel));
        return list;
    }

    public static string RenderText(IReadOnlyList<EnvCheck> checks)
    {
        var lines = checks.Select(c => $"{(c.Ok ? "[OK] " : "[EKSIK] ")} {c.Name}: {c.Detail}");
        return string.Join("\n", lines) +
            "\n\nBinder EKSIK ise: Waydroid, WSL'nin varsayilan cekirdegiyle calismaz. " +
            "Docs: Waydroid + WSL icin binder yamali ozel cekirdek gerekir.";
    }
}
