using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using VNotch;
using VNotch.Controllers;
using VNotch.Models;
using VNotch.Services;
using VNotch.Services.Spotlight;
using VNotch.TestSupport;
using VNotch.ViewModels;

static class Program
{
    static string Qa => Path.GetDirectoryName(ZuzongIntegration.ConfigPath)!;
    static readonly List<string> Results = new();
    static void Check(bool ok, string message) { Results.Add((ok ? "PASS " : "FAIL ") + message); File.WriteAllLines(Path.Combine(Qa, "checks.txt"), Results); if (!ok) throw new Exception(message); }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    static object? Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    static void Export(FrameworkElement element, string file)
    {
        element.UpdateLayout(); var image = new RenderTargetBitmap(Math.Max(1, (int)element.ActualWidth), Math.Max(1, (int)element.ActualHeight), 96, 96, PixelFormats.Pbgra32); image.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var stream = File.Create(Path.Combine(Qa, file)); encoder.Save(stream);
    }
    [STAThread]
    static int Main(string[] args)
    {
        int exit = 1;
        var thread = new Thread(() =>
        {
            try
            {
                BackgroundTestDesktop.AttachCurrentThread();
                var app = new App(); app.InitializeComponent();
                app.Startup += async (_, _) =>
                {
                    try
                    {
                        var main = App.Services.GetRequiredService<MainWindow>();
                        var media = App.Services.GetRequiredService<IMediaDetectionService>();
                        var watch = Stopwatch.StartNew(); double? firstArt = null;
                        media.MediaChanged += (_, info) => {
                            if (info.Thumbnail != null) firstArt ??= watch.Elapsed.TotalMilliseconds;
                            File.AppendAllText(Path.Combine(Qa,"art-timing.jsonl"),JsonSerializer.Serialize(new{ms=watch.Elapsed.TotalMilliseconds,width=info.Thumbnail?.PixelWidth,height=info.Thumbnail?.PixelHeight,source=info.MediaSource})+Environment.NewLine);
                        };
                        await Task.Delay(6500);
                        var live = Field<MediaInfo?>(main, "_currentMediaInfo");
                        File.WriteAllText(Path.Combine(Qa, "live-media.json"), JsonSerializer.Serialize(new { firstArtMs = firstArt, hasTrack = !string.IsNullOrEmpty(live?.CurrentTrack), source = live?.MediaSource, artWidth = live?.Thumbnail?.PixelWidth, artHeight = live?.Thumbnail?.PixelHeight }));
                        if(args.Contains("--api-only")){await CheckAi(true);exit=0;return;}
                        var lean = Field<NotchSettings>(main,"_settings");
                        Check(lean.ZuzongLightweightDefaultsVersion==1&&!lean.EnableLiveTranslation&&!lean.EnableSmartCrop&&!lean.EnableSubjectBlur&&!lean.EnableSpotlight&&!lean.EnableSpotifyCanvas&&!lean.ZuzongSystemMonitor&&!lean.ZuzongClipboardHistory,"lightweight migration disables resident and AI-heavy optional modules");
                        var changedBack=lean.Clone();changedBack.EnableSpotlight=true;Check(!ZuzongLightweightDefaults.ApplyOnce(changedBack)&&changedBack.EnableSpotlight,"lightweight migration runs once and preserves later user choices");
                        Check(ZuzongIntegration.Tint=="#18000000"&&lean.LiquidGlass.Saturation==0,"default glass tint and saturation are neutral");
                        var probeMutex="Local\\ZuzongRecoveryMutex-"+Guid.NewGuid();using(var dormant=new Mutex(false,probeMutex)){using var guard=new SingleInstanceGuard(probeMutex);Check(guard.TryAcquire(),"unowned existing mutex does not block launching the app");}
                        var safe=typeof(VNotch.Services.Translation.SelectionTextReader).GetMethod("Safely",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(string));
                        Check(safe.Invoke(null,new object[]{new Func<string>(()=>throw new System.ComponentModel.Win32Exception(5))})==null,"UI Automation access denied is contained without terminating app");
                        Check(ZuzongIntegration.ProfileRoot.EndsWith("ZuzongDynamic"), "standalone profile does not use taskbar route");
                        Check(ZuzongIntegration.RestartArguments().Contains("--background") && !ZuzongIntegration.RestartArguments().Contains("--editor"), "restart requires only the standalone executable");
                        Check(SettingsWindow.ZuzongGithubUrl == "https://github.com/zuzongdnt", "support links use Zuzong GitHub");
                        Check(SpotifyBrowserAuth.Challenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk") == "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", "Spotify PKCE RFC 7636 challenge");
                        var auth = SpotifyBrowserAuth.AuthorizationUri(new string('a', 32), "state", "challenge");
                        Check(auth.Host == "accounts.spotify.com" && auth.Query.Contains("code_challenge_method=S256") && auth.Query.Contains("127.0.0.1"), "Spotify auth uses official system browser PKCE loopback redirect");
                        Check(!SpotifyBrowserAuth.IsClientId("invalid") && SpotifyBrowserAuth.IsClientId(new string('a', 32)), "Spotify client ID validated before browser opens");
                        Check(SpotifyBrowserAuth.ParseCallback("/callback?state=s&code=c%2B1")["code"] == "c+1" && SpotifyBrowserAuth.ParseCallback("/callback?state=s&state=t")["state"] == "", "Spotify callback decodes code and rejects duplicate state");
                        Field<System.Windows.Threading.DispatcherTimer>(main, "_zuzongTimer").Stop(); Set(main, "_postAppearInitializationStarted", false); media.Stop(); await Task.Delay(2500);
                        var fake = new VNotch.Tests.Fakes.FakeMediaDetectionService(); Set(main, "_mediaService", fake);
                        var settings = Field<NotchSettings>(main, "_settings").Clone(); settings.DisableMouseLeaveAutoClose = true; settings.ShowMediaArtBackground = true; settings.EnableBlurEffects = true; settings.EnableHelloGreeting = false;
                        await main.SaveZuzongFeatures(settings);
                        var pixels = new byte[240 * 140 * 4]; for (int y = 0; y < 140; y++) for (int x = 0; x < 240; x++) { int i = (y * 240 + x) * 4; pixels[i] = (byte)(x < 120 ? 35 : 220); pixels[i + 1] = (byte)(y < 70 ? 110 : 180); pixels[i + 2] = (byte)(x < 120 ? 230 : 65); pixels[i + 3] = 255; }
                        var art = BitmapSource.Create(240, 140, 96, 96, PixelFormats.Bgra32, null, pixels, 240 * 4); art.Freeze();
                        var info = new MediaInfo { IsAnyMediaPlaying = true, IsPlaying = true, CurrentTrack = "Zuzong Ã‚Â· Video Ã„â€˜ang phÃƒÂ¡t", CurrentArtist = "Ã¡ÂºÂ¢nh tÃ¡Â»Â« Windows", MediaSource = "YouTube", Thumbnail = art, Duration = TimeSpan.FromSeconds(210), Position = TimeSpan.FromSeconds(59) };
                        ((MediaDetectionService)media).SuppressIntermediateYouTubeThumbnail(info, true);
                        Check(ReferenceEquals(info.Thumbnail, art), "widescreen SMTC artwork is published immediately, never blanked for a network fetch");
                        var vm = new MediaViewModel(fake, _ => Task.CompletedTask); vm.Update(info);
                        var player = (VNotch.Controls.MediaPlaybackView)main.FindName("MediaPlayback"); player.Attach(vm, fake);
                        var coordinator = App.Services.GetRequiredService<NotchTransitionCoordinator>();
                        if(!args.Contains("--render-only")) foreach (var widget in new[] { "none", "sysmon" }) foreach (var width in new[] { 320, 480 }) foreach (var style in new[] { "attached", "floating" })
                        {
                            coordinator.RequestView(NotchView.Compact, "QA"); await Task.Delay(750);
                            settings = Field<NotchSettings>(main, "_settings").Clone(); settings.ExpandedWidget = widget; await main.SaveZuzongFeatures(settings);
                            var basic = JsonNode.Parse(File.ReadAllText(ZuzongIntegration.ConfigPath))!; basic["expandedWidth"] = width; basic["style"] = style; basic["glass"] = true; File.WriteAllText(ZuzongIntegration.ConfigPath, basic.ToJsonString()); Call(main, "PollZuzong");
                            Set(main, "_currentMediaInfo", info); Field<ShellViewModel>(main, "_viewModel").Media.Update(info);
                            coordinator.RequestView(NotchView.Media, "QA"); await Task.Delay(1200);
                            var thumbnail = (Image)main.FindName("ThumbnailImage"); thumbnail.Source = art; thumbnail.Visibility = Visibility.Visible;
                            Call(main, "ShowMediaBackground"); await Task.Delay(750); main.UpdateLayout();
                            var content = (FrameworkElement)main.FindName("ExpandedContent"); var container = (FrameworkElement)main.FindName("MediaWidgetContainer");
                            var buttons = new[] { "PrevButton", "PlayPauseButton", "NextButton" }.Select(n => (FrameworkElement)player.FindName(n)).ToArray();
                            var bounds = buttons.Select(b => b.TransformToAncestor(content).TransformBounds(new Rect(0, 0, b.ActualWidth, b.ActualHeight))).ToArray();
                            Check(bounds.All(b => b.Left >= 0 && b.Right <= content.ActualWidth) && bounds[0].Right <= bounds[1].Left + 1 && bounds[1].Right <= bounds[2].Left + 1, $"three transport buttons fit {widget} {width} {style}");
                            Check(widget != "none" || (Grid.GetColumnSpan(container) == 3 && player.ActualWidth > content.ActualWidth * .48 && ((FrameworkElement)main.FindName("MediaRightColumn")).Visibility == Visibility.Collapsed), $"media uses available row without empty side column {widget} {width} {style}");
                            Export((FrameworkElement)main.Content, $"media-{widget}-{width}-{style}.png");
                        }
                        var layoutSettings=Field<NotchSettings>(main,"_settings").Clone();layoutSettings.ExpandedWidget="none";await main.SaveZuzongFeatures(layoutSettings);
                        Set(main,"_isLyricsActive",true);Call(main,"UpdateMediaOnlyLayout");Check(((FrameworkElement)main.FindName("MediaRightColumn")).Visibility==Visibility.Visible&&Grid.GetColumnSpan((UIElement)main.FindName("MediaWidgetContainer"))==1,"lyrics restore their column when auxiliary widget is off");
                        Set(main,"_isLyricsActive",false);Call(main,"UpdateMediaOnlyLayout");Check(Grid.GetColumnSpan((UIElement)main.FindName("MediaWidgetContainer"))==3,"media reclaims empty column after lyrics close");
                        var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent };
                        Call(player, "PlayPauseButton_Click", player, click); await Task.Delay(250); Check(fake.PlayPauseCount == 1 && !vm.IsPlaying, "pause routes through media service");
                        Call(player, "PlayPauseButton_Click", player, click); await Task.Delay(250); Call(player, "NextButton_Click", player, click); await Task.Delay(250); Check(fake.PlayPauseCount == 2 && vm.IsPlaying && fake.NextTrackCount == 1, "resume and next route through media service");
                        File.WriteAllText(Path.Combine(Qa,"notch-command.json"),"{\"type\":\"appearance\"}");Call(main,"PollZuzong");var appearance=Application.Current.Windows.OfType<VNotch.Windows.ZuzongAppearanceWindow>().First(w=>w.IsVisible); await Task.Delay(250); Export(appearance, "appearance.png"); appearance.Close(); Check(true, "standalone appearance settings render");
                        main.OpenZuzongAdvanced();var advanced=Application.Current.Windows.OfType<SettingsWindow>().First(w=>w.IsVisible);
                        var updateFixture=new UpdateFixture();var updateSettings=lean.Clone();updateSettings.AutoCheckUpdates=false;updateSettings.EnableLocalOnlyMode=false;
                        NetworkPrivacy.Current.Apply(updateSettings);
                        var updateWindow=new SettingsWindow(updateSettings,App.Services.GetRequiredService<ISettingsApplicationService>(),updateService:updateFixture); updateWindow.Show();updateWindow.NavigateToSection("Updates");await Task.Delay(500);
                        Check(((TextBlock)updateWindow.FindName("CurrentVersionText")).Text.Contains(AppVersion.Current),"update page displays Zuzong assembly version");
                        Check(((TextBlock)updateWindow.FindName("UpdateStatusText")).Text==Loc.Get("settings.updates.notChecked"),"update page does not claim latest before checking");
                        NetworkPrivacy.Current.Apply(updateSettings);
                        ((Button)updateWindow.FindName("CheckUpdateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);
                        Check(updateFixture.Checks>=1&&updateFixture.Forced,$"actual Check button calls GitHub updater and bypasses cache (calls={updateFixture.Checks}, forced={updateFixture.Forced}, online={NetworkPrivacy.Current.IsAllowed(NetworkFeature.Updates)})");
                        Check(((Button)updateWindow.FindName("DownloadUpdateButton")).Visibility==Visibility.Visible,"new release reveals install action");
                        ((Button)updateWindow.FindName("DownloadUpdateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);
                        Check(updateFixture.Downloads==1,"actual Download button calls verified installer service");
                        Export(updateWindow,"updates.png");updateWindow.Close();
                        advanced.NavigateToSection("Donating"); await Task.Delay(650); Export(advanced, "support.png");
                        Check(((Image)advanced.FindName("ZuzongSupportQr")).Source != null, "supplied support QR loads as embedded asset"); advanced.NavigateToSection("Spotlight"); await Task.Delay(650); Export(advanced, "ai-settings.png"); advanced.Close();
                        var tiktok=(Button)advanced.FindName("SocialWebsite");var github=(Button)advanced.FindName("SocialGitHub");
                        Check(tiktok.Opacity>.9&&github.Opacity>.9&&tiktok.Content is System.Windows.Controls.Viewbox vb&&vb.Child is System.Windows.Shapes.Path,"TikTok uses one vector path and both social icons remain visible");
                        var spotify = new VNotch.Windows.SpotifyBrowserConnectWindow { Owner = main }; spotify.Show(); await Task.Delay(300); Export(spotify, "spotify-setup.png"); spotify.Close(); Check(true, "Spotify browser setup renders without credentials");
                        await CheckAi(args.Contains("--live-api"));
                        exit = 0;
                    }
                    catch (Exception ex) { Results.Add("FAIL " + ex); File.WriteAllLines(Path.Combine(Qa, "checks.txt"), Results); }
                    finally { await App.RequestShutdownAsync(exit); }
                };
                app.Run();
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(Qa, "bootstrap-error.txt"), ex.ToString()); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join(); return exit;
    }

    sealed class UpdateFixture : IUpdateService
    {
        public int Checks, Downloads;
        public bool Forced;
        public string CurrentVersion=>AppVersion.Current;
        public UpdateInfo? LatestUpdateInfo=>new UpdateInfo {Version="99.0.0",IsNewerVersion=true};
        public event EventHandler<UpdateInfo?>? UpdateCheckCompleted {add{} remove{}}
        public Task<UpdateInfo?> CheckForUpdatesAsync(bool includePrereleases=false,bool forceRefresh=false){Checks++;Forced=forceRefresh;return Task.FromResult(LatestUpdateInfo);}
        public Task<IReadOnlyList<UpdateInfo>> GetAllReleasesAsync()=>Task.FromResult<IReadOnlyList<UpdateInfo>>([]);
        public Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo info,IProgress<double>? progress=null,CancellationToken cancellationToken=default){Downloads++;return Task.FromResult(true);}
    }
    static async Task CheckAi(bool live)
    {
        var settings = new NotchSettings { EnableLocalOnlyMode = false, AllowOnlineAi = true, SpotlightAiProvider = "Gemini", SpotlightGeminiApiKey = "test-key", SpotlightGeminiModel = "models/gemini-flash-lite-latest" };
        NetworkPrivacy.Current.Apply(settings);
        using var request = SpotlightAiService.CreateRequest(settings, new[] { new SpotlightAiMessage("user", "test") });
        Check(request.RequestUri!.AbsolutePath.EndsWith("/models/gemini-flash-lite-latest:generateContent") && !request.RequestUri.AbsolutePath.Contains("%2F", StringComparison.OrdinalIgnoreCase), "Gemini model resource normalized once");
        if (!live) return;
        var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZuzongTaskbarGlass", "NotchFull", "Preferences", "settings.json");
        using var saved = JsonDocument.Parse(File.ReadAllText(profile)); settings.SpotlightGeminiApiKey = DataProtection.Unprotect(saved.RootElement.GetProperty("SpotlightGeminiApiKey").GetString());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)); var ai = new SpotlightAiService();
        var models = await ai.ListGeminiModelsAsync(settings, timeout.Token); Check(models.Contains("gemini-flash-lite-latest"), "saved Gemini key lists live available models");
        string answer = await ai.SendAsync(settings, new[] { new SpotlightAiMessage("user", "Reply only OK.") }, timeout.Token); Check(!string.IsNullOrWhiteSpace(answer), "live Gemini chat returns content through app HTTP service");
        var text = new StringBuilder(); await foreach (string part in ai.StreamAsync(settings, new[] { new SpotlightAiMessage("user", "Reply only OK.") }, timeout.Token)) text.Append(part);
        Check(text.Length > 0, "live Gemini streaming chat returns content through app parser");
    }
}
