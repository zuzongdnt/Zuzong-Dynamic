using System.Diagnostics;
using System.IO;
using System.Reflection;
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
using VNotch.Controls;
using VNotch.Models;
using VNotch.Services;
using VNotch.TestSupport;
using VNotch.ViewModels;

static class Program
{
    static string Root => ZuzongIntegration.ParentData;
    static readonly List<string> Results = [];
    static void Check(bool value, string name) { Results.Add((value ? "PASS " : "FAIL ") + name); File.WriteAllLines(Path.Combine(Root, "checks.txt"), Results); if (!value) throw new Exception(name); }
    static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    static object? Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(value, args);
    static IEnumerable<DependencyObject> Tree(DependencyObject root) { yield return root; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var item in Tree(VisualTreeHelper.GetChild(root, i))) yield return item; }
    static void Export(FrameworkElement view, string name)
    {
        view.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(Root, name)); encoder.Save(stream);
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
                        var main = App.Services.GetRequiredService<MainWindow>(); await Task.Delay(5500);
                        var s = Field<NotchSettings>(main, "_settings");
                        Check(!s.EnableSpotifyCanvas && !s.EnableOnlineLyrics && !s.EnableSpotifyLyrics && !s.EnableYouTubeSubtitles && !s.ShowMediaArtBackground && !s.ZuzongClipboardHistory, "all four removed feature groups are blocked at runtime");
                        var legacy = new NotchSettings { EnableSpotifyCanvas = true, EnableOnlineLyrics = true, ShowMediaArtBackground = true, ZuzongClipboardHistory = true, Language = "ja" }; ZuzongLite.Apply(legacy);
                        Check(!legacy.EnableSpotifyCanvas && !legacy.EnableOnlineLyrics && !legacy.ShowMediaArtBackground && !legacy.ZuzongClipboardHistory && legacy.Language == "en", "old full profiles cannot reactivate removed features or languages");
                        Check(Field<ClipboardHistoryController?>(main, "_clipboardHistory") == null && Field<LiveTranslationController?>(main, "_liveTranslation") == null, "no resident clipboard store or translation controller");
                        Check(!Tree(main).OfType<MediaElement>().Any(), "main visual tree contains no video decoder controls");
                        Check(Loc.GetAvailableLanguages().Select(x => x.Code).Order().SequenceEqual(new[] { "en", "vi" }), "only English and Vietnamese are offered");
                        Check(typeof(App).Assembly.GetManifestResourceNames().Count(n => n.StartsWith("VNotch.Locales.")) == 2, "only two language resources are embedded");
                        var process = Process.GetCurrentProcess(); process.Refresh(); File.WriteAllText(Path.Combine(Root, "memory-before-settings.json"), JsonSerializer.Serialize(new { workingSet = process.WorkingSet64, privateBytes = process.PrivateMemorySize64 }));
                        Field<System.Windows.Threading.DispatcherTimer>(main, "_zuzongTimer").Stop();
                        var media = App.Services.GetRequiredService<IMediaDetectionService>(); media.Stop(); await Task.Delay(600);
                        var fakeMedia = new VNotch.Tests.Fakes.FakeMediaDetectionService();
                        main.GetType().GetField("_mediaService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, fakeMedia);
                        main.GetType().GetField("_postAppearInitializationStarted", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, false);
                        var info = new MediaInfo { IsAnyMediaPlaying = true, IsPlaying = true, CurrentTrack = "Lite playback test", CurrentArtist = "Zuzong", MediaSource = "YouTube", Duration = TimeSpan.FromMinutes(3), Position = TimeSpan.FromSeconds(25) };
                        var vm = new MediaViewModel(fakeMedia, _ => Task.CompletedTask); vm.Update(info);
                        var player = (MediaPlaybackView)main.FindName("MediaPlayback"); player.Attach(vm, fakeMedia);
                        var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent };
                        Call(player, "PlayPauseButton_Click", player, click); await Task.Delay(300); Call(player, "PlayPauseButton_Click", player, click); await Task.Delay(300); Call(player, "NextButton_Click", player, click); await Task.Delay(300);
                        Check(fakeMedia.PlayPauseCount == 2 && fakeMedia.NextTrackCount == 1, $"pause, resume and next still reach media transport (toggle={fakeMedia.PlayPauseCount}, next={fakeMedia.NextTrackCount})");
                        Check(Grid.GetColumnSpan((UIElement)main.FindName("MediaWidgetContainer")) == 3, "media uses the full expanded width without a lyrics column");
                        var coordinator = App.Services.GetRequiredService<NotchTransitionCoordinator>();
                        coordinator.RequestView(NotchView.Media, "LiteQA"); await Task.Delay(700); Export(main, "media.png"); coordinator.RequestView(NotchView.Compact, "LiteQA"); await Task.Delay(700);
                        var shelf = new LiteFileShelf();
                        var text = new DataObject(DataFormats.UnicodeText, "Do not capture this text"); Check(!LiteFileShelf.Supports(text), "file shelf refuses clipboard text");
                        string file = Path.Combine(Root, "original-file.txt"); File.WriteAllText(file, "original user content");
                        var drop = new DataObject(DataFormats.FileDrop, new[] { file }); await shelf.ImportCurrentCategoryDropAsync(drop);
                        Check(File.ReadAllText(file) == "original user content" && JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(Root, "lite-file-shelf.json")))!.Single() == file, "file shelf persists only the original path without copying or changing the file");
                        shelf.Dispose();
                        var application = new FakeSettingsApplication(s);
                        var updates = new FakeUpdates();
                        var window = new SettingsWindow(s, application, updateService: updates); window.Show(); await Task.Delay(300);
                        foreach (string language in new[] { "vi", "en" })
                        {
                            Loc.SetLanguage(language); await Task.Delay(150);
                            foreach (string section in new[] { "Appearance", "Features", "Updates", "Donating" })
                            {
                                window.NavigateToSection(section); await Task.Delay(150); Export(window, language + "-" + section + ".png");
                                string content = string.Join(" ", Tree(window).OfType<TextBlock>().Select(t => t.Text));
                                Check(!content.Contains("Spotify Canvas") && !content.Contains("Spotlight") && !content.Contains("WebView"), language + " " + section + " omits removed and unavailable options");
                            }
                            Check(window.Title.Contains(language == "vi" ? "Cài đặt" : "Settings"), language + " window title follows the selected language");
                        }
                        window.NavigateToSection("Updates"); window.UpdateLayout(); await Task.Delay(150);
                        Field<Dictionary<string, CheckBox>>(window, "_checks")["online"].IsChecked = true;
                        NetworkPrivacy.Current.Apply(new NotchSettings { EnableLocalOnlyMode = false });
                        var updateCheck = Tree(window).OfType<Button>().Single(b => (string?)b.Content == "Check"); updateCheck.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(250);
                        Check(updates.Checks == 1, "Lite update button saves choices and calls the real updater interface");
                        var install = Tree(window).OfType<Button>().Single(b => (string?)b.Content == "Download and install"); Check(install.Visibility == Visibility.Visible, "new release exposes the install action"); install.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(150); Check(updates.Installs == 1, "Lite install button reaches verified installer service");
                        window.NavigateToSection("Appearance"); window.UpdateLayout(); await Task.Delay(150); var tint = Tree(window).OfType<TextBox>().Single(); tint.Text = "invalid"; Call(window, "SaveAsync"); await Task.Delay(150); Check(!File.ReadAllText(ZuzongIntegration.ConfigPath).Contains("invalid"), "invalid glass tint cannot overwrite the saved configuration");
                        window.Close();
                        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name is "LLamaSharp" or "Microsoft.ML.OnnxRuntime" or "Microsoft.Web.WebView2.Wpf" or "LanguageDetection.Ai"), "no local AI, ONNX or embedded browser assembly is loaded");
                        File.AppendAllText(Path.Combine(Root, "checks.txt"), $"TOTAL {Results.Count} PASS{Environment.NewLine}"); exit = 0;
                    }
                    catch (Exception ex) { File.AppendAllText(Path.Combine(Root, "checks.txt"), "FAIL " + ex + Environment.NewLine); }
                    finally { await App.RequestShutdownAsync(exit); }
                };
                app.Run();
            }
            catch (Exception ex) { Directory.CreateDirectory(Root); File.WriteAllText(Path.Combine(Root, "bootstrap-error.txt"), ex.ToString()); }
        }); thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join(); return exit;
    }
    sealed class FakeSettingsApplication(NotchSettings settings) : ISettingsApplicationService
    {
        public event EventHandler<NotchSettings>? SettingsApplied { add { } remove { } }
        public NotchSettings Load() => settings.Clone();
        public Task ApplyAsync(NotchSettings value, CancellationToken ct = default) => Task.CompletedTask;
        public bool IsAutoStartEnabled() => settings.AutoStart;
        public Task<(NotchSettings Settings, bool RequiresRestart)> ImportAsync(string file, NotchSettings? currentSettings = null, CancellationToken ct = default) => Task.FromResult((settings.Clone(), false));
        public void Export(string file, NotchSettings value) { }
    }
    sealed class FakeUpdates : IUpdateService
    {
        public int Checks, Installs;
        public string CurrentVersion => AppVersion.Current;
        public UpdateInfo? LatestUpdateInfo => new() { Version = "99.0.0", IsNewerVersion = true };
        public event EventHandler<UpdateInfo?>? UpdateCheckCompleted { add { } remove { } }
        public Task<UpdateInfo?> CheckForUpdatesAsync(bool includePrereleases = false, bool forceRefresh = false) { Checks++; return Task.FromResult(LatestUpdateInfo); }
        public Task<IReadOnlyList<UpdateInfo>> GetAllReleasesAsync() => Task.FromResult<IReadOnlyList<UpdateInfo>>([]);
        public Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default) { Installs++; return Task.FromResult(true); }
    }
}
