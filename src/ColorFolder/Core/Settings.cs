using Microsoft.Win32;

namespace ColorFolder.Core;

public enum IconStyle { Windows, Mac, Template }

/// <summary>User settings, stored in HKCU\Software\ColorFolder.</summary>
public static class Settings
{
    private const string Key = @"Software\ColorFolder";

    private static string? Get(string name)
    {
        using var k = Registry.CurrentUser.OpenSubKey(Key);
        return k?.GetValue(name) as string;
    }

    private static void Set(string name, string? value)
    {
        using var k = Registry.CurrentUser.CreateSubKey(Key);
        if (value is null) k.DeleteValue(name, false);
        else k.SetValue(name, value);
    }

    public static IconStyle Style
    {
        get => Enum.TryParse<IconStyle>(Get("Style"), out var s) ? s : IconStyle.Mac;
        set => Set("Style", value.ToString());
    }

    /// <summary>"th" or "en". Defaults to Thai when Windows is set to Thai.</summary>
    public static string Language
    {
        get => Get("Language") ?? (System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "th" ? "th" : "en");
        set => Set("Language", value);
    }

    /// <summary>Copy the icon into the folder itself, so the color shows on other PCs.</summary>
    public static bool Portable
    {
        get => Get("Portable") == "1";
        set => Set("Portable", value ? "1" : "0");
    }

    /// <summary>Full path of the user's own template .ico, if any.</summary>
    public static string? TemplatePath
    {
        get => Get("Template");
        set => Set("Template", value);
    }
}
