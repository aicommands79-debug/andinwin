using System.IO;
using Wpf = global::System.Windows;
using Microsoft.Win32;
using AndinWin.Core;
using AndinWin.Core.Caching;
using AndinWin.Core.Config;
using AndinWin.Core.Infrastructure;
using AndinWin.Core.Models;
using AndinWin.Core.Wsl;

namespace AndinWin.App;

public partial class MainWindow : Wpf.Window
{
    public event Action? HideToTrayRequested;
    private bool _explicitExit;

    private readonly AndinWinOptions _opt = new();
    private readonly WaydroidClient _client;
    private readonly AppCache _cache;
    private readonly WslManager _wslMgr;

    public MainWindow()
    {
        InitializeComponent();
        var runner = new ProcessRunner();
        var wsl = new WslRunner(runner, _opt);
        _client = new WaydroidClient(wsl, _opt);
        _cache = new AppCache(_opt);
        _wslMgr = new WslManager(runner, _opt);
        Loaded += async (_, _) => await RefreshAsync(useCacheFirst: true);
    }

    private async Task RefreshAsync(bool useCacheFirst = false)
    {
        try
        {
            if (useCacheFirst)
            {
                var cached = await _cache.LoadAsync();
                if (cached.Count > 0) AppList.ItemsSource = cached;
            }
            StatusText.Text = "Waydroid sorgulaniyor...";
            var apps = await _client.ListAppsAsync();
            AppList.ItemsSource = apps;
            await _cache.SaveAsync(apps);
            StatusText.Text = $"{apps.Count} uygulama bulundu.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "WSL/Waydroid erisilemedi: " + ex.Message;
        }
    }

    private AndroidApp? Selected => AppList.SelectedItem as AndroidApp;

    private async void Refresh_Click(object sender, Wpf.RoutedEventArgs e) => await RefreshAsync();
    private async void Launch_Click(object sender, Wpf.RoutedEventArgs e) => await LaunchSelectedAsync();
    private async void AppList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => await LaunchSelectedAsync();

    private async Task LaunchSelectedAsync()
    {
        if (Selected is null) { StatusText.Text = "Once listeden uygulama sec."; return; }
        try
        {
            StatusText.Text = $"{Selected.Label} baslatiliyor...";
            await _client.LaunchAsync(Selected.PackageName);
            StatusText.Text = $"{Selected.Label} baslatildi.";
        }
        catch (Exception ex) { StatusText.Text = "Baslatma hatasi: " + ex.Message; }
    }

    private async void InstallApk_Click(object sender, Wpf.RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Android paketi (*.apk)|*.apk" };
        if (dlg.ShowDialog() == true) await InstallAsync(dlg.FileName);
    }

    private async Task InstallAsync(string apkPath)
    {
        try
        {
            var prog = new Progress<string>(m => StatusText.Text = m);
            await _client.InstallApkAsync(apkPath, prog);
            StatusText.Text = "Yukleme tamamlandi, liste yenileniyor...";
            await RefreshAsync();
        }
        catch (Exception ex) { StatusText.Text = "Yukleme hatasi: " + ex.Message; }
    }

    private void Window_DragOver(object sender, Wpf.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(Wpf.DataFormats.FileDrop) ? Wpf.DragDropEffects.Copy : Wpf.DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, Wpf.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(Wpf.DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(Wpf.DataFormats.FileDrop);
        var apk = files.FirstOrDefault(f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
        if (apk is null) { StatusText.Text = "Sadece .apk dosyasi birak."; return; }
        await InstallAsync(apk);
    }

    private void Shortcut_Click(object sender, Wpf.RoutedEventArgs e)
    {
        if (Selected is null) { StatusText.Text = "Kisayol icin uygulama sec."; return; }
        try
        {
            // Faz-2: WSL ikonunu bulup .ico'ya cevir, sonra lnk olustur
            var wslIcon = AndinWin.Core.Icons.IconPipeline.FindWslIcon(Selected.PackageName, _opt.DistroName);
            if (wslIcon is not null)
                AndinWin.Core.Icons.IconPipeline.ConvertPngToIco(wslIcon, Selected.PackageName, _opt);
            var created = ShortcutManager.CreateShortcuts(Selected.PackageName, Selected.Label, _opt);
            StatusText.Text = "Kisayol olusturuldu: " + created;
        }
        catch (Exception ex) { StatusText.Text = "Kisayol hatasi: " + ex.Message; }
    }

    private async void SessionStart_Click(object sender, Wpf.RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Session baslatiliyor...";
            await _client.EnsureSessionAsync();
            StatusText.Text = "Session hazir, liste yenileniyor...";
            await RefreshAsync();
        }
        catch (Exception ex) { StatusText.Text = "Session baslatilamadi: " + ex.Message; }
    }

    private async void SessionStop_Click(object sender, Wpf.RoutedEventArgs e)
    {
        StatusText.Text = "Session durduruluyor...";
        var r = new WslRunner(new ProcessRunner(), _opt);
        await r.RunAsync("waydroid session stop 2>&1");
        StatusText.Text = "Session durduruldu.";
    }

    private async void WslShutdown_Click(object sender, Wpf.RoutedEventArgs e)
    {
        var c = Wpf.MessageBox.Show("WSL tamamen kapatilsin mi? (wsl --shutdown)", "AndinWin", Wpf.MessageBoxButton.YesNo);
        if (c != Wpf.MessageBoxResult.Yes) return;
        await _wslMgr.ShutdownAsync();
        StatusText.Text = "WSL kapatildi.";
    }

    private void Exit_Click(object sender, Wpf.RoutedEventArgs e)
    {
        _explicitExit = true;
        global::System.Windows.Application.Current.Shutdown();
    }

    private void MinimizeToTray_Click(object sender, Wpf.RoutedEventArgs e) => HideToTrayRequested?.Invoke();

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_explicitExit) return;
        e.Cancel = true;
        HideToTrayRequested?.Invoke();
    }

    public void UpdateResourceStatus(ResourceSnapshot snap)
    {
        if (snap.WslMemoryUsedMb is null) ResourceText.Text = "WSL: erisilemedi";
        else ResourceText.Text = $"WSL: {snap.WslMemoryUsedMb:F0}/{snap.WslMemoryTotalMb:F0} MB" +
            (snap.WslCpuPercent is null ? "" : $" %{snap.WslCpuPercent:F0}");
    }

    private void SetupWizard_Click(object sender, Wpf.RoutedEventArgs e)
    {
        var w = new SetupWizardWindow();
        w.Owner = this;
        w.ShowDialog();
    }

    private async void Diagnose_Click(object sender, Wpf.RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Tani toplaniyor...";
            var st = await _client.GetStatusAsync();
            Wpf.MessageBox.Show(st.RawOutput.Length > 2000 ? st.RawOutput[..2000] : st.RawOutput,
                "AndinWin Tani (waydroid status)", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Information);
            StatusText.Text = "Tani gosterildi.";
        }
        catch (Exception ex) { StatusText.Text = "Tani alinamadi: " + ex.Message; }
    }

    private void AssociateApk_Click(object sender, Wpf.RoutedEventArgs e)
    {
        try
        {
            if (ApkAssociation.IsRegistered()) { StatusText.Text = ".apk iliskisi zaten kurulu."; return; }
            ApkAssociation.Register();
            StatusText.Text = ".apk iliskisi kuruldu: cift tikla kurulum calisir.";
        }
        catch (Exception ex) { StatusText.Text = "Iliski kurulamadi: " + ex.Message; }
    }
}
