using VNotch.Models;

namespace VNotch.Services;

internal static class ZuzongLightweightDefaults
{
    internal const int Version = 1;
    internal static bool ApplyOnce(NotchSettings settings)
    {
        if (settings.ZuzongLightweightDefaultsVersion >= Version) return false;
        settings.EnableLiveTranslation = false;
        settings.AutoLiveTranslation = false;
        settings.EnableTranslationHumanizer = false;
        settings.EnableCurrencyConversion = false;
        settings.EnableSmartCrop = false;
        settings.EnableSubjectBlur = false;
        settings.EnableSpotlight = false;
        settings.EnableSpotlightHistory = false;
        settings.ZuzongClipboardHistory = false;
        settings.ZuzongBluetooth = false;
        settings.ZuzongCalendar = false;
        settings.ZuzongSystemMonitor = false;
        settings.ZuzongWhiteNoise = false;
        settings.EnablePrivacyIndicators = false;
        settings.EnableWeather = false;
        settings.EnableSpotifyCanvas = false;
        settings.EnableSpotifyLyrics = false;
        settings.EnableYouTubeSubtitles = false;
        settings.EnableOnlineLyrics = false;
        settings.EnableInteractionSounds = false;
        settings.ShowMediaArtBackground = false;
        settings.HideCamera = true;
        settings.VisibleNavTabs = "Media,Secondary,Timer,AudioMixer";
        settings.ExpandedWidget = "none";
        settings.AnimationFps = 60;
        settings.AutoAnimationFps = true;
        settings.LiquidGlass.Saturation = 0;
        settings.LiquidGlass.UseUltimateGpu = false;
        settings.ZuzongLightweightDefaultsVersion = Version;
        return true;
    }
}
