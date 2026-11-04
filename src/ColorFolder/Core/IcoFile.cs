using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorFolder.Core;

/// <summary>Read and write .ico files as a set of 32-bit BGRA frames.</summary>
public static class IcoFile
{
    public static readonly int[] StandardSizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

    /// <summary>All frames of an .ico, largest per size, as Bgra32.</summary>
    public static SortedDictionary<int, BitmapSource> Read(string path)
    {
        var result = new SortedDictionary<int, BitmapSource>();
        using var fs = File.OpenRead(path);
        var dec = new IconBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        foreach (var f in dec.Frames)
        {
            var bmp = ToBgra(f);
            int size = bmp.PixelWidth;
            if (!result.TryGetValue(size, out var existing) || existing.Format.BitsPerPixel < f.Format.BitsPerPixel)
                result[size] = bmp;
        }
        return result;
    }

    public static BitmapSource ToBgra(BitmapSource src)
    {
        var b = src.Format == PixelFormats.Bgra32 ? src : new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        b.Freeze();
        return b;
    }

    /// <summary>High quality resize of a square image to n x n.</summary>
    public static BitmapSource Resize(BitmapSource src, int n)
    {
        var dv = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
        using (var dc = dv.RenderOpen())
            dc.DrawImage(src, new Rect(0, 0, n, n));
        var rtb = new RenderTargetBitmap(n, n, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        return ToBgra(rtb);
    }

    /// <summary>Fill in missing standard sizes by scaling down the largest frame.</summary>
    public static SortedDictionary<int, BitmapSource> Complete(SortedDictionary<int, BitmapSource> frames)
    {
        var big = frames[frames.Keys.Max()];
        var all = new SortedDictionary<int, BitmapSource>(frames);
        foreach (var n in StandardSizes)
            if (!all.ContainsKey(n) && n <= big.PixelWidth)
                all[n] = Resize(big, n);
        if (!all.ContainsKey(256) && big.PixelWidth > 256)
            all[256] = Resize(big, 256);
        foreach (var k in all.Keys.Where(k => k > 256).ToList()) all.Remove(k);
        return all;
    }

    /// <summary>Write frames as an .ico with PNG-compressed entries (supported since Windows Vista).</summary>
    public static void Write(string path, IDictionary<int, BitmapSource> frames)
    {
        var pngs = frames.OrderBy(f => f.Key).Select(f =>
        {
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(f.Value));
            using var ms = new MemoryStream();
            enc.Save(ms);
            return (size: f.Key, data: ms.ToArray());
        }).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        using (var w = new BinaryWriter(File.Create(tmp)))
        {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)pngs.Count);
            int offset = 6 + 16 * pngs.Count;
            foreach (var (size, data) in pngs)
            {
                w.Write((byte)(size >= 256 ? 0 : size));
                w.Write((byte)(size >= 256 ? 0 : size));
                w.Write((byte)0); w.Write((byte)0);
                w.Write((ushort)1); w.Write((ushort)32);
                w.Write(data.Length); w.Write(offset);
                offset += data.Length;
            }
            foreach (var (_, data) in pngs) w.Write(data);
        }
        File.Move(tmp, path, true);
    }

    /// <summary>Load any supported image (ico, png, icns, jpg, bmp) as a square Bgra32 image.</summary>
    public static SortedDictionary<int, BitmapSource> LoadAny(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".ico") return Read(path);

        BitmapSource img;
        if (ext == ".icns") img = Icns.LargestImage(path);
        else
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(Path.GetFullPath(path));
            bi.EndInit();
            img = bi;
        }
        img = Square(ToBgra(img));
        return new SortedDictionary<int, BitmapSource> { [img.PixelWidth] = img };
    }

    /// <summary>Pad a non-square image with transparency so it becomes square.</summary>
    private static BitmapSource Square(BitmapSource src)
    {
        if (src.PixelWidth == src.PixelHeight) return src;
        int n = Math.Max(src.PixelWidth, src.PixelHeight);
        var dv = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
        using (var dc = dv.RenderOpen())
            dc.DrawImage(src, new Rect((n - src.PixelWidth) / 2.0, (n - src.PixelHeight) / 2.0, src.PixelWidth, src.PixelHeight));
        var rtb = new RenderTargetBitmap(n, n, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        return ToBgra(rtb);
    }
}

/// <summary>Minimal macOS .icns reader: returns the largest PNG/JPEG2000-free entry.</summary>
public static class Icns
{
    public static BitmapSource LargestImage(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 8 || bytes[0] != 'i' || bytes[1] != 'c' || bytes[2] != 'n' || bytes[3] != 's')
            throw new InvalidDataException("Not an .icns file");
        byte[]? best = null;
        int pos = 8;
        while (pos + 8 <= bytes.Length)
        {
            int len = (bytes[pos + 4] << 24) | (bytes[pos + 5] << 16) | (bytes[pos + 6] << 8) | bytes[pos + 7];
            if (len < 8 || pos + len > bytes.Length) break;
            // PNG signature
            if (len > 16 && bytes[pos + 8] == 0x89 && bytes[pos + 9] == (byte)'P' && bytes[pos + 10] == (byte)'N' && bytes[pos + 11] == (byte)'G')
            {
                if (best is null || len - 8 > best.Length)
                    best = bytes.AsSpan(pos + 8, len - 8).ToArray();
            }
            pos += len;
        }
        if (best is null) throw new InvalidDataException("This .icns has no PNG images. Export it as PNG first.");
        var dec = new PngBitmapDecoder(new MemoryStream(best), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return dec.Frames[0];
    }
}
