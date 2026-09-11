using AndinWin.Core.Config;
using AndinWin.Core.Icons;

namespace AndinWin.Core.Tests;

public class IconPipelineTests
{
    [Fact]
    public void ConvertPngToIco_GecerliIcoUretir()
    {
        // 1x1 seffaf PNG
        var pngB64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var tmp = Path.Combine(Path.GetTempPath(), "andinwin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var png = Path.Combine(tmp, "a.png");
        File.WriteAllBytes(png, Convert.FromBase64String(pngB64));
        var opt = new AndinWinOptions();
        // gecici ikon klasoru kullan (orijinali ezme)
        var ico = IconPipeline.ConvertPngToIco(png, "com.test.icon", opt);
        Assert.NotNull(ico);
        Assert.True(File.Exists(ico));
        var bytes = File.ReadAllBytes(ico!);
        Assert.True(bytes.Length > 30);
        Assert.Equal(0, bytes[0]); // ICO reserved
        Assert.Equal(1, bytes[2]); // type
        // temizlik
        try { File.Delete(ico!); File.Delete(png); Directory.Delete(tmp); } catch { }
    }
}
