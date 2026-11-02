namespace VNotch;

public partial class MainWindow
{
    private bool _coreModulesStarted;

    private void StartCoreModules()
    {
        _coreModulesStarted = true;
        if(_settings.ShowBatteryIndicator)_batteryModule.Start();
        if(_settings.ZuzongBluetooth)_bluetoothModule.Start();
        if (_settings.EnableWeather && !_weatherModule.IsRunning)
        {
            _weatherModule.Start();
        }
        ApplyPrivacyIndicatorSettings();
    }

    private void EnsureCalendarFeatureLoaded()
    {
        if(!_settings.ZuzongCalendar)return;
        InitializeCalendarPresenter();
        if (!_calendarModule.IsRunning)
        {
            _calendarModule.Start();
        }
    }

    private void EnsureActiveExpandedWidgetFeatureLoaded()
    {
        if (!_isExpanded)
        {
            return;
        }

        if (_settings.EnableWeather && !_weatherModule.IsRunning)
        {
            _weatherModule.Start();
        }

        if (_settings.ZuzongSystemMonitor && IsSystemMonitorWidgetMode && !_systemMonitorModule.IsRunning)
        {
            _systemMonitorModule.Start();
        }
    }
}
