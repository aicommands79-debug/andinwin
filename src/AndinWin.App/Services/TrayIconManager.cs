using System.Drawing;
using WinForms = System.Windows.Forms;
using AndinWin.Core;
using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;
using AndinWin.Core.Models;
using AndinWin.Core.Wsl;

namespace AndinWin.App.Services;

/// <summary>
/// Faz-3: Sistem tepsisi. Harici paket yok, WinForms NotifyIcon kullanir.
/// 5 sn'de bir ResourceMonitor ile RAM/CPU gunceller.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ToolStripMenuItem _statusItem;
    private readonly global::System.Threading.Timer _timer;
    private readonly ResourceMonitor _monitor;
    private readonly WaydroidClient _client;
    private readonly WslManager _wslMgr;
    private readonly AndinWinOptions _opt;
    private bool _disposed;

    public event Action? OpenMainWindowRequested;
    public event Action<ResourceSnapshot>? ResourceUpdated;

    public TrayIconManager(AndinWinOptions? opt = null)
    {
        _opt = opt ?? new AndinWinOptions();
        var runner = new ProcessRunner();
        var wsl = new WslRunner(runner, _opt);
        _monitor = new ResourceMonitor(wsl);
        _client = new WaydroidClient(wsl, _opt);
        _wslMgr = new WslManager(runner, _opt);

        _icon = new WinForms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "AndinWin - baslatiliyor...",
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => OpenMainWindowRequested?.Invoke();

        var menu = new WinForms.ContextMenuStrip();
        _statusItem = new WinForms.ToolStripMenuItem("Durum: bilinmiyor") { Enabled = false };
        var open = new WinForms.ToolStripMenuItem("Ana Pencereyi Ac", null, (_, _) => OpenMainWindowRequested?.Invoke());
        var start = new WinForms.ToolStripMenuItem("Waydroid Session Baslat", null, async (_, _) => await FireAndForget(() => _client.EnsureSessionAsync(), "Session baslatildi"));
        var stop = new WinForms.ToolStripMenuItem("Waydroid Session Durdur", null, async (_, _) => await FireAndForget(() => StopSessionAsync(), "Session durduruldu"));
        var shutdown = new WinForms.ToolStripMenuItem("WSL'i Kapat (wsl --shutdown)", null, async (_, _) => await FireAndForget(() => _wslMgr.ShutdownAsync(), "WSL kapatildi"));
        var exit = new WinForms.ToolStripMenuItem("Cikis", null, (_, _) => global::System.Windows.Application.Current.Shutdown());

        menu.Items.Add(_statusItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(open);
        menu.Items.Add(start);
        menu.Items.Add(stop);
        menu.Items.Add(shutdown);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exit);
        _icon.ContextMenuStrip = menu;

        _timer = new global::System.Threading.Timer(async _ => await PollAsync(), null, 1000, 5000);
    }

    private async Task StopSessionAsync()
    {
        var r = new WslRunner(new ProcessRunner(), _opt);
        await r.RunAsync("waydroid session stop 2>&1");
    }

    private async Task FireAndForget(Func<Task> fn, string balloon)
    {
        try
        {
            await fn();
            _icon.ShowBalloonTip(2000, "AndinWin", balloon, WinForms.ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _icon.ShowBalloonTip(3000, "AndinWin hata", ex.Message, WinForms.ToolTipIcon.Error);
        }
    }

    private async Task PollAsync()
    {
        try
        {
            var snap = await _monitor.SampleAsync();
            ResourceUpdated?.Invoke(snap);
            string txt;
            if (snap.WslMemoryUsedMb is null)
                txt = "AndinWin - WSL erisilemedi";
            else
                txt = $"AndinWin - RAM {snap.WslMemoryUsedMb:F0}/{snap.WslMemoryTotalMb:F0} MB" +
                      (snap.WslCpuPercent is null ? "" : $" | CPU %{snap.WslCpuPercent:F0}");
            // NotifyIcon Text max 63 karakter
            if (txt.Length > 63) txt = txt[..63];
            try
            {
                _icon.Text = txt;
                _statusItem.Text = txt;
            }
            catch { }
        }
        catch { }
    }

    public void ShowBalloon(string title, string text) =>
        _icon.ShowBalloonTip(2000, title, text, WinForms.ToolTipIcon.Info);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
    }
}
