namespace AndinWin.Core.Models;

public sealed record WaydroidStatus(
    bool SessionRunning,
    bool ContainerRunning,
    bool MultiWindowsEnabled,
    string RawOutput
);

public sealed record ProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr
)
{
    public bool Success => ExitCode == 0;
}
