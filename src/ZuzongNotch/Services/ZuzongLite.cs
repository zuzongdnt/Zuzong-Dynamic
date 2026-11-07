using VNotch.Models;

namespace VNotch.Services;

internal static class ZuzongLite
{
    internal const string ProductName = "Zuzong Dynamic Lite";
    internal static string Text(string vi, string en) => Loc.CurrentLanguage == "vi" ? vi : en;

    // Enforce the shipped feature set after loading/importing an older full profile.
    internal static void Apply(NotchSettings s)
    {
        s.Language = s.Language == "vi" ? "vi" : "en";
        s.EnableSpotifyCanvas = false;
        s.EnableSpotifyLyrics = false;
        s.EnableOnlineLyrics = false;
        s.EnableYouTubeSubtitles = false;
        s.ShowMediaArtBackground = false;
        s.ZuzongClipboardHistory = false;
        s.EnableSmartCrop = false;
        s.EnableSubjectBlur = false;
        s.EnableLiveTranslation = false;
        s.AutoLiveTranslation = false;
        s.EnableTranslationHumanizer = false;
        s.EnableSpotlight = false;
        s.EnableSpotlightHistory = false;
        s.EnableCurrencyConversion = false;
        s.HideCamera = true;
        s.ZuzongBluetooth = false;
        s.ZuzongCalendar = false;
        s.ZuzongSystemMonitor = false;
        s.ZuzongWhiteNoise = false;
        s.EnableWeather = false;
        s.EnablePrivacyIndicators = false;
        s.EnableGestureControls = false;
        s.EnableBrowserUrlInspection = false;
        s.EnableOnlineArtworkLookup = false;
        s.EnableInteractionSounds = false;
        s.EnableHelloGreeting = false;
        s.EnableDiagnosticLogging = false;
        s.ExpandedWidget = "none";
        s.VisibleNavTabs = string.Join(",", s.VisibleNavTabs.Split(',').Where(t => t is "Media" or "Secondary" or "Timer" or "AudioMixer"));
    }
}
