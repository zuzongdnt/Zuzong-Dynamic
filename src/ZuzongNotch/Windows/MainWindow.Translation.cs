using VNotch.Controllers;

namespace VNotch;

public partial class MainWindow
{
    private LiveTranslationController? _liveTranslation;
    private void InitializeLiveTranslation()
    {
        if (!_settings.EnableLiveTranslation) return;
        _liveTranslation ??= new LiveTranslationController(
            Dispatcher,
            () => OpenAppSettings(showTranslation: true),
            saveLanguages: (source, target) =>
            {
                _settings.TranslationSourceLanguage = source;
                _settings.TranslationTargetLanguage = target;
                _settingsService.Save(_settings);
            });
        _liveTranslation.ApplySettings(_settings);
        _ = _liveTranslation.PrepareModelAsync();
    }
}
