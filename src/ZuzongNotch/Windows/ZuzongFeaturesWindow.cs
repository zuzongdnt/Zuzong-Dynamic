using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using VNotch.Models;

namespace VNotch.Windows;
internal sealed class ZuzongFeaturesWindow:Window {
 readonly MainWindow main;
 NotchSettings current;
 readonly TextBlock status=new(){Text="Thay đổi được lưu ngay. Tắt phần không dùng để giảm tác vụ nền.",Foreground=new SolidColorBrush(Color.FromRgb(169,183,201)),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,18)};
 bool saving;
 public ZuzongFeaturesWindow(MainWindow owner,NotchSettings snapshot){
  main=owner;current=snapshot;Title="Zuzong · Tính năng Dynamic Notch";Width=650;Height=720;MinWidth=520;MinHeight=480;WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=new FontFamily("Segoe UI");FontSize=14;Background=new SolidColorBrush(Color.FromRgb(17,20,27));Foreground=Brushes.White;
  var checkboxStyle=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='CheckBox'><Setter Property='MinHeight' Value='34'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><Grid Background='Transparent'><Grid.ColumnDefinitions><ColumnDefinition Width='24'/><ColumnDefinition/></Grid.ColumnDefinitions><Border x:Name='Box' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='16' Height='16' CornerRadius='4' BorderBrush='#8191A5' BorderThickness='1' Background='Transparent' VerticalAlignment='Center'><Path x:Name='Tick' Data='M2,6 L5,9 L11,2' Stroke='#132337' StrokeThickness='2' Visibility='Collapsed' Width='12' Height='11'/></Border><ContentPresenter Grid.Column='1' VerticalAlignment='Center' Margin='8,0,0,0'/></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Box' Property='Background' Value='#A7D6F7'/><Setter TargetName='Tick' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Box' Property='BorderBrush' Value='White'/><Setter TargetName='Box' Property='BorderThickness' Value='2'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
  var panel=new StackPanel{Margin=new Thickness(24)};Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  panel.Children.Add(new TextBlock{Text="Tính năng Dynamic Notch",FontSize=24,FontWeight=FontWeights.SemiBold});panel.Children.Add(status);
  void Heading(string text)=>panel.Children.Add(new TextBlock{Text=text,FontSize=17,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,20,0,9)});
  void Check(string title,Func<NotchSettings,bool> get,Action<NotchSettings,bool> set){var box=new CheckBox{Content=title,IsChecked=get(current),Foreground=Brushes.White,Margin=new Thickness(0,0,0,6),Style=checkboxStyle,Padding=new Thickness(0)};panel.Children.Add(box);box.Click+=async(_,_)=>{if(saving){box.IsChecked=get(current);return;}bool enabled=box.IsChecked==true;var next=current.Clone();set(next,enabled);saving=true;box.IsEnabled=false;try{await main.SaveZuzongFeatures(next);current=next;status.Text="Đã lưu. Mô-đun đang dùng cấu hình mới.";}catch(Exception e){box.IsChecked=get(current);status.Text="Chưa lưu được: "+e.Message;}finally{saving=false;box.IsEnabled=true;}};}
  void Property(string title,string name)=>Check(title,s=>(bool)typeof(NotchSettings).GetProperty(name)!.GetValue(s)!, (s,v)=>typeof(NotchSettings).GetProperty(name)!.SetValue(s,v));
  void Tab(string title,string token)=>Check(title,s=>s.VisibleNavTabs.Split(',').Contains(token)&&(token!="Camera"||!s.HideCamera),(s,v)=>{var tabs=s.VisibleNavTabs.Split(',',StringSplitOptions.RemoveEmptyEntries).ToHashSet();if(v)tabs.Add(token);else tabs.Remove(token);s.VisibleNavTabs=string.Join(",",new[]{"Media","Secondary","Timer","AudioMixer","Camera"}.Where(t=>tabs.Contains(t)));if(token=="Camera")s.HideCamera=!v;});
  Heading("Nhạc và các trang");
  Property("Nhận nhạc, điều khiển phát và sóng nhạc","ZuzongMusic");Tab("Khay kéo thả file / clipboard","Secondary");Property("Lưu lịch sử clipboard (văn bản, ảnh, file)","ZuzongClipboardHistory");Tab("Hẹn giờ / đếm ngược","Timer");Tab("Mixer âm lượng theo ứng dụng","AudioMixer");Tab("Trang camera (chỉ mở camera khi dùng)","Camera");
  Heading("Thông tin và tiện ích");
  Property("Pin và thông báo pin","ShowBatteryIndicator");Property("Bluetooth và thông báo thiết bị","ZuzongBluetooth");Property("Lịch","ZuzongCalendar");Property("Theo dõi CPU / RAM / GPU","ZuzongSystemMonitor");Property("Chỉ báo quyền riêng tư camera / mic","EnablePrivacyIndicators");Property("Âm thanh thư giãn / White Noise","ZuzongWhiteNoise");Property("Thời tiết (cần bật kết nối)","EnableWeather");Property("Spotlight: tìm app, file, tính toán và AI","EnableSpotlight");Property("Dịch trực tiếp","EnableLiveTranslation");
  Heading("Nội dung và hiệu ứng");
  Property("Lời bài hát Spotify","EnableSpotifyLyrics");Property("Tra lời bài hát trực tuyến","EnableOnlineLyrics");Property("Phụ đề YouTube","EnableYouTubeSubtitles");Property("Spotify Canvas","EnableSpotifyCanvas");Property("Cắt ảnh bìa thông minh","EnableSmartCrop");Property("Làm mờ hậu cảnh chủ thể","EnableSubjectBlur");Property("Điều khiển bằng cử chỉ","EnableGestureControls");Property("Chào khi mở máy / mở khóa","EnableHelloGreeting");Property("Âm thanh tương tác","EnableInteractionSounds");Property("Chuyển động giao diện","EnableAnimations");Property("Mở khi rê chuột","EnableHoverExpand");
  Heading("Kết nối và chẩn đoán");
  Property("Chỉ dùng cục bộ (tắt để dùng tính năng trực tuyến)","EnableLocalOnlyMode");Property("Đọc URL trình duyệt để nhận dạng nội dung","EnableBrowserUrlInspection");Property("Ghi log chẩn đoán","EnableDiagnosticLogging");
  panel.Children.Add(new TextBlock{Text="Màu, kính lỏng, logo, vị trí và thông báo Codex/sao chép ở tab Dynamic Notch của bộ chỉnh. API/model/tài khoản và chi tiết từng tính năng nằm trong Cài đặt nâng cao.",Foreground=new SolidColorBrush(Color.FromRgb(169,183,201)),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,20,0,0)});
 }
}
