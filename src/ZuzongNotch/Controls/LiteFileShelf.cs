using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VNotch.Services;

namespace VNotch.Controls;

public sealed class LiteFileShelf : UserControl, IDisposable
{
    public static readonly DependencyProperty NavigationWidthProperty = DependencyProperty.Register(nameof(NavigationWidth), typeof(double), typeof(LiteFileShelf), new PropertyMetadata(0d));
    public double NavigationWidth { get => (double)GetValue(NavigationWidthProperty); set => SetValue(NavigationWidthProperty, value); }
    public TranslateTransform HeaderTranslate { get; } = new();
    public ContentControl SettingsButtonHost { get; } = new() { HorizontalAlignment = HorizontalAlignment.Right };
    public bool IsContextMenuOpen => false;
    readonly List<string> _files = [];
    readonly StackPanel _items = new();
    readonly TextBlock _title = new() { FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
    readonly TextBlock _empty = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray, Margin = new Thickness(0, 25, 0, 0) };
    readonly TextBlock _error = new() { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap };
    string Store => Path.Combine(ZuzongIntegration.ParentData, "lite-file-shelf.json");
    public LiteFileShelf()
    {
        var root = new DockPanel { Margin = new Thickness(8, 36, 8, 10) };
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12), RenderTransform = HeaderTranslate };
        header.Children.Add(_title); header.Children.Add(SettingsButtonHost); DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        DockPanel.SetDock(_error, Dock.Bottom); root.Children.Add(_error);
        root.Children.Add(new ScrollViewer { Content = _items, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Content = root;
        try { if (File.Exists(Store) && new FileInfo(Store).Length < 65536) _files.AddRange((JsonSerializer.Deserialize<string[]>(File.ReadAllText(Store)) ?? []).Where(IsLocalPath).Distinct(StringComparer.OrdinalIgnoreCase).Take(24)); } catch { }
        Loc.LanguageChanged += ApplyLocalization; ApplyLocalization();
    }
    internal static bool IsLocalPath(string path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal) && (File.Exists(path) || Directory.Exists(path));
    internal static bool Supports(IDataObject data) => data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths && paths.Length <= 24 && paths.All(IsLocalPath);
    public async Task ImportCurrentCategoryDropAsync(IDataObject data)
    {
        if (!Supports(data)) return;
        var paths = (string[])data.GetData(DataFormats.FileDrop);
        foreach (string path in paths) if (_files.Count < 24 && !_files.Contains(path, StringComparer.OrdinalIgnoreCase)) _files.Add(path);
        await SaveAsync(); ApplyLocalization();
    }
    async Task SaveAsync()
    {
        try { string temp = Store + ".new"; await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(_files)); File.Move(temp, Store, true); _error.Text = ""; }
        catch (Exception ex) { _error.Text = ZuzongLite.Text("Không lưu được khay: ", "Could not save shelf: ") + ex.Message; }
    }
    public void ApplyLocalization()
    {
        _title.Text = ZuzongLite.Text("Khay file", "File shelf");
        _empty.Text = ZuzongLite.Text("Kéo file vào đây, rồi kéo sang ứng dụng khác. Chỉ lưu đường dẫn, không sao chép nội dung file.", "Drop files here, then drag them into another app. Only file paths are stored; file contents are not copied.");
        _items.Children.Clear();
        if (_files.Count == 0) { _items.Children.Add(_empty); return; }
        foreach (string file in _files.ToArray())
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8), Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)) };
            var remove = new Button { Content = ZuzongLite.Text("Bỏ khỏi khay", "Remove"), Margin = new Thickness(8), Padding = new Thickness(8, 4, 8, 4) };
            remove.Click += async (_, _) => { _files.Remove(file); await SaveAsync(); ApplyLocalization(); }; DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
            var label = new TextBlock { Text = Path.GetFileName(file.TrimEnd(Path.DirectorySeparatorChar)), ToolTip = file, Foreground = Brushes.White, Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Cursor = Cursors.Hand };
            Point start = default;
            label.PreviewMouseLeftButtonDown += (_, e) => start = e.GetPosition(label);
            label.MouseMove += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed && (e.GetPosition(label) - start).Length > 6 && IsLocalPath(file)) { var payload = new DataObject(DataFormats.FileDrop, new[] { file }); DragDrop.DoDragDrop(label, payload, DragDropEffects.Copy); } };
            row.Children.Add(label); _items.Children.Add(row);
        }
    }
    public void BeginEntrance() => BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
    public void BeginExit() { }
    public bool TryDismissOverlay() => false;
    public bool ContainsMenuPoint(Point point) => false;
    public void Dispose() => Loc.LanguageChanged -= ApplyLocalization;
}
