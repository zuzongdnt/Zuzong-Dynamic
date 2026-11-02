using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VNotch.Services;

namespace VNotch.Windows;

public sealed class SpotifyBrowserConnectWindow : Window
{
    readonly CancellationTokenSource _lifetime = new(TimeSpan.FromMinutes(3));
    public bool Connected { get; private set; }
    public SpotifyBrowserConnectWindow()
    {
        Title = "Spotify · Đăng nhập qua trình duyệt"; Width = 550; Height = 490;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = new SolidColorBrush(Color.FromRgb(16, 17, 20)); Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI");
        var panel = new StackPanel { Margin = new Thickness(28) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Kết nối Spotify", FontSize = 26, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Đăng nhập Google hoặc Facebook trong trình duyệt mặc định. Nhạc qua Windows vẫn hoạt động khi chưa kết nối tài khoản.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 18) });
        var dashboard = new Button { Content = "Mở Spotify Developer Dashboard", Padding = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Left };
        dashboard.Click += (_, _) => SafeLauncher.TryOpenUrl("https://developer.spotify.com/dashboard"); panel.Children.Add(dashboard);
        panel.Children.Add(new TextBlock { Text = "Client ID của ứng dụng Spotify", Margin = new Thickness(0, 18, 0, 6) });
        var id = new TextBox { Text = SpotifyBrowserAuth.ReadClientId(), Padding = new Thickness(10) }; panel.Children.Add(id);
        panel.Children.Add(new TextBlock { Text = "Khai báo Redirect URI này trong ứng dụng Spotify:", Margin = new Thickness(0, 14, 0, 6) });
        var redirect = new TextBox { Text = SpotifyBrowserAuth.RedirectUri, IsReadOnly = true, Padding = new Thickness(8) }; panel.Children.Add(redirect);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray, Margin = new Thickness(0, 16, 0, 14) }; panel.Children.Add(status);
        var connect = new Button { Content = "Đăng nhập bằng trình duyệt", Padding = new Thickness(14, 10, 14, 10), HorizontalAlignment = HorizontalAlignment.Left }; panel.Children.Add(connect);
        connect.Click += async (_, _) =>
        {
            if (!SpotifyBrowserAuth.IsClientId(id.Text.Trim())) { status.Text = "Điền Client ID trong Dashboard trước khi đăng nhập. Client ID là mã công khai, không phải mật khẩu."; return; }
            connect.IsEnabled = false; status.Text = "Đang chờ đăng nhập trong trình duyệt…";
            try { await SpotifyBrowserAuth.ConnectAsync(id.Text.Trim(), _lifetime.Token); Connected = true; status.Text = "Đã kết nối Spotify. Bạn có thể đóng cửa sổ này."; }
            catch (OperationCanceledException) { status.Text = "Đã hết thời gian chờ. Đóng rồi mở lại kết nối để thử tiếp."; }
            catch (Exception ex) { status.Text = ex is InvalidOperationException ? ex.Message : "Chưa kết nối được Spotify. Kiểm tra mạng và cổng 43821 rồi thử lại."; }
            finally { connect.IsEnabled = true; }
        };
        Closed += (_, _) => { _lifetime.Cancel(); _lifetime.Dispose(); };
    }
}
