using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VNotch.Models;
using VNotch.Presenters;
using VNotch.Services;

namespace VNotch;

public class SettingsWindow : Window
{
    readonly ISettingsApplicationService _application;
    readonly IUpdateService _updates;
    readonly bool _ownsUpdates;
    NotchSettings _settings;
    JsonObject _basic;
    readonly Dictionary<string, Slider> _sliders = [];
    readonly Dictionary<string, CheckBox> _checks = [];
    SettingsUpdatePresenter? _presenter;
    StackPanel _body = null!;
    TextBox? _tint;
    ComboBox? _style;
    TextBlock _status = null!;
    string _section = "Appearance";
    bool _building, _saving;
    public event EventHandler<NotchSettings>? SettingsChanged;
    public event EventHandler<UpdateInfo?>? UpdateDetected;
    public event Action? TranslationPreviewRequested { add { } remove { } }
    internal const string ZuzongGithubUrl = "https://github.com/zuzongdnt";
    static string T(string vi, string en) => ZuzongLite.Text(vi, en);

    public SettingsWindow(NotchSettings settings, ISettingsService service, bool isSpotlightHotkeyRegistered = true, IUpdateService? updateService = null, bool prepareForFirstShow = false, Func<string?>? clipboardHotkeyError = null, Func<bool>? invisibleHotkeyRegistered = null)
        : this(settings, new SettingsApplicationService(service), isSpotlightHotkeyRegistered, updateService, prepareForFirstShow, clipboardHotkeyError, invisibleHotkeyRegistered) { }

    public SettingsWindow(NotchSettings settings, ISettingsApplicationService service, bool isSpotlightHotkeyRegistered = true, IUpdateService? updateService = null, bool prepareForFirstShow = false, Func<string?>? clipboardHotkeyError = null, Func<bool>? invisibleHotkeyRegistered = null)
    {
        _application = service; _updates = updateService ?? new ZuzongUpdateService(); _ownsUpdates = updateService == null;
        _settings = settings.Clone(); ZuzongLite.Apply(_settings);
        _basic = JsonNode.Parse(File.ReadAllText(ZuzongIntegration.ConfigPath))!.AsObject();
        Width = 740; Height = 760; MinWidth = 580; MinHeight = 500; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14; Foreground = Brushes.White; Background = new SolidColorBrush(Color.FromRgb(17, 18, 21));
        Loc.LanguageChanged += Rebuild;
        Closed += (_, _) =>
        {
            Loc.LanguageChanged -= Rebuild; _presenter?.Dispose(); if (_ownsUpdates && _updates is IDisposable d) d.Dispose();
            var owner = Owner as MainWindow;
            ZuzongLiteMemory.ReleaseIdleGarbage(() => owner?.CanReleaseLiteCaches != false);
        };
        Rebuild();
    }

    internal static int GetEffectiveGpuPreference(NotchSettings settings) => 0;
    internal static void ApplyGpuPreference(int preference) { /* Lite lets Windows select the graphics adapter. */ }
    public void ActivatePreparedWindow(NotchSettings settings, bool snapshotMatches, bool isSpotlightHotkeyRegistered) { _settings = settings.Clone(); ZuzongLite.Apply(_settings); Rebuild(); }
    public void NavigateToTranslation() => NavigateToSection("Features");
    public void SetSpotlightHotkeyStatus(bool registered) { }
    public async Task ToggleInvisibleFromShortcutAsync() { _settings.InvisibleMode = !_settings.InvisibleMode; await SaveAsync(); }
    public void NavigateToSection(string section) { CaptureDraft(); _section = section is "Updates" or "Donating" or "Features" ? section : "Appearance"; Rebuild(); }

    void Rebuild()
    {
        if (_building) return;
        _building = true;
        _presenter?.Dispose(); _presenter = null; _sliders.Clear(); _checks.Clear(); _tint = null; _style = null;
        Title = ZuzongLite.ProductName + " · " + T("Cài đặt", "Settings");
        var root = new DockPanel { Margin = new Thickness(24) };
        var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = ZuzongLite.ProductName, FontSize = 24, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var language = new ComboBox { ItemsSource = new[] { "English", "Tiếng Việt" }, SelectedIndex = Loc.CurrentLanguage == "vi" ? 1 : 0, MinWidth = 120, Padding = new Thickness(8), Background = Brushes.White, Foreground = Brushes.Black };
        System.Windows.Automation.AutomationProperties.SetName(language, T("Ngôn ngữ", "Language")); Grid.SetColumn(language, 1); header.Children.Add(language);
        language.SelectionChanged += (_, _) => { if (_building) return; CaptureDraft(); _settings.Language = language.SelectedIndex == 1 ? "vi" : "en"; Loc.SetLanguage(_settings.Language); SettingsChanged?.Invoke(this, _settings.Clone()); };
        DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var tabs = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        foreach (var item in new[] { ("Appearance", T("Giao diện", "Appearance")), ("Features", T("Tính năng", "Features")), ("Updates", T("Cập nhật", "Updates")), ("Donating", T("Thông tin & ủng hộ", "About & support")) })
        {
            var button = Button(item.Item2, () => NavigateToSection(item.Item1));
            if (item.Item1 == _section) button.FontWeight = FontWeights.Bold;
            tabs.Children.Add(button);
        }
        DockPanel.SetDock(tabs, Dock.Top); root.Children.Add(tabs);
        var footer = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        _status = new TextBlock { Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
        footer.Children.Add(_status);
        var actions = new WrapPanel();
        actions.Children.Add(Button(T("Lưu", "Save"), async () => await SaveAsync()));
        actions.Children.Add(Button(T("Đóng cài đặt", "Close settings"), Close));
        footer.Children.Add(actions); footer.Children.Add(new TextBlock { Text = T("Đóng cài đặt vẫn giữ Notch chạy nền. Thoát app qua biểu tượng khay hệ thống.", "Closing settings keeps the Notch running. Quit from the system tray."), Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        _body = new StackPanel { Margin = new Thickness(0, 0, 14, 0) }; root.Children.Add(new ScrollViewer { Content = _body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }); Content = root;
        switch (_section) { case "Features": BuildFeatures(); break; case "Updates": BuildUpdates(); break; case "Donating": BuildSupport(); break; default: BuildAppearance(); break; }
        _building = false;
    }

    static Button Button(string label, Action action)
    {
        var b = new Button { Content = label, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 10, 8), Foreground = Brushes.Black, Background = new SolidColorBrush(Color.FromRgb(235, 237, 240)), Cursor = System.Windows.Input.Cursors.Hand };
        b.Click += (_, _) => action(); return b;
    }
    void Hint(string text) => _body.Children.Add(new TextBlock { Text = text, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18) });
    void Row(string label, FrameworkElement control)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 14) }; row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        row.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
        Grid.SetColumn(control, 1); row.Children.Add(control); _body.Children.Add(row); System.Windows.Automation.AutomationProperties.SetName(control, label);
    }
    void Check(string key, string title, bool value)
    {
        var check = new CheckBox { IsChecked = value, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        _checks[key] = check; Row(title, check);
    }
    void Slider(string key, string title, double min, double max, double value)
    {
        var slider = new Slider { Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), TickFrequency = 1, IsSnapToTickEnabled = true, Width = 160, VerticalAlignment = VerticalAlignment.Center };
        var valueText = new TextBlock { Text = slider.Value.ToString("0"), Width = 35, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        slider.ValueChanged += (_, _) => valueText.Text = slider.Value.ToString("0");
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; panel.Children.Add(slider); panel.Children.Add(valueText); _sliders[key] = slider; Row(title, panel);
    }
    bool Flag(string key, bool fallback = false) => _basic[key]?.GetValue<bool>() ?? fallback;
    int Number(string key, int fallback) => _basic[key]?.GetValue<int>() ?? fallback;

    void BuildAppearance()
    {
        Hint(T("Kính lỏng giữ nền thật và khúc xạ. Tắt kính để dùng nền đen nhẹ hơn.", "Liquid glass refracts the real backdrop. Turn glass off for a lighter black background."));
        _style = new ComboBox { ItemsSource = new[] { T("Sát mép trên", "Attached to top edge"), T("Viên thuốc nổi", "Floating pill") }, SelectedIndex = _basic["style"]?.GetValue<string>() == "floating" ? 1 : 0, Padding = new Thickness(6), Foreground = Brushes.Black, Background = Brushes.White }; Row(T("Kiểu Notch", "Notch shape"), _style);
        Slider("width", T("Chiều rộng khi thu", "Compact width"), 180, 420, Number("width", 230));
        Slider("expandedWidth", T("Chiều rộng khi mở", "Expanded width"), 320, 760, Number("expandedWidth", 480));
        Slider("top", T("Khoảng cách mép trên", "Distance from top"), 0, 140, Number("top", 0));
        Check("glass", T("Kính lỏng", "Liquid glass"), Flag("glass"));
        _tint = new TextBox { Text = _basic["tintColor"]?.GetValue<string>() ?? "#18000000", Padding = new Thickness(7), Foreground = Brushes.Black, Background = Brushes.White }; Row(T("Màu kính · #AARRGGBB", "Glass tint · #AARRGGBB"), _tint);
        Slider("blur", T("Độ mờ", "Blur"), 0, 28, Number("blur", 3));
        Slider("refraction", T("Khúc xạ", "Refraction"), 0, 120, Number("refraction", 32));
        Slider("bezelWidth", T("Độ dày mép kính", "Glass edge width"), 1, 80, Number("bezelWidth", 16));
        Slider("rimHighlight", T("Ánh sáng viền", "Edge lighting"), 0, 100, Number("rimHighlight", 35));
        Check("showLogo", T("Hiện logo Z", "Show Z logo"), Flag("showLogo"));
        Check("hover", T("Mở khi rê chuột", "Expand on hover"), Flag("hover", true));
        Check("hideFullscreen", T("Ẩn khi toàn màn hình", "Hide during full screen"), Flag("hideFullscreen"));
        Check("startup", T("Chạy khi mở máy", "Start with Windows"), _settings.AutoStart);
    }
    void BuildFeatures()
    {
        Hint(T("Lite giữ nhạc, đồng hồ, pin, khay file, hẹn giờ và mixer. Các mô-đun không cần thiết đã được loại khỏi giao diện.", "Lite keeps music, clock, battery, file shelf, timer and mixer. Unneeded modules have been removed from settings."));
        Check("music", T("Nhạc và điều khiển phát", "Music and playback controls"), _settings.ZuzongMusic);
        Check("files", T("Khay kéo thả file", "Drag-and-drop file shelf"), _settings.VisibleNavTabs.Split(',').Contains("Secondary"));
        Check("timer", T("Hẹn giờ", "Timer"), _settings.VisibleNavTabs.Split(',').Contains("Timer"));
        Check("mixer", T("Mixer âm lượng ứng dụng", "App volume mixer"), _settings.VisibleNavTabs.Split(',').Contains("AudioMixer"));
        Check("battery", T("Pin và thông báo pin", "Battery and battery notices"), _settings.ShowBatteryIndicator);
        Check("clipboardNotifications", T("Thông báo đã sao chép", "Copied notification"), Flag("clipboardNotifications", true));
        Check("codexNotifications", T("Thông báo Codex hoàn tất", "Codex completion notification"), Flag("codexNotifications", true));
        Check("animations", T("Chuyển động mở và thu", "Expand and collapse animation"), _settings.EnableAnimations);
    }
    void BuildUpdates()
    {
        Hint(T("Phiên bản hiện tại: ", "Current version: ") + AppVersion.Current);
        Check("online", T("Cho phép kết nối GitHub để cập nhật", "Allow GitHub connections for updates"), !_settings.EnableLocalOnlyMode);
        Check("autoUpdates", T("Tự kiểm tra cập nhật nền", "Check for updates in background"), _settings.AutoCheckUpdates);
        Check("beta", T("Nhận bản thử nghiệm beta", "Receive beta releases"), _settings.IncludePrereleaseUpdates);
        var status = new TextBlock { Text = _settings.EnableLocalOnlyMode ? Loc.Get("settings.updates.offline") : Loc.Get("settings.updates.notChecked"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 16) }; _body.Children.Add(status);
        var check = Button(T("Kiểm tra", "Check"), async () => { CaptureDraft(); await SaveAsync(); if (_presenter != null) await _presenter.CheckForUpdatesAsync(true); });
        var install = Button(T("Tải và cài đặt", "Download and install"), async () => { if (_presenter != null) await _presenter.DownloadUpdateAsync(); }); install.Visibility = Visibility.Collapsed;
        _body.Children.Add(check); _body.Children.Add(install);
        _presenter = new SettingsUpdatePresenter(_updates, new SettingsUpdateViewRefs(this, status, check, install), includePrereleases: () => _settings.IncludePrereleaseUpdates);
        _presenter.UpdateDetected += (_, info) => UpdateDetected?.Invoke(this, info);
        _body.Children.Add(Button(T("Lịch sử thay đổi", "Release notes"), () => _presenter.ShowChangelog()));
        _body.Children.Add(Button(T("Báo lỗi trên GitHub", "Report an issue on GitHub"), () => SafeLauncher.TryOpenUrl(ZuzongUpdateService.RepositoryUrl + "/issues")));
    }
    void BuildSupport()
    {
        _body.Children.Add(new TextBlock { Text = "Zuzong · Dương Nhứt Thịnh", FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
        Hint(T("Notch độc lập cho Windows. Dựa trên V-Notch của w0otdev theo Apache-2.0.", "Standalone Windows Notch. Based on V-Notch by w0otdev under Apache-2.0."));
        _body.Children.Add(Button("GitHub · zuzongdnt", () => SafeLauncher.TryOpenUrl(ZuzongGithubUrl)));
        _body.Children.Add(Button("TikTok · @_thinhdzuong_", () => SafeLauncher.TryOpenUrl("https://www.tiktok.com/@_thinhdzuong_")));
        Hint(T("Ủng hộ tác giả · Tài khoản 1050374824", "Support the author · Account 1050374824"));
        _body.Children.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/ZuzongDynamic;component/Assets/ZuzongSupportQr.png")), Width = 210, Height = 225, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left });
    }
    void CaptureDraft()
    {
        if (_building) return;
        foreach (var pair in _sliders) _basic[pair.Key] = (int)pair.Value.Value;
        if (_style != null) _basic["style"] = _style.SelectedIndex == 1 ? "floating" : "attached";
        if (_tint != null) _basic["tintColor"] = _tint.Text.Trim();
        foreach (var pair in _checks)
        {
            bool value = pair.Value.IsChecked == true;
            switch (pair.Key)
            {
                case "startup": _settings.AutoStart = value; break;
                case "music": _settings.ZuzongMusic = value; _basic["media"] = value; break;
                case "files": SetTab("Secondary", value); _basic["files"] = value; break;
                case "timer": SetTab("Timer", value); break;
                case "mixer": SetTab("AudioMixer", value); break;
                case "battery": _settings.ShowBatteryIndicator = value; _basic["battery"] = value; break;
                case "animations": _settings.EnableAnimations = value; break;
                case "online": _settings.EnableLocalOnlyMode = !value; break;
                case "autoUpdates": _settings.AutoCheckUpdates = value; break;
                case "beta": _settings.IncludePrereleaseUpdates = value; break;
                default: _basic[pair.Key] = value; break;
            }
        }
        ZuzongLite.Apply(_settings);
    }
    void SetTab(string tab, bool enabled)
    {
        var tabs = _settings.VisibleNavTabs.Split(',').ToHashSet(); if (enabled) tabs.Add(tab); else tabs.Remove(tab);
        _settings.VisibleNavTabs = string.Join(",", new[] { "Media", "Secondary", "Timer", "AudioMixer" }.Where(tabs.Contains));
    }
    async Task SaveAsync()
    {
        if (_saving) return; _saving = true;
        try
        {
            CaptureDraft();
            var tint = _basic["tintColor"]?.GetValue<string>() ?? "#18000000";
            if (tint.Length != 9 || tint[0] != '#' || !tint[1..].All(Uri.IsHexDigit)) throw new FormatException(T("Màu kính cần đúng dạng #AARRGGBB.", "Glass tint must use #AARRGGBB."));
            _ = ColorConverter.ConvertFromString(tint);
            string before = File.ReadAllText(ZuzongIntegration.ConfigPath);
            string temp = ZuzongIntegration.ConfigPath + ".lite-new";
            await File.WriteAllTextAsync(temp, _basic.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, ZuzongIntegration.ConfigPath, true);
            bool startup = _settings.AutoStart;
            try { ZuzongIntegration.MapCustomSettings(_settings); _settings.AutoStart = startup; await _application.ApplyAsync(_settings); }
            catch { File.WriteAllText(temp, before); File.Move(temp, ZuzongIntegration.ConfigPath, true); throw; }
            NetworkPrivacy.Current.Apply(_settings);
            SettingsChanged?.Invoke(this, _settings.Clone());
            _status.Text = T("Đã lưu.", "Saved.");
        }
        catch (Exception ex) { _status.Text = T("Chưa lưu được: ", "Could not save: ") + ex.Message; }
        finally { _saving = false; }
    }
}
