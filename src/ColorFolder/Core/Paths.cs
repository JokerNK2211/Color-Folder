using System.IO;

namespace ColorFolder.Core;

public static class Paths
{
    /// <summary>Folder next to the exe that holds the bundled icon set.</summary>
    public static string BundledIcons => Path.Combine(AppContext.BaseDirectory, "icons");

    /// <summary>Per-user data: %LOCALAPPDATA%\ColorFolder</summary>
    public static string Data
    {
        get
        {
            var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ColorFolder");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    /// <summary>Icons that folders point at. Kept here so they survive app updates and moves.</summary>
    public static string AppliedIcons => Ensure(Path.Combine(Data, "icons"));

    /// <summary>Icons made from the user's own template.</summary>
    public static string TemplateIcons => Ensure(Path.Combine(Data, "template"));

    /// <summary>Icons the user imported (PNG/ICNS/ICO).</summary>
    public static string ImportedIcons => Ensure(Path.Combine(Data, "imported"));

    /// <summary>List of folders this app has changed, used by "Reset all".</summary>
    public static string History => Path.Combine(Data, "folders.txt");

    public static string Exe => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "ColorFolder.exe");

    private static string Ensure(string p)
    {
        Directory.CreateDirectory(p);
        return p;
    }
}
