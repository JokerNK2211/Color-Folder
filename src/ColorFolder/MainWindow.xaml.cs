using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorFolder.Core;
using Microsoft.Win32;

namespace ColorFolder;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _folders = new();
    private string _color = Palette.DefaultColor;
    private string? _badge;
    private bool _loading = true;

    public MainWindow()
    {
        InitializeComponent();
        FolderList.ItemsSource = _folders;
        _folders.CollectionChanged += (_, _) => DropHint.Visibility = _folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        switch (Settings.Style)
        {
            case IconStyle.Windows: StyleWindows.IsChecked = true; break;
            case IconStyle.Template: StyleTemplate.IsChecked = true; break;
            default: StyleMac.IsChecked = true; break;
        }
        MenuCheck.IsChecked = ShellMenu.IsRegistered;
        PortableCheck.IsChecked = Settings.Portable;
        _loading = false;
        ApplyLanguage();
    }

    public void AddFolder(string path)
    {
        path = Path.GetFullPath(path);
        if (Directory.Exists(path) && !_folders.Contains(path, StringComparer.OrdinalIgnoreCase))
            _folders.Add(path);
        if (_folders.Count == 1)
        {
            var st = FolderIcon.Read(path);
            if (st.Color != null) { _color = st.Color; _badge = st.Badge; BuildPickers(); }
        }
    }

    // ---------- UI text ----------
    private void ApplyLanguage()
    {
        Title = T.AppName;
        FoldersTitle.Text = T.Folders;
        DropHint.Text = T.DropHint;
        AddButton.Content = T.Add;
        RemoveButton.Content = T.Remove;
        StyleTitle.Text = T.Style;
        StyleMac.Content = T.StyleMac;
        StyleWindows.Content = T.StyleWindows;
        StyleTemplate.Content = T.StyleTemplate;
        PickTemplateButton.Content = T.PickTemplate;
        ColorsTitle.Text = T.Colors;
        BadgesTitle.Text = T.Badges;
        MenuCheck.Content = T.ShowInMenu;
        PortableCheck.Content = T.Portable;
        RefreshButton.Content = T.RefreshIcons;
        ImportButton.Content = T.ImportIcon;
        ResetButton.Content = T.RestoreDefault;
        ApplyButton.Content = T.Apply;
        LanguageButton.Content = T.Language;
        DropHint.Visibility = _folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTemplateRow();
        BuildPickers();
    }

    private void UpdateTemplateRow()
    {
        TemplateRow.Visibility = StyleTemplate.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        var t = Settings.TemplatePath;
        TemplateName.Text = t != null && File.Exists(t) ? Path.GetFileName(t) : T.NoTemplate;
    }

    // ---------- color and badge pickers ----------
    private void BuildPickers()
    {
        var style = Settings.Style;
        ColorPanel.Children.Clear();
        foreach (var c in Palette.Colors)
        {
            var b = PickerButton(IconStore.GetSource(style, c.Id, null), T.Thai ? c.Th : c.En, c.Id == _color);
            var id = c.Id;
            b.Click += (_, _) => { _color = id; BuildPickers(); };
            ColorPanel.Children.Add(b);
        }

        BadgePanel.Children.Clear();
        var none = PickerButton(IconStore.GetSource(style, _color, null), T.NoBadge, _badge == null);
        none.Click += (_, _) => { _badge = null; BuildPickers(); };
        BadgePanel.Children.Add(none);
        foreach (var bd in Palette.Badges)
        {
            var b = PickerButton(IconStore.GetSource(style, _color, bd.Id), T.Thai ? bd.Th : bd.En, bd.Id == _badge);
            var id = bd.Id;
            b.Click += (_, _) => { _badge = id; BuildPickers(); };
            BadgePanel.Children.Add(b);
        }

        PreviewImage.Source = LoadIcon(IconStore.GetSource(style, _color, _badge), 256);
    }

    private static Button PickerButton(string icon, string tip, bool selected)
    {
        return new Button
        {
            Content = new Image { Source = LoadIcon(icon, 48), Width = 40, Height = 40 },
            ToolTip = tip,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 0, 6, 6),
            Background = selected ? new SolidColorBrush(Color.FromRgb(0xD6, 0xE8, 0xFF)) : Brushes.White,
            BorderBrush = selected ? new SolidColorBrush(Color.FromRgb(0x3E, 0x63, 0xDD)) : new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xE3)),
            BorderThickness = new Thickness(selected ? 2 : 1),
        };
    }

    private static readonly Dictionary<string, BitmapSource> IconCache = new();

    private static BitmapSource? LoadIcon(string path, int size)
    {
        var key = path + "|" + size;
        if (IconCache.TryGetValue(key, out var hit)) return hit;
        try
        {
            var frames = IcoFile.Read(path);
            var best = frames.Where(f => f.Key >= size).Select(f => f.Value).FirstOrDefault() ?? frames.Last().Value;
            IconCache[key] = best;
            return best;
        }
        catch { return null; }
    }

    // ---------- events ----------
    private void Style_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Settings.Style = StyleWindows.IsChecked == true ? IconStyle.Windows
            : StyleTemplate.IsChecked == true ? IconStyle.Template : IconStyle.Mac;
        if (Settings.Style == IconStyle.Template && Settings.TemplatePath is null) PickTemplate_Click(sender, e);
        UpdateTemplateRow();
        BuildPickers();
        if (ShellMenu.IsRegistered) ShellMenu.Register(); // menu icons follow the style
    }

    private void PickTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = T.TemplateFilter, Title = T.PickTemplate };
        if (dlg.ShowDialog(this) != true) return;
        // Keep our own copy, so the template still works if the original is moved.
        var copy = Path.Combine(Paths.Data, "template" + Path.GetExtension(dlg.FileName).ToLowerInvariant());
        File.Copy(dlg.FileName, copy, true);
        Settings.TemplatePath = copy;
        IconCache.Clear();
        UpdateTemplateRow();
        Run(() => { BuildPickers(); if (ShellMenu.IsRegistered) ShellMenu.Register(); }, null);
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        Settings.Language = T.Thai ? "en" : "th";
        ApplyLanguage();
        if (ShellMenu.IsRegistered) ShellMenu.Register(); // menu text follows the language
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Multiselect = true };
        if (dlg.ShowDialog(this) == true)
            foreach (var f in dlg.FolderNames) AddFolder(f);
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        foreach (var f in FolderList.SelectedItems.Cast<string>().ToList()) _folders.Remove(f);
    }

    private void FolderList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void FolderList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            foreach (var p in paths.Where(Directory.Exists)) AddFolder(p);
    }

    private void Apply_Click(object sender, RoutedEventArgs e) =>
        ForEachFolder(f => Actions.SetColor(f, _color, _badge, Settings.Style), T.Done);

    private void Reset_Click(object sender, RoutedEventArgs e) =>
        ForEachFolder(Actions.Reset, T.Restored);

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (_folders.Count == 0) { Status.Text = T.NoFolders; return; }
        var dlg = new OpenFileDialog { Filter = T.IconFilter, Title = T.ImportIcon };
        if (dlg.ShowDialog(this) != true) return;
        ForEachFolder(f => Actions.SetCustomIcon(f, dlg.FileName), T.Done);
    }

    private void Menu_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Run(() => { if (MenuCheck.IsChecked == true) ShellMenu.Register(); else ShellMenu.Unregister(); }, null);
    }

    private void Portable_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Settings.Portable = PortableCheck.IsChecked == true;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => FolderIcon.RefreshAll();

    private void ForEachFolder(Action<string> action, Func<int, string> message)
    {
        if (_folders.Count == 0) { Status.Text = T.NoFolders; return; }
        int ok = 0;
        var errors = new List<string>();
        foreach (var f in _folders)
        {
            try { action(f); ok++; }
            catch (Exception ex) { errors.Add($"{Path.GetFileName(f)}: {ex.Message}"); }
        }
        Status.Text = message(ok);
        if (errors.Count > 0)
            MessageBox.Show(this, $"{T.Error}:\n" + string.Join("\n", errors), T.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Run(Action a, string? done)
    {
        try { a(); if (done != null) Status.Text = done; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, T.AppName, MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
