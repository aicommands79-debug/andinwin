using AndinWin.Core;
using AndinWin.Core.Wsl;

namespace AndinWin.Core.Tests;

public class WaydroidParserTests
{
    [Fact]
    public void ParseAppList_SadeListe()
    {
        var outp = "com.spotify.music\ncom.whatsapp\norg.fdroid.fdroid\n";
        var list = WaydroidParsers.ParseAppList(outp);
        Assert.Equal(3, list.Count);
        Assert.Contains("com.spotify.music", list);
    }

    [Fact]
    public void ParseAppList_KarisikCikti_BosSatirVeBasliklariAtlar()
    {
        var outp = "Installed apps:\n  com.android.settings\n# yorum\n\n  package: com.example.app v1\n";
        var list = WaydroidParsers.ParseAppList(outp);
        Assert.Contains("com.android.settings", list);
        Assert.Contains("com.example.app", list);
    }

    [Fact]
    public void ParseStatus_RunningAlgilar()
    {
        var fake = "Session: RUNNING\nContainer: RUNNING\npersist.waydroid.multi_windows: true";
        var st = WaydroidParsers.ParseStatus(fake);
        Assert.True(st.SessionRunning);
        Assert.True(st.ContainerRunning);
        Assert.True(st.MultiWindowsEnabled);
    }

    [Fact]
    public void ParseStatus_StoppedAlgilar()
    {
        var fake = "Session: STOPPED\nContainer: STOPPED";
        var st = WaydroidParsers.ParseStatus(fake);
        Assert.False(st.SessionRunning);
        Assert.False(st.ContainerRunning);
    }

    [Fact]
    public void LabelFromPackage_SonParcayiBuyutur()
    {
        Assert.Equal("Music", WaydroidParsers.LabelFromPackage("com.spotify.music"));
    }

    [Fact]
    public void ParseFreeM_MemSatiriniOkur()
    {
        var free = "              total        used        free\nMem:           7835        2100        1200\nSwap:             0           0           0";
        var parsed = WslManager.ParseFreeM(free);
        Assert.NotNull(parsed);
        Assert.Equal(2100, parsed!.Value.usedMb);
        Assert.Equal(7835, parsed!.Value.totalMb);
    }

    [Fact]
    public void LooksLikeNotRunning_DurmusSessioniYakalar()
    {
        Assert.True(WaydroidParsers.LooksLikeNotRunning("WayDroid session is stopped\n"));
        Assert.True(WaydroidParsers.LooksLikeNotRunning("ERROR: container failed to start"));
        Assert.False(WaydroidParsers.LooksLikeNotRunning("com.spotify.music\ncom.whatsapp\n"));
        Assert.False(WaydroidParsers.LooksLikeNotRunning(""));
    }

    [Fact]
    public void BuildApkUrl_FdroidDeseni()
    {
        Assert.Equal("https://f-droid.org/repo/com.aurora.store_76.apk",
            AndinWin.Core.Stores.AppStoreService.BuildApkUrl("com.aurora.store", 76));
    }

    [Fact]
    public void ParseKernelRelease_AssetleriBulur()
    {
        var json = "{\"tag_name\":\"linux-msft-wsl-6.6.1-binder\",\"assets\":[" +
            "{\"name\":\"bzImage\",\"browser_download_url\":\"https://example.com/bzImage\"}," +
            "{\"name\":\"modules.tar.gz\",\"browser_download_url\":\"https://example.com/modules.tar.gz\"}]}";
        var (tag, bz, mod) = AndinWin.Core.Setup.SetupService.ParseKernelRelease(json);
        Assert.Equal("linux-msft-wsl-6.6.1-binder", tag);
        Assert.EndsWith("bzImage", bz);
        Assert.EndsWith("modules.tar.gz", mod);
    }

    [Fact]
    public void UpdateWslConfig_MevcutAyarlariKorur()
    {
        var before = "[wsl2]\nmemory=8GB\n";
        var after = AndinWin.Core.Setup.SetupService.UpdateWslConfig(before, "C:\\k\\bzImage");
        Assert.Contains("memory=8GB", after);
        Assert.Contains("kernel=C:\\k\\bzImage", after);
        // ikinci calisma cift satir uretmemeli
        var twice = AndinWin.Core.Setup.SetupService.UpdateWslConfig(after, "C:\\k2\\bzImage");
        Assert.DoesNotContain("C:\\k\\bzImage", twice);
        Assert.Contains("kernel=C:\\k2\\bzImage", twice);
        // bolum yoksa ekler
        var fresh = AndinWin.Core.Setup.SetupService.UpdateWslConfig("", "C:\\k\\bzImage");
        Assert.Contains("[wsl2]", fresh);
    }

    [Fact]
    public void ParseProbe_SaglikliMakineyiYesilGosterir()
    {
        var fake = "@@WHICH\n/usr/bin/waydroid\n@@STATUS\nSession:\tRUNNING\nContainer:\tRUNNING\n" +
            "@@BINDER\n/dev/binder\n@@SYSTEMD\nrunning\n@@CONTAINER\nactive\n@@KERNEL\n5.15.133-binder\n@@END\n";
        var checks = AndinWin.Core.Diagnostics.EnvironmentCheck.ParseProbe(fake);
        Assert.Equal(7, checks.Count);
        Assert.All(checks, c => Assert.True(c.Ok));
    }

    [Fact]
    public void ParseProbe_EksikBinderVeInitiYakalar()
    {
        var fake = "@@WHICH\n/usr/bin/waydroid\n@@STATUS\nWaydroid is not initialized, run \"waydroid init\"\n" +
            "@@BINDER\nls: cannot access '/dev/binder*': No such file or directory\n@@SYSTEMD\nrunning\n" +
            "@@CONTAINER\ninactive\n@@KERNEL\n5.15.146.1-microsoft-standard-WSL2\n@@END\n";
        var byName = AndinWin.Core.Diagnostics.EnvironmentCheck.ParseProbe(fake).ToDictionary(c => c.Name);
        Assert.False(byName["Init yapilmis"].Ok);
        Assert.False(byName["Binder surucusu (/dev/binder)"].Ok);
        Assert.True(byName["Systemd calisiyor"].Ok);
        Assert.False(byName["Waydroid container servisi"].Ok);
    }

    [Fact]
    public void NormalizeDistroList_Utf16KirlenmesiniTemizler()
    {
        // wsl.exe UTF-16 basar: "U\0b\0u\0..." ve bosluklu cozumu "U b u n t u"
        var broken = "U\0b\0u\0n\0t\0u\0-\02\04\0.\00\04\0\r\nk\0a\0l\0i\0-\0l\0i\0n\0u\0x\0\r\n";
        var names = AndinWin.Core.Setup.SetupService.NormalizeDistroList(broken);
        Assert.Contains("Ubuntu-24.04", names);
        var spaced = "U b u n t u - 2 4 . 0 4\n";
        Assert.Contains("Ubuntu-24.04", AndinWin.Core.Setup.SetupService.NormalizeDistroList(spaced));
    }
}
