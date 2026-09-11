using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;
using AndinWin.Core.Models;

namespace AndinWin.Core.Wsl;

/// <summary>wsl --shutdown / --terminate + free/proc parsing (tray Faz-3 icin).</summary>
public sealed class WslManager
{
    private readonly IProcessRunner _proc;
    private readonly AndinWinOptions _opt;
    public WslManager(IProcessRunner proc, AndinWinOptions? opt = null)
    {
        _proc = proc; _opt = opt ?? new AndinWinOptions();
    }

    public Task<ProcessResult> ShutdownAsync() => _proc.RunAsync("wsl.exe", new[] { "--shutdown" }, 30_000);
    public Task<ProcessResult> TerminateDistroAsync() => _proc.RunAsync("wsl.exe", new[] { "--terminate", _opt.DistroName }, 30_000);

    public static (double usedMb, double totalMb)? ParseFreeM(string freeOutput)
    {
        // "Mem:  7835  2100  1200 ..." satirini yakala
        foreach (var line in freeOutput.Split('\n'))
        {
            var t = line.Trim();
            if (!t.StartsWith("Mem:")) continue;
            var parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && double.TryParse(parts[1], out var total) && double.TryParse(parts[2], out var used))
                return (used, total);
        }
        return null;
    }
}
