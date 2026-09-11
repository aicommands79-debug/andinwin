using AndinWin.Core.Models;

namespace AndinWin.Core.Wsl;

/// <summary>Faz-3: 5 sn'de bir free/loadavg okuyup snapshot uretir (tray gostergesi icin).</summary>
public sealed class ResourceMonitor
{
    private readonly WslRunner _wsl;
    public ResourceMonitor(WslRunner wsl) => _wsl = wsl;

    public async Task<ResourceSnapshot> SampleAsync(CancellationToken ct = default)
    {
        var r = await _wsl.RunAsync("free -m 2>&1; echo ---LOAD---; cat /proc/loadavg 2>&1", 15_000, ct);
        var mem = WslManager.ParseFreeM(r.StdOut);
        double? cpu = ParseLoad(r.StdOut);
        return new ResourceSnapshot(DateTimeOffset.Now, mem?.usedMb, mem?.totalMb, cpu);
    }

    private static double? ParseLoad(string s)
    {
        foreach (var line in s.Split('\n'))
        {
            var t = line.Trim();
            var parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && double.TryParse(parts[0], global::System.Globalization.NumberStyles.Float, global::System.Globalization.CultureInfo.InvariantCulture, out var l1))
                return l1 * 100.0 / Environment.ProcessorCount;
        }
        return null;
    }
}
