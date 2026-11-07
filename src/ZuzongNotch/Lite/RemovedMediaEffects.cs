using System.Windows.Media;
using System.Windows.Media.Imaging;
using VNotch.Controllers;
using VNotch.Models;
using VNotch.Services;

namespace VNotch;

// These transition hooks keep the shared shell layout stable without constructing
// lyric clients, video decoders, album blur workers, caches or their timers.
public partial class MainWindow
{
    private enum SyncedTextSource { None, SpotifyLyrics, YouTubeSubtitles }
    private bool _isLyricsActive { get => false; set { } }
    private bool _isLyricsSearchVisible => false;
    private bool _isSpotifyCanvasMediaOpen => false;
    private SyncedTextSource _syncedTextSource { get => SyncedTextSource.None; set { } }
    private List<LyricLine>? _currentLyrics => null;
    private int _currentLyricIndex { get => -1; set { } }
    private string _lyricsTrackKey { get => ""; set { } }
    private string _lyricsProvider => "";
    private string _lastKnownYouTubeVideoId { get => ""; set { } }
    private LyricsService? _lyricsService => null;
    private YouTubeSubtitleService? _youtubeSubtitleService => null;
    private SpotifyCanvasController? _spotifyCanvasController => null;
    private UiScheduledTimer? _mediaBackgroundRecoveryTimer { get => null; set { } }
    private UiScheduledTimer? _blurDissolveDebounce => null;
    private BitmapSource? _lastBackgroundThumbnail { get => null; set { } }
    private BitmapSource? _lastBlurThumbnailRef { get => null; set { } }
    private BitmapSource? _pendingBlurResult { get => null; set { } }
    private bool _suppressNextBlurDissolve { get => false; set { } }
    private int _blurCrossfadeVersion { get => 0; set { } }
    private int _blurTaskVersion { get => 0; set { } }
    private Task FetchLyricsForTrack(MediaInfo info) => Task.CompletedTask;
    private Task FetchSubtitlesForTrack(MediaInfo info, bool force = false) => Task.CompletedTask;
    private void SuspendSpotifyCanvasLifecycle() { }
    private void ResumeSpotifyCanvasLifecycle() { }
    private void DisposeSpotifyCanvasLifecycle() { }
    internal void UpdateSpotifyCanvasPresentationContext() { }
    private void ApplySpotifyCanvasBrightness() { }
    private void UpdateSpotifyCanvasPlaybackState(MediaInfo info) { }
    private void FadeInSpotifyCanvasBackgroundIfReady() { }
    private void ShowLyricsPlaceholder(string title, string artist, string provider, bool transitionFromSearch = false) { }
    private void UpdateLyricsDisplay() { }
    private void HideLyricsWidget() { }
    internal void CheckAndRetryYouTubeSubtitlesOnExpand() { }
    private static bool IsCurrentTrackYouTube(MediaInfo info) => false;
    private void ClearLyrics() { }
    private void UpdateMediaBackground(MediaInfo? info, bool forceRefresh = false) { }
    private void HideMediaBackground() { }
    private void HideMediaBackgroundOverlay() { }
    private void ShowMediaBackground() { }
    private void ScheduleMediaBackgroundRecovery() { }
    private void ClearBlurredBackgroundSources() { }
    private void StopTitleGradientShift() { }
    private static SolidColorBrush CreateFrozenBrush(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
    private static Color LiftDarkColor(Color color) => color;
}
