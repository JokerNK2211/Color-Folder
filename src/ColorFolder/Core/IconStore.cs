using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ColorFolder.Core;

/// <summary>Finds or builds the .ico for a color/badge/style and keeps a stable copy in LocalAppData.</summary>
public static class IconStore
{
    /// <summary>Path of the bundled (or template-generated) icon, for previews and menus.</summary>
    public static string GetSource(IconStyle style, string color, string? badge)
    {
        string name = badge is null ? color : $"{color}-{badge}";
        switch (style)
        {
            case IconStyle.Windows:
                return Path.Combine(Paths.BundledIcons, "windows", name + ".ico");
            case IconStyle.Mac:
                return Path.Combine(Paths.BundledIcons, "mac", name + ".ico");
            default:
                var template = Settings.TemplatePath;
                if (template is null || !File.Exists(template))
                    return GetSource(IconStyle.Mac, color, badge); // no template yet: fall back
                var dir = Path.Combine(Paths.TemplateIcons, TemplateKey(template));
                var path = Path.Combine(dir, name + ".ico");
                if (!File.Exists(path))
                {
                    var c = Palette.FindColor(color) ?? Palette.FindColor(Palette.DefaultColor)!;
                    var frames = IcoFile.Complete(IcoFile.LoadAny(template));
                    IcoFile.Write(path, Recolor.Apply(frames, c.Color, badge));
                }
                return path;
        }
    }

    /// <summary>Copy the icon to a stable per-user location and return that path.</summary>
    public static string Stable(string source)
    {
        // Name includes a content hash, so a changed icon gets a new path and Explorer's cache can't show the old one.
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)))[..10].ToLowerInvariant();
        var dest = Path.Combine(Paths.AppliedIcons, $"{Path.GetFileNameWithoutExtension(source)}-{hash}.ico");
        if (!File.Exists(dest)) File.Copy(source, dest);
        return dest;
    }

    /// <summary>Convert a PNG/ICNS/ICO the user picked into an .ico in the imported folder.</summary>
    public static string Import(string file)
    {
        var frames = IcoFile.Complete(IcoFile.LoadAny(file));
        var name = Path.GetFileNameWithoutExtension(file);
        foreach (var ch in Path.GetInvalidFileNameChars()) name = name.Replace(ch, '_');
        var dest = Path.Combine(Paths.ImportedIcons, name + ".ico");
        IcoFile.Write(dest, frames);
        return dest;
    }

    public static IEnumerable<string> Imported() =>
        Directory.EnumerateFiles(Paths.ImportedIcons, "*.ico").OrderBy(f => f);

    private static string TemplateKey(string template)
    {
        var bytes = File.ReadAllBytes(template);
        return Convert.ToHexString(SHA256.HashData(bytes))[..12].ToLowerInvariant();
    }
}
