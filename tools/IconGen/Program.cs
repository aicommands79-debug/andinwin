// AndinWin ikon/banner ureteci: "Windows penceresi icinde Android telefonu" motifi.
// Calistirma: dotnet run --project tools/IconGen -- <repo-koku>
// Ciktilar: assets/logo.svg, assets/banner.png, assets/icons/png/*.png, assets/icons/andinwin.ico
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

var root = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var assets = Path.Combine(root, "assets");
var iconsDir = Path.Combine(assets, "icons");
var pngDir = Path.Combine(iconsDir, "png");
Directory.CreateDirectory(pngDir);

var winBlue = Color.FromArgb(0, 120, 212);
var phoneGreen = Color.FromArgb(61, 220, 132);
var phoneDark = Color.FromArgb(32, 33, 36);

// 1) SVG master (vektor kaynak)
File.WriteAllText(Path.Combine(assets, "logo.svg"), SvgMaster());
Console.WriteLine("logo.svg yazildi");

// 2) PNG ikonlar
foreach (var s in new[] { 16, 24, 32, 48, 64, 128, 256 })
{
    using var bmp = new Bitmap(s, s);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);
    DrawLogo(g, s);
    var path = Path.Combine(pngDir, $"andinwin-{s}.png");
    bmp.Save(path, ImageFormat.Png);
    Console.WriteLine($"{path} yazildi");
}

// 3) Cok cozunurluklu ICO (PNG gomulu: 16/32/48/256)
var icoSizes = new[] { 16, 32, 48, 256 };
var pngs = icoSizes.Select(s => File.ReadAllBytes(Path.Combine(pngDir, $"andinwin-{s}.png"))).ToList();
using (var fs = File.Open(Path.Combine(iconsDir, "andinwin.ico"), FileMode.Create))
using (var bw = new BinaryWriter(fs))
{
    bw.Write((short)0);
    bw.Write((short)1);
    bw.Write((short)pngs.Count);
    int offset = 6 + 16 * pngs.Count;
    for (int i = 0; i < pngs.Count; i++)
    {
        int dim = icoSizes[i] >= 256 ? 0 : icoSizes[i];
        bw.Write((byte)dim);
        bw.Write((byte)dim);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(pngs[i].Length);
        bw.Write(offset);
        offset += pngs[i].Length;
    }
    foreach (var p in pngs) bw.Write(p);
}
Console.WriteLine("andinwin.ico yazildi");

// 4) GitHub banner 1280x640
using (var banner = new Bitmap(1280, 640))
using (var g = Graphics.FromImage(banner))
{
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    using var bg = new LinearGradientBrush(new Point(0, 0), new Point(1280, 640),
        Color.FromArgb(15, 23, 42), Color.FromArgb(12, 74, 110));
    g.FillRectangle(bg, 0, 0, 1280, 640);
    // sol: buyuk logo
    var state = g.Save();
    g.TranslateTransform(110, 110);
    g.ScaleTransform(1.65f, 1.65f);
    DrawLogo(g, 256);
    g.Restore(state);
    // sag: yazi (metin alani x=580..1230)
    using var titleFont = new Font("Segoe UI", 88, FontStyle.Bold, GraphicsUnit.Pixel);
    using var subFont = new Font("Segoe UI", 34, FontStyle.Regular, GraphicsUnit.Pixel);
    using var sub2Font = new Font("Segoe UI", 30, FontStyle.Regular, GraphicsUnit.Pixel);
    g.DrawString("AndinWin", titleFont, Brushes.White, 580, 140);
    g.DrawString("Windows \u2013 Android K\u00f6pr\u00fcs\u00fc",
        subFont, new SolidBrush(Color.FromArgb(186, 230, 253)), 584, 262);
    g.DrawString("(WSL2 + Waydroid)",
        sub2Font, new SolidBrush(Color.FromArgb(125, 211, 252)), 584, 312);
    using var pillFont = new Font("Segoe UI", 26, FontStyle.Bold, GraphicsUnit.Pixel);
    var pillText = "APK  \u2022  K\u0131sayol  \u2022  Tray";
    var sz = g.MeasureString(pillText, pillFont);
    using var pill = new SolidBrush(Color.FromArgb(61, 220, 132));
    g.FillRoundedRect(pill, 584, 392, sz.Width + 56, 62, 31);
    g.DrawString(pillText, pillFont, new SolidBrush(Color.FromArgb(15, 23, 42)), 612, 401);
    banner.Save(Path.Combine(assets, "banner.png"), ImageFormat.Png);
}
Console.WriteLine("banner.png yazildi");

static void DrawLogo(Graphics g, int s)
{
    float u = s / 256f; // birim
    var winBlue = Color.FromArgb(0, 120, 212);
    var phoneGreen = Color.FromArgb(61, 220, 132);
    var phoneDark = Color.FromArgb(32, 33, 36);

    // golge
    using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
        g.FillRoundedRect(shadow, 22 * u, 52 * u, 212 * u, 176 * u, 18 * u);

    // pencere govde
    using (var body = new SolidBrush(Color.White))
        g.FillRoundedRect(body, 18 * u, 46 * u, 212 * u, 176 * u, 18 * u);
    using (var pen = new Pen(winBlue, 9 * u))
        g.DrawRoundedRect(pen, 22 * u, 50 * u, 204 * u, 168 * u, 16 * u);

    // baslik cubugu
    using (var bar = new SolidBrush(winBlue))
    {
        var path = GdiExt.RoundedTopRect(22 * u, 50 * u, 204 * u, 44 * u, 16 * u);
        g.FillPath(bar, path);
    }
    // trafik dugmeleri
    using (var w = new Pen(Color.White, 5 * u))
    {
        g.DrawLine(w, 178 * u, 72 * u, 192 * u, 72 * u);                    // _
        g.DrawRectangle(w, 198 * u, 66 * u, 11 * u, 11 * u);                // kare
        g.DrawLine(w, 214 * u, 66 * u, 224 * u, 76 * u);                    // X
        g.DrawLine(w, 224 * u, 66 * u, 214 * u, 76 * u);
    }

    // telefon govde (ortada, one cikan)
    using (var dark = new SolidBrush(phoneDark))
        g.FillRoundedRect(dark, 98 * u, 78 * u, 60 * u, 124 * u, 14 * u);
    // ekran
    using (var scr = new SolidBrush(phoneGreen))
        g.FillRoundedRect(scr, 104 * u, 92 * u, 48 * u, 96 * u, 8 * u);
    // kamera + home
    using (var cam = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
        g.FillEllipse(cam, 123 * u, 82 * u, 6 * u, 6 * u);
    using (var hm = new Pen(Color.FromArgb(120, 255, 255, 255), 3 * u))
        g.DrawLine(hm, 118 * u, 196 * u, 138 * u, 196 * u);
    // ekranda mini uygulama izgara
    using (var dot = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                g.FillEllipse(dot, (110 + c * 14) * u, (104 + r * 16) * u, 9 * u, 9 * u);
    // alt gorev cubugu imasi (mavi)
    using (var task = new SolidBrush(winBlue))
        g.FillRoundedRect(task, 34 * u, 196 * u, 44 * u, 10 * u, 5 * u);
}

static string SvgMaster() => """
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">
  <rect x="22" y="52" width="212" height="176" rx="18" fill="#000" opacity="0.15"/>
  <rect x="18" y="46" width="212" height="176" rx="18" fill="#fff" stroke="#0078D4" stroke-width="9"/>
  <path d="M22 50 h204 v20 a6 6 0 0 1 -6 6 H28 a6 6 0 0 1 -6 -6 Z" fill="#0078D4"/>
  <line x1="178" y1="72" x2="192" y2="72" stroke="#fff" stroke-width="5"/>
  <rect x="198" y="66" width="11" height="11" fill="none" stroke="#fff" stroke-width="3"/>
  <line x1="214" y1="66" x2="224" y2="76" stroke="#fff" stroke-width="4"/>
  <line x1="224" y1="66" x2="214" y2="76" stroke="#fff" stroke-width="4"/>
  <rect x="98" y="78" width="60" height="124" rx="14" fill="#202124"/>
  <rect x="104" y="92" width="48" height="96" rx="8" fill="#3DDC84"/>
  <circle cx="126" cy="85" r="3.5" fill="#fff"/>
  <line x1="118" y1="196" x2="138" y2="196" stroke="#fff" stroke-width="3" opacity="0.6"/>
  <g fill="#fff" opacity="0.9">
    <circle cx="114" cy="108" r="4.5"/><circle cx="128" cy="108" r="4.5"/><circle cx="142" cy="108" r="4.5"/>
    <circle cx="114" cy="124" r="4.5"/><circle cx="128" cy="124" r="4.5"/><circle cx="142" cy="124" r="4.5"/>
    <circle cx="114" cy="140" r="4.5"/><circle cx="128" cy="140" r="4.5"/><circle cx="142" cy="140" r="4.5"/>
  </g>
  <rect x="34" y="196" width="44" height="10" rx="5" fill="#0078D4"/>
</svg>
""";

// GDI+ yardimcilar
static class GdiExt
{
    public static void FillRoundedRect(this Graphics g, Brush b, float x, float y, float w, float h, float r)
    {
        using var p = RoundedPath(x, y, w, h, r);
        g.FillPath(b, p);
    }
    public static void DrawRoundedRect(this Graphics g, Pen p, float x, float y, float w, float h, float r)
    {
        using var path = RoundedPath(x, y, w, h, r);
        g.DrawPath(p, path);
    }
    public static void FillRoundedRect(this Graphics g, Brush b, RectangleF r, float radius)
        => g.FillRoundedRect(b, r.X, r.Y, r.Width, r.Height, radius);
    public static GraphicsPath RoundedPath(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure();
        return p;
    }
    public static GraphicsPath RoundedTopRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddLine(x + w, y + r, x + w, y + h);
        p.AddLine(x + w, y + h, x, y + h);
        p.AddLine(x, y + h, x, y + r);
        p.CloseFigure();
        return p;
    }
}
