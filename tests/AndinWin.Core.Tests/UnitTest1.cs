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
}
