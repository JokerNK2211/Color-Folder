using Microsoft.Win32;

namespace ColorFolder.Core;

/// <summary>
/// Registers the "Color Folder" cascading menu for folders, per user (no admin).
/// On Windows 11 it shows under "Show more options".
/// </summary>
public static class ContextMenu
{
    private const string Root = @"Software\Classes\Directory\shell\ColorFolder";

    public static bool IsRegistered
    {
        get { using var k = Registry.CurrentUser.OpenSubKey(Root); return k != null; }
    }

    public static void Register()
    {
        Unregister();
        var exe = Paths.Exe;
        var style = Settings.Style;

        using var root = Registry.CurrentUser.CreateSubKey(Root);
        root.SetValue("MUIVerb", T.AppName);
        root.SetValue("SubCommands", "");
        root.SetValue("Icon", MenuIcon(style, Palette.DefaultColor, null));

        // Colors
        using (var colors = root.CreateSubKey(@"shell\01colors"))
        {
            colors.SetValue("MUIVerb", T.Colors);
            colors.SetValue("SubCommands", "");
            colors.SetValue("Icon", MenuIcon(style, "red", null));
            int i = 1;
            foreach (var c in Palette.Colors)
                Leaf(colors, $"{i++:00}{c.Id}", T.Thai ? c.Th : c.En, MenuIcon(style, c.Id, null), $"\"{exe}\" --color {c.Id} \"%1\"").Dispose();
        }

        // Badges (keep the folder's color)
        using (var badges = root.CreateSubKey(@"shell\02badges"))
        {
            badges.SetValue("MUIVerb", T.Badges);
            badges.SetValue("SubCommands", "");
            badges.SetValue("Icon", MenuIcon(style, Palette.DefaultColor, "star"));
            Leaf(badges, "00none", T.NoBadge, MenuIcon(style, Palette.DefaultColor, null), $"\"{exe}\" --badge none \"%1\"").Dispose();
            int i = 1;
            foreach (var b in Palette.Badges)
                Leaf(badges, $"{i++:00}{b.Id}", T.Thai ? b.Th : b.En, MenuIcon(style, Palette.DefaultColor, b.Id), $"\"{exe}\" --badge {b.Id} \"%1\"").Dispose();
        }

        Leaf(root, "03pick", T.ChooseIcon, null, $"\"{exe}\" --pick \"%1\"", single: true).Dispose();
        Leaf(root, "04reset", T.RestoreDefault, null, $"\"{exe}\" --reset \"%1\"").Dispose();
        using (var open = Leaf(root, "05open", T.OpenApp, exe + ",0", $"\"{exe}\" --open \"%1\"", single: true))
            open.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord); // separator before this item
    }

    public static void Unregister() => Registry.CurrentUser.DeleteSubKeyTree(Root, false);

    private static RegistryKey Leaf(RegistryKey parent, string key, string text, string? icon, string command, bool single = false)
    {
        var k = parent.CreateSubKey(@"shell\" + key);
        k.SetValue("MUIVerb", text);
        if (icon != null) k.SetValue("Icon", icon);
        // "Player" lets the item work when many folders are selected (one run per folder).
        k.SetValue("MultiSelectModel", single ? "Single" : "Player");
        using var cmd = k.CreateSubKey("command");
        cmd.SetValue("", command);
        return k;
    }

    private static string MenuIcon(IconStyle style, string color, string? badge)
    {
        try { return IconStore.Stable(IconStore.GetSource(style, color, badge)); }
        catch { return Paths.Exe + ",0"; }
    }
}
