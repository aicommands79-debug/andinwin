namespace AndinWin.Core.Models;

public sealed record ResourceSnapshot(
    DateTimeOffset Timestamp,
    double? WslMemoryUsedMb,
    double? WslMemoryTotalMb,
    double? WslCpuPercent
);
