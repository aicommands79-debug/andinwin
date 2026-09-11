using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;
using AndinWin.Core.Models;

namespace AndinWin.Core;

/// <summary>Tum WSL erisimi buradan: wsl -d Ubuntu-24.04 -e ...</summary>
public sealed class WslRunner
{
    private readonly IProcessRunner _proc;
    private readonly AndinWinOptions _opt;

    public WslRunner(IProcessRunner proc, AndinWinOptions? opt = null)
    {
        _proc = proc;
        _opt = opt ?? new AndinWinOptions();
    }

    public Task<ProcessResult> RunAsync(string linuxCommand, int? timeoutMs = null, CancellationToken ct = default)
        => _proc.RunAsync("wsl.exe",
            new[] { "-d", _opt.DistroName, "-e", "sh", "-c", linuxCommand },
            timeoutMs ?? _opt.DefaultTimeoutMs, ct);

    public Task<ProcessResult> RunDirectAsync(string[] wslArgs, int? timeoutMs = null, CancellationToken ct = default)
    {
        var args = new List<string> { "-d", _opt.DistroName };
        args.AddRange(wslArgs);
        return _proc.RunAsync("wsl.exe", args, timeoutMs ?? _opt.DefaultTimeoutMs, ct);
    }

    public Task<ProcessResult> ListDistrosAsync(CancellationToken ct = default)
        => _proc.RunAsync("wsl.exe", new[] { "--list", "--verbose" }, 15_000, ct);
}
