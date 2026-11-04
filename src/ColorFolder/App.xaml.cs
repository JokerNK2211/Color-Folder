using System.IO;
using System.Windows;
using ColorFolder.Core;
using Microsoft.Win32;

namespace ColorFolder;

/// <summary>
/// Command line (used by the right-click menu and the installer):
///   --color &lt;id&gt; "folder"      set color, keep badge
///   --badge &lt;id|none&gt; "folder" set badge, keep color
///   --icon "file" "folder"      use an icon file
///   --pick "folder"             choose an icon file in a dialog
///   --reset "folder"            restore the normal icon
///   --open "folder"             open the main window with this folder
///   --register / --unregister   add or remove the right-click menu
///   --reset-all                 restore every folder this app changed (asks first)
/// No arguments opens the main window.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var a = e.Args;
        try
        {
            if (a.Length == 0) { ShowMain(null); return; }
            switch (a[0])
            {
                case "--color" when a.Length >= 3:
                    Actions.SetColorKeepBadge(a[2], a[1]);
                    break;
                case "--badge" when a.Length >= 3:
                    Actions.SetBadgeKeepColor(a[2], a[1] == "none" ? null : a[1]);
                    break;
                case "--icon" when a.Length >= 3:
                    Actions.SetCustomIcon(a[2], a[1]);
                    break;
                case "--pick" when a.Length >= 2:
                    var dlg = new OpenFileDialog { Filter = T.IconFilter, Title = T.ChooseIcon };
                    if (dlg.ShowDialog() == true) Actions.SetCustomIcon(a[1], dlg.FileName);
                    break;
                case "--reset" when a.Length >= 2:
                    Actions.Reset(a[1]);
                    break;
                case "--open":
                    ShowMain(a.Length >= 2 ? a[1] : null);
                    return;
                case "--register":
                    ContextMenu.Register();
                    break;
                case "--unregister":
                    ContextMenu.Unregister();
                    break;
                case "--reset-all":
                    if (History.All().Count > 0 &&
                        MessageBox.Show(T.ResetAllQuestion, T.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Actions.ResetAll();
                    break;
                default:
                    if (Directory.Exists(a[0])) { ShowMain(a[0]); return; }
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{T.Error}:\n{ex.Message}", T.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Shutdown();
    }

    private void ShowMain(string? folder)
    {
        var w = new MainWindow();
        if (folder != null) w.AddFolder(folder);
        w.Closed += (_, _) => Shutdown();
        w.Show();
    }
}
