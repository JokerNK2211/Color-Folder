using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorFolder.Core;

/// <summary>
/// Recolors a folder icon template into any color, keeping its shape and texture.
/// Port of tools/recolor_template.py.
/// </summary>
public static class Recolor
{
    public static SortedDictionary<int, BitmapSource> Apply(SortedDictionary<int, BitmapSource> template, Color target, string? badge)
    {
        var baseHsv = BaseOf(template[template.Keys.Max()]);
        var outFrames = new SortedDictionary<int, BitmapSource>();
        foreach (var (size, frame) in template)
        {
            var px = Pixels(frame);
            RecolorPixels(px, baseHsv, target);
            if (badge != null) Emboss(px, size, badge, target);
            outFrames[size] = FromPixels(px, size);
        }
        return outFrames;
    }

    private static byte[] Pixels(BitmapSource b)
    {
        var px = new byte[b.PixelWidth * b.PixelHeight * 4];
        b.CopyPixels(px, b.PixelWidth * 4, 0);
        return px;
    }

    private static BitmapSource FromPixels(byte[] px, int n)
    {
        var b = BitmapSource.Create(n, n, 96, 96, PixelFormats.Bgra32, null, px, n * 4);
        b.Freeze();
        return b;
    }

    private static (double h, double s, double v) BaseOf(BitmapSource frame)
    {
        var px = Pixels(frame);
        var hues = new List<double>();
        for (int i = 0; i < px.Length; i += 4)
        {
            if (px[i + 3] < 230) continue;
            var (h, s, _) = ToHsv(px[i + 2], px[i + 1], px[i]);
            if (s > 0.3) hues.Add(h);
        }
        if (hues.Count == 0) return (0.55, 0.9, 0.9);
        hues.Sort();
        double hm = hues[hues.Count / 2];
        // Treat the template as already showing the closest palette color, so that color looks identical.
        (double d, double s, double v) best = (double.MaxValue, 0.9, 0.9);
        foreach (var c in Palette.Colors)
        {
            var (ph, ps, pv) = ToHsv(c.Color.R, c.Color.G, c.Color.B);
            if (ps < 0.3) continue;
            double d = Math.Min(Math.Abs(ph - hm), 1 - Math.Abs(ph - hm));
            if (d < best.d) best = (d, ps, pv);
        }
        return (hm, best.s, best.v);
    }

    private static void RecolorPixels(byte[] px, (double h, double s, double v) b, Color target)
    {
        var (th, ts, tv) = ToHsv(target.R, target.G, target.B);
        for (int i = 0; i < px.Length; i += 4)
        {
            if (px[i + 3] == 0) continue;
            double r = px[i + 2] / 255.0, g = px[i + 1] / 255.0, bl = px[i] / 255.0;
            var (_, s, v) = ToHsv(px[i + 2], px[i + 1], px[i]);
            double w = Math.Clamp((s - 0.12) / 0.2, 0, 1); // how "colored" this pixel is
            if (w <= 0) continue;
            double ns = Math.Clamp(s * ts / b.s, 0, 1);
            double nv = Math.Clamp(v * tv / b.v, 0, 1);
            var (nr, ng, nb) = FromHsv(th, ns, nv);
            px[i + 2] = (byte)Math.Round((r * (1 - w) + nr * w) * 255);
            px[i + 1] = (byte)Math.Round((g * (1 - w) + ng * w) * 255);
            px[i] = (byte)Math.Round((bl * (1 - w) + nb * w) * 255);
        }
    }

    private static readonly Dictionary<string, BitmapSource> MaskCache = new();

    private static void Emboss(byte[] px, int n, string badge, Color target)
    {
        var maskPath = Path.Combine(Paths.BundledIcons, "badges", badge + ".png");
        if (!File.Exists(maskPath)) return;
        if (!MaskCache.TryGetValue(badge, out var mask))
        {
            var dec = new PngBitmapDecoder(new Uri(maskPath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            mask = IcoFile.ToBgra(dec.Frames[0]);
            MaskCache[badge] = mask;
        }
        int side = (int)(n * 0.43);
        if (side < 4) return;
        var m = Pixels(IcoFile.Resize(mask, side));
        int x0 = (n - side) / 2, y0 = (int)(n * 0.31), drop = Math.Max(1, n / 150);
        var ink = (r: target.R * 0.62, g: target.G * 0.62, b: target.B * 0.62);
        Blend(px, n, m, side, x0, y0 + drop, (255, 255, 255), 0.5);
        Blend(px, n, m, side, x0, y0, ink, 0.85);
    }

    private static void Blend(byte[] px, int n, byte[] mask, int side, int x0, int y0, (double r, double g, double b) c, double strength)
    {
        for (int y = 0; y < side; y++)
        for (int x = 0; x < side; x++)
        {
            int tx = x0 + x, ty = y0 + y;
            if (tx < 0 || ty < 0 || tx >= n || ty >= n) continue;
            double a = mask[(y * side + x) * 4 + 3] / 255.0 * strength;
            if (a <= 0) continue;
            int i = (ty * n + tx) * 4;
            double da = px[i + 3] / 255.0;
            if (da <= 0) continue; // only draw on the folder itself
            px[i + 2] = (byte)(px[i + 2] * (1 - a) + c.r * a);
            px[i + 1] = (byte)(px[i + 1] * (1 - a) + c.g * a);
            px[i] = (byte)(px[i] * (1 - a) + c.b * a);
        }
    }

    public static (double h, double s, double v) ToHsv(byte r8, byte g8, byte b8)
    {
        double r = r8 / 255.0, g = g8 / 255.0, b = b8 / 255.0;
        double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)), d = mx - mn;
        double h = 0;
        if (d > 1e-6)
        {
            if (mx == r) h = ((g - b) / d % 6 + 6) % 6;
            else if (mx == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h /= 6;
        }
        return (h, mx > 1e-6 ? d / mx : 0, mx);
    }

    public static (double r, double g, double b) FromHsv(double h, double s, double v)
    {
        int i = (int)Math.Floor(h * 6) % 6;
        double f = h * 6 - Math.Floor(h * 6);
        double p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
        return i switch
        {
            0 => (v, t, p), 1 => (q, v, p), 2 => (p, v, t),
            3 => (p, q, v), 4 => (t, p, v), _ => (v, p, q),
        };
    }
}
