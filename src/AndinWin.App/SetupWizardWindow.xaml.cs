using System.Windows;
using AndinWin.Core.Setup;

namespace AndinWin.App;

public partial class SetupWizardWindow : global::System.Windows.Window
{
    private readonly SetupService _setup = new();
    private CancellationTokenSource? _cts;

    public SetupWizardWindow()
    {
        InitializeComponent();
    }

    private async void Start_Click(object sender, global::System.Windows.RoutedEventArgs e)
    {
        StartBtn.IsEnabled = false;
        _cts = new CancellationTokenSource();
        Log("Kurulum basladi...");
        var prog = new Progress<SetupProgress>(p => Log($"[{p.Step}] {p.Message}"));
        try
        {
            await _setup.RunAllAsync(prog, _cts.Token);
            Log("TAMAMLANDI. Ana pencereden Yenile'ye basin.");
            global::System.Windows.MessageBox.Show("Kurulum tamamlandi. WSL yeniden baslatildi.", "AndinWin", global::System.Windows.MessageBoxButton.OK, global::System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log("HATA: " + ex.Message);
            global::System.Windows.MessageBox.Show(ex.Message, "Kurulum hatasi", global::System.Windows.MessageBoxButton.OK, global::System.Windows.MessageBoxImage.Error);
        }
        finally { StartBtn.IsEnabled = true; }
    }

    private void Log(string s) => LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\n");

    private void Close_Click(object sender, global::System.Windows.RoutedEventArgs e)
    {
        try { _cts?.Cancel(); } catch { }
        Close();
    }
}
