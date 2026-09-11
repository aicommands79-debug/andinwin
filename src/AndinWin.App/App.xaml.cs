using System.Windows;
using AndinWin.App.Services;
using AndinWin.Core.Config;

namespace AndinWin.App;

public partial class App : global::System.Windows.Application
{
    private TrayIconManager? _tray;
    private MainWindow? _main;
    private readonly AndinWinOptions _opt = new();

    protected override void OnStartup(global::System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        _tray = new TrayIconManager(_opt);
        _tray.OpenMainWindowRequested += ShowMain;
        _tray.ResourceUpdated += snap =>
        {
            // UI thread'e tasi
            Dispatcher.BeginInvoke(() => _main?.UpdateResourceStatus(snap));
        };

        _main = new MainWindow();
        _main.HideToTrayRequested += () => _main.Hide();
        _main.Show();
        // Indir-calistir: ilk acilista distro yoksa wizard'i otomatik ac
        _ = CheckFirstRunAsync();
    }

    private async Task CheckFirstRunAsync()
    {
        try
        {
            var setup = new AndinWin.Core.Setup.SetupService();
            if (await setup.IsDistroInstalledAsync())
                return;
            await Dispatcher.BeginInvoke(async () =>
            {
                var r = global::System.Windows.MessageBox.Show(
                    "Ubuntu-24.04 bulunamadi. AndinWin'in calismasi icin gerekli kurulum (Ubuntu + Waydroid, tek seferlik, 10-20 dk) simdi baslatilsin mi?",
                    "AndinWin - Ilk Kurulum",
                    global::System.Windows.MessageBoxButton.YesNo,
                    global::System.Windows.MessageBoxImage.Question);
                if (r == global::System.Windows.MessageBoxResult.Yes && _main is not null)
                {
                    var w = new SetupWizardWindow { Owner = _main };
                    w.ShowDialog();
                }
            });
        }
        catch { }
    }

    private void ShowMain()
    {
        if (_main is null) return;
        if (!_main.IsVisible) _main.Show();
        if (_main.WindowState == global::System.Windows.WindowState.Minimized) _main.WindowState = global::System.Windows.WindowState.Normal;
        _main.Activate();
        _main.Focus();
    }

    protected override void OnExit(global::System.Windows.ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
