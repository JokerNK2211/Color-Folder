using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ColorFolder.Core;

/// <summary>What this app applied to a folder, stored in the folder's desktop.ini.</summary>
public record FolderState(string? Color, string? Badge, IconStyle? Style, string? CustomIcon);

/// <summary>Sets and clears folder icons the same way Explorer's "Change Icon" does.</summary>
public static class FolderIcon
{
    private const string Section = "ColorFolder";
    private const string PortableName = ".colorfolder.ico";

    public static void Set(string folder, string iconPath, FolderState state)
    {
        folder = Path.GetFullPath(folder);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);

        string iconRef = iconPath;
        var portableFile = Path.Combine(folder, PortableName);
        if (Settings.Portable)
        {
            if (File.Exists(portableFile)) File.SetAttributes(portableFile, FileAttributes.Normal);
            File.Copy(iconPath, portableFile, true);
            File.SetAttributes(portableFile, FileAttributes.Hidden | FileAttributes.System);
            iconRef = PortableName; // relative path: travels with the folder
        }
        else if (File.Exists(portableFile))
        {
            File.SetAttributes(portableFile, FileAttributes.Normal);
            File.Delete(portableFile);
        }

        var fcs = new SHFOLDERCUSTOMSETTINGS
        {
            dwSize = (uint)Marshal.SizeOf<SHFOLDERCUSTOMSETTINGS>(),
            dwMask = FCSM_ICONFILE,
            pszIconFile = iconRef,
            cchIconFile = 0,
            iIconIndex = 0,
        };
        int hr = SHGetSetFolderCustomSettings(ref fcs, folder, FCS_FORCEWRITE);
        Marshal.ThrowExceptionForHR(hr);

        var ini = Path.Combine(folder, "desktop.ini");
        Write(ini, "Color", state.Color);
        Write(ini, "Badge", state.Badge);
        Write(ini, "Style", state.Style?.ToString());
        Write(ini, "CustomIcon", state.CustomIcon);
        History.Add(folder);
        Notify(folder);
    }

    public static void Reset(string folder)
    {
        folder = Path.GetFullPath(folder);
        var fcs = new SHFOLDERCUSTOMSETTINGS
        {
            dwSize = (uint)Marshal.SizeOf<SHFOLDERCUSTOMSETTINGS>(),
            dwMask = FCSM_ICONFILE,
            pszIconFile = null,
        };
        SHGetSetFolderCustomSettings(ref fcs, folder, FCS_FORCEWRITE);

        var ini = Path.Combine(folder, "desktop.ini");
        if (File.Exists(ini))
        {
            WritePrivateProfileString(Section, null, null, ini); // remove our section
            foreach (var key in new[] { "IconResource", "IconFile", "IconIndex" })
                WritePrivateProfileString(".ShellClassInfo", key, null, ini);
            RemoveIfEmpty(ini);
        }
        var portableFile = Path.Combine(folder, PortableName);
        if (File.Exists(portableFile))
        {
            File.SetAttributes(portableFile, FileAttributes.Normal);
            File.Delete(portableFile);
        }
        History.Remove(folder);
        Notify(folder);
    }

    public static FolderState Read(string folder)
    {
        var ini = Path.Combine(folder, "desktop.ini");
        if (!File.Exists(ini)) return new FolderState(null, null, null, null);
        IconStyle? style = Enum.TryParse<IconStyle>(ReadValue(ini, "Style"), out var s) ? s : null;
        return new FolderState(ReadValue(ini, "Color"), ReadValue(ini, "Badge"), style, ReadValue(ini, "CustomIcon"));
    }

    /// <summary>Delete desktop.ini if nothing but icon settings are left in it.</summary>
    private static void RemoveIfEmpty(string ini)
    {
        var lines = File.ReadAllLines(ini)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith(';'))
            .ToList();
        bool onlyIcon = lines.All(l =>
            l.Equals("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase));
        if (onlyIcon)
        {
            File.SetAttributes(ini, FileAttributes.Normal);
            File.Delete(ini);
        }
    }

    private static void Write(string ini, string key, string? value) =>
        WritePrivateProfileString(Section, key, value, ini);

    private static string? ReadValue(string ini, string key)
    {
        var sb = new StringBuilder(1024);
        GetPrivateProfileString(Section, key, "", sb, sb.Capacity, ini);
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static void Notify(string folder)
    {
        var p = Marshal.StringToHGlobalUni(folder);
        try
        {
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, p, IntPtr.Zero);
            SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, p, IntPtr.Zero);
        }
        finally { Marshal.FreeHGlobal(p); }
    }

    /// <summary>Ask Explorer to drop its cached icons (used by "Refresh icons").</summary>
    public static void RefreshAll() => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

    // ---- Win32 ----
    private const uint FCSM_ICONFILE = 0x00000010;
    private const uint FCS_FORCEWRITE = 0x00000002;
    private const int SHCNE_UPDATEDIR = 0x00001000;
    private const int SHCNE_UPDATEITEM = 0x00002000;
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_PATHW = 0x0005;
    private const uint SHCNF_FLUSHNOWAIT = 0x3000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFOLDERCUSTOMSETTINGS
    {
        public uint dwSize;
        public uint dwMask;
        public IntPtr pvid;
        public string? pszWebViewTemplate;
        public uint cchWebViewTemplate;
        public string? pszWebViewTemplateVersion;
        public string? pszInfoTip;
        public uint cchInfoTip;
        public IntPtr pclsid;
        public uint dwFlags;
        public string? pszIconFile;
        public uint cchIconFile;
        public int iIconIndex;
        public string? pszLogo;
        public uint cchLogo;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetSetFolderCustomSettings(ref SHFOLDERCUSTOMSETTINGS pfcs, string pszPath, uint dwReadWrite);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WritePrivateProfileString(string lpAppName, string? lpKeyName, string? lpString, string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
}

/// <summary>Folders this app has changed, so uninstall can offer to reset them.</summary>
public static class History
{
    private static readonly object Lock = new();

    public static IReadOnlyList<string> All()
    {
        lock (Lock)
            return File.Exists(Paths.History) ? File.ReadAllLines(Paths.History).Where(l => l.Length > 0).Distinct().ToList() : new List<string>();
    }

    public static void Add(string folder) => Update(l => { if (!l.Contains(folder, StringComparer.OrdinalIgnoreCase)) l.Add(folder); });

    public static void Remove(string folder) => Update(l => l.RemoveAll(x => string.Equals(x, folder, StringComparison.OrdinalIgnoreCase)));

    private static void Update(Action<List<string>> change)
    {
        // Several processes can run at once (one per selected folder), so retry on sharing errors.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                lock (Lock)
                {
                    using var fs = new FileStream(Paths.History, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    var list = new StreamReader(fs).ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    change(list);
                    fs.SetLength(0);
                    using var w = new StreamWriter(fs);
                    foreach (var l in list) w.WriteLine(l);
                }
                return;
            }
            catch (IOException) { Thread.Sleep(50 + attempt * 50); }
        }
    }
}
