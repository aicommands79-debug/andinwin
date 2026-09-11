using AndinWin.Core.Config;

namespace AndinWin.Core.Icons;

/// <summary>
/// Faz-2: WSL'deki waydroid png ikonunu Windows .ico'ya cevirir.
/// Harici bagimlilik yok: PNG'yi ICO container icine gom (Vista+ PNG-sikistirmali ICO destekler).
/// </summary>
public static class IconPipeline
{
    public static string? ConvertPngToIco(string pngPath, string packageName, AndinWinOptions? opt = null)
    {
        if (!File.Exists(pngPath)) return null;
        opt ??= new AndinWinOptions();
        Directory.CreateDirectory(opt.IconsDir);
        var icoPath = Path.Combine(opt.IconsDir, packageName + ".ico");
        var png = File.ReadAllBytes(pngPath);
        var (w, h) = ReadPngSize(png);
        using var fs = File.Open(icoPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);
        bw.Write((short)0); // reserved
        bw.Write((short)1); // type ICO
        bw.Write((short)1); // count
        bw.Write(ToIcoByte(w));
        bw.Write(ToIcoByte(h));
        bw.Write((byte)0); // colors
        bw.Write((byte)0); // reserved
        bw.Write((short)1); // planes
        bw.Write((short)32); // bpp
        bw.Write(png.Length);
        bw.Write(6 + 16); // offset
        bw.Write(png);
        return icoPath;
    }

    /// <summary>\\wsl$\Ubuntu-24.04\... uzerinden erisilebilen WSL ikonunu bulur.</summary>
    public static string? FindWslIcon(string packageName, string distro, string? linuxUser = null)
    {
        // En olasi konumlar (hicolor 192 ve 512)
        var user = linuxUser ?? Environment.UserName.ToLowerInvariant();
        var baseUnc = $@"\\wsl$\{distro}\home\{user}\.local\share\icons\hicolor";
        foreach (var size in new[] { "192x192", "512x512", "256x256", "128x128", "96x96" })
        {
            var p = Path.Combine(baseUnc, size, "apps", $"waydroid.{packageName}.png");
            if (File.Exists(p)) return p;
            var p2 = Path.Combine(baseUnc, size, "apps", $"{packageName}.png");
            if (File.Exists(p2)) return p2;
        }
        return null;
    }

    private static byte ToIcoByte(int v) => v >= 256 ? (byte)0 : (byte)v;

    private static (int w, int h) ReadPngSize(byte[] png)
    {
        try
        {
            // PNG IHDR: 16..24 arasi width/height big-endian
            if (png.Length > 24 && png[0] == 0x89 && png[1] == 0x50)
            {
                int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
                int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
                if (w > 0 && h > 0 && w <= 2048 && h <= 2048) return (w, h);
            }
        }
        catch { }
        return (256, 256);
    }
}
