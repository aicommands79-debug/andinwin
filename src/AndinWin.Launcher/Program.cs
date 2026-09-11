// AndinWin.Launcher: kisayollarin hedefi. Konsol gostermez (WinExe).
// Kullanim: AndinWin.Launcher.exe launch <paket> | install <apk> | status
using AndinWin.Core;
using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;

var opt = new AndinWinOptions();
var wsl = new WslRunner(new ProcessRunner(), opt);
var client = new WaydroidClient(wsl, opt);

if (args.Length == 0)
{
    return 2;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "launch" when args.Length >= 2:
            await client.LaunchAsync(args[1]);
            return 0;
        case "install" when args.Length >= 2:
            await client.InstallApkAsync(args[1]);
            return 0;
        case "status":
            var st = await client.GetStatusAsync();
            Console.WriteLine(st.RawOutput);
            return st.SessionRunning ? 0 : 1;
        default:
            return 2;
    }
}
catch (Exception ex)
{
    try { File.AppendAllText(Path.Combine(opt.AppDataDir, "launcher-error.log"), $"[{DateTimeOffset.Now}] {ex}\n"); } catch { }
    return 1;
}
