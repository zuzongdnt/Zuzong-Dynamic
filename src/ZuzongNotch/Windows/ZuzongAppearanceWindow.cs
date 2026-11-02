using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VNotch.Services;

namespace VNotch.Windows;

public sealed class ZuzongAppearanceWindow : Window
{
    readonly MainWindow _main;
    readonly StackPanel _form = new();
    readonly Dictionary<string, Slider> _sliders = new();
    readonly Dictionary<string, CheckBox> _switches = new();
    readonly ComboBox _style;
    readonly TextBox _tint;
    readonly TextBlock _status;
    readonly CheckBox _startup;

    public ZuzongAppearanceWindow(MainWindow main)
    {
        _main = main;
        Title = "Zuzong Dynamic · Giao diện";
        Width = 540; Height = 760; MinWidth = 460; MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(15, 16, 19));
        Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var config = JsonNode.Parse(File.ReadAllText(ZuzongIntegration.ConfigPath))!.AsObject();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _form };
        _form.Margin = new Thickness(28, 22, 28, 24); Content = scroll;
        _form.Children.Add(new TextBlock { Text = "Zuzong Dynamic", FontSize = 28, FontWeight = FontWeights.SemiBold });
        _form.Children.Add(new TextBlock { Text = "Notch độc lập cho Windows", Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 24) });
        var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
        actions.Children.Add(Button("Tính năng", () => main.OpenZuzongFeatures()));
        actions.Children.Add(Button("Cài đặt nâng cao", () => main.OpenZuzongAdvanced()));
        _form.Children.Add(actions);
        _style = new ComboBox { ItemsSource = new[] { "Sát mép trên", "Viên thuốc nổi" }, SelectedIndex = config["style"]?.GetValue<string>() == "floating" ? 1 : 0, MinWidth = 170, Padding = new Thickness(6), HorizontalAlignment = HorizontalAlignment.Right };
        Row("Kiểu Notch", _style);
        Slider("width", "Chiều rộng khi thu", 180, 420, Number(config, "width", 230));
        Slider("expandedWidth", "Chiều rộng khi mở", 320, 760, Number(config, "expandedWidth", 480));
        Slider("top", "Khoảng cách mép trên", 0, 140, Number(config, "top", 0));
        Switch("glass", "Kính lỏng", config["glass"]?.GetValue<bool>() ?? false);
        _tint = new TextBox { Text = config["tintColor"]?.GetValue<string>() ?? "#18000000", Width = 150, Padding = new Thickness(7), HorizontalAlignment = HorizontalAlignment.Right };
        Row("Màu kính · #AARRGGBB", _tint);
        Slider("blur", "Độ mờ", 0, 28, Number(config, "blur", 3));
        Slider("refraction", "Khúc xạ", 0, 120, Number(config, "refraction", 32));
        Slider("bezelWidth", "Độ dày mép kính", 1, 80, Number(config, "bezelWidth", 16));
        Slider("rimHighlight", "Ánh sáng viền", 0, 100, Number(config, "rimHighlight", 35));
        Switch("showLogo", "Hiện logo Z", config["showLogo"]?.GetValue<bool>() ?? false);
        Switch("hover", "Mở khi rê chuột", config["hover"]?.GetValue<bool>() ?? true);
        Switch("hideFullscreen", "Ẩn khi toàn màn hình", config["hideFullscreen"]?.GetValue<bool>() ?? false);
        _startup = new CheckBox { IsChecked = StartupManager.IsAutoStartEnabled(), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        Row("Chạy khi mở máy", _startup);
        _form.Children.Add(new TextBlock { Text = "Đóng bảng cài đặt vẫn giữ Notch chạy ở khay hệ thống. Chọn Thoát trong menu khay để dừng app.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray, Margin = new Thickness(0, 6, 0, 4) });
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray, Margin = new Thickness(0, 14, 0, 10) };
        _form.Children.Add(_status);
        var bottom = new WrapPanel();
        bottom.Children.Add(Button("Lưu giao diện", Apply));
        bottom.Children.Add(Button("Đóng cài đặt", Close));
        _form.Children.Add(bottom);
    }

    static int Number(JsonObject config, string key, int fallback) => config[key]?.GetValue<int>() ?? fallback;
    static Button Button(string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(14, 9, 14, 9), Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush(Color.FromRgb(232, 234, 239)), Foreground = Brushes.Black, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        button.Click += (_, _) => action();
        return button;
    }
    void Row(string label, FrameworkElement input)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
        System.Windows.Automation.AutomationProperties.SetName(input, label);
        Grid.SetColumn(input, 1); row.Children.Add(input); _form.Children.Add(row);
    }
    void Slider(string key, string label, int min, int max, int value)
    {
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, IsSnapToTickEnabled = true, TickFrequency = 1, Width = 155, VerticalAlignment = VerticalAlignment.Center };
        var text = new TextBlock { Text = value.ToString(), Width = 35, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
        slider.ValueChanged += (_, _) => text.Text = ((int)slider.Value).ToString();
        var group = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; group.Children.Add(slider); group.Children.Add(text);
        _sliders[key] = slider; Row(label, group);
    }
    void Switch(string key, string label, bool value)
    {
        var check = new CheckBox { IsChecked = value, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        _switches[key] = check; Row(label, check);
    }
    void Apply()
    {
        try
        {
            string tint = _tint.Text.Trim();
            if (tint.Length != 7 && tint.Length != 9) throw new FormatException();
            _ = ColorConverter.ConvertFromString(tint);
            var config = JsonNode.Parse(File.ReadAllText(ZuzongIntegration.ConfigPath))!.AsObject();
            foreach (var (key, slider) in _sliders) config[key] = (int)slider.Value;
            foreach (var (key, check) in _switches) config[key] = check.IsChecked == true;
            config["style"] = _style.SelectedIndex == 1 ? "floating" : "attached";
            config["tintColor"] = tint; config["followTaskbar"] = false;
            string temp = ZuzongIntegration.ConfigPath + ".appearance-new";
            File.WriteAllText(temp, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, ZuzongIntegration.ConfigPath, true);
            StartupManager.SetAutoStart(_startup.IsChecked == true);
            _main.ReloadZuzongAppearance();
            _status.Text = "Đã lưu. Notch dùng giao diện mới ngay.";
        }
        catch (FormatException) { _status.Text = "Màu chưa hợp lệ. Ví dụ: #55172438 hoặc #172438."; }
        catch (Exception) { _status.Text = "Chưa lưu được giao diện. Kiểm tra quyền ghi rồi thử lại."; }
    }
}
