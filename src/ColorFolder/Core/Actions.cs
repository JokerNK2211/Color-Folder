using System.IO;

namespace ColorFolder.Core;

/// <summary>The operations behind both the right-click menu and the main window.</summary>
public static class Actions
{
    public static void SetColor(string folder, string color, string? badge, IconStyle style)
    {
        var src = IconStore.GetSource(style, color, badge);
        FolderIcon.Set(folder, IconStore.Stable(src), new FolderState(color, badge, style, null));
    }

    /// <summary>Change only the color, keeping the folder's current badge.</summary>
    public static void SetColorKeepBadge(string folder, string color)
    {
        var cur = FolderIcon.Read(folder);
        SetColor(folder, color, cur.Badge, Settings.Style);
    }

    /// <summary>Change only the badge (or remove it with null), keeping the folder's current color.</summary>
    public static void SetBadgeKeepColor(string folder, string? badge)
    {
        var cur = FolderIcon.Read(folder);
        SetColor(folder, cur.Color ?? Palette.DefaultColor, badge, Settings.Style);
    }

    public static void SetCustomIcon(string folder, string iconFile)
    {
        var ico = Path.GetExtension(iconFile).Equals(".ico", StringComparison.OrdinalIgnoreCase)
            ? iconFile
            : IconStore.Import(iconFile);
        FolderIcon.Set(folder, IconStore.Stable(ico), new FolderState(null, null, null, Path.GetFileName(ico)));
    }

    public static void Reset(string folder) => FolderIcon.Reset(folder);

    public static int ResetAll()
    {
        int n = 0;
        foreach (var f in History.All())
        {
            try { if (Directory.Exists(f)) { FolderIcon.Reset(f); n++; } else History.Remove(f); }
            catch { /* skip folders we can no longer write to */ }
        }
        return n;
    }
}
