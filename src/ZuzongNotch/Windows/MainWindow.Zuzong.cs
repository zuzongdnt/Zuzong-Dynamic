using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using VNotch.Models;
using VNotch.Services;

namespace VNotch;
public partial class MainWindow {
 DispatcherTimer? _zuzongTimer;
 DateTime _zuzongStamp,_zuzongTaskbarStamp,_zuzongCommandStamp,_zuzongNoticeStamp;
 int _zuzongTicks,_zuzongNotices;
 internal void StartZuzongIntegration(){
  _zuzongStamp=File.GetLastWriteTimeUtc(ZuzongIntegration.ConfigPath);_zuzongTaskbarStamp=File.GetLastWriteTimeUtc(Path.Combine(ZuzongIntegration.ParentData,"settings.json"));
  // Do not replay a settings command from the previous process at Windows sign-in.
  _zuzongCommandStamp=File.GetLastWriteTimeUtc(Path.Combine(ZuzongIntegration.ParentData,"notch-command.json"));
  _zuzongNoticeStamp=File.GetLastWriteTimeUtc(Path.Combine(ZuzongIntegration.ParentData,"notch-notice.json"));
  _transitionCoordinator.StateChanged+=(_,_)=>{ZuzongLogoOverlay.Visibility=ZuzongIntegration.ShowLogo&&!_isExpanded&&!_transitionCoordinator.IsTransitionActive?Visibility.Visible:Visibility.Collapsed;};
  Title="Zuzong Dynamic Notch";ApplyZuzongFeatures();ApplyZuzongColor();
  _zuzongTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(750)};
  _zuzongTimer.Tick+=(_,_)=>PollZuzong();_zuzongTimer.Start();Closed+=(_,_)=>{_zuzongTimer.Stop();DisposeShellHealth();WriteZuzongStatus(true);};WriteZuzongStatus();
 }
 void PollZuzong(){if(_cleanedUp||(Application.Current as App)?.IsShutdownRequested==true)return;try{
  if(!ZuzongIntegration.SettingsEnabled){_ = App.RequestShutdownAsync();return;}
  DateTime stamp=File.GetLastWriteTimeUtc(ZuzongIntegration.ConfigPath),taskbar=File.GetLastWriteTimeUtc(Path.Combine(ZuzongIntegration.ParentData,"settings.json"));
  if(stamp!=_zuzongStamp||taskbar!=_zuzongTaskbarStamp){var next=_settings.Clone();ZuzongIntegration.MapCustomSettings(next);OnSettingsWindowSettingsChanged(null,next);_shellState.ExpandedWidth=ZuzongIntegration.ExpandedWidth;ApplyZuzongFeatures();ApplyZuzongColor();_zuzongStamp=stamp;_zuzongTaskbarStamp=taskbar;}
  string command=Path.Combine(ZuzongIntegration.ParentData,"notch-command.json");DateTime cmdStamp=File.GetLastWriteTimeUtc(command);
  if(cmdStamp!=_zuzongCommandStamp&&File.Exists(command)&&new FileInfo(command).Length<2048){_zuzongCommandStamp=cmdStamp;if((DateTime.UtcNow-cmdStamp).TotalSeconds<12){using var c=JsonDocument.Parse(File.ReadAllText(command));string? type=c.RootElement.GetProperty("type").GetString();if(type=="appearance")OpenZuzongAppearance();else if(type=="features")OpenZuzongFeatures();else if(type=="settings")OpenAppSettings();}}
  string notice=Path.Combine(ZuzongIntegration.ParentData,"notch-notice.json");DateTime ns=File.GetLastWriteTimeUtc(notice);
  if(ZuzongIntegration.CodexNotice&&ns!=_zuzongNoticeStamp&&File.Exists(notice)){_zuzongNoticeStamp=ns;if((DateTime.UtcNow-ns).TotalSeconds<12&&new FileInfo(notice).Length<2048){using var n=JsonDocument.Parse(File.ReadAllText(notice));if(n.RootElement.GetProperty("type").GetString()=="codex-complete"){ShowZuzongNotice("Codex đã hoàn tất");_zuzongNotices++;}}}
  CheckShellHealth();
  if(++_zuzongTicks%4==0)WriteZuzongStatus();
 }catch(Exception e){RuntimeLog.Error("ZUZONG",e,"Keeping last valid configuration");}}
 void WriteZuzongStatus(bool stopped=false){try{var p=Process.GetCurrentProcess();string file=Path.Combine(ZuzongIntegration.ParentData,"notch-runtime.json"),temp=file+".new";File.WriteAllText(temp,JsonSerializer.Serialize(new{pid=stopped?0:p.Id,startTicks=p.StartTime.ToUniversalTime().Ticks,state=stopped?"stopped":_isHiddenByFullscreen?"hidden-fullscreen":_isExpanded?"expanded":"compact",view=_isMusicCompactMode?"media":"clock",glassReady=_liquidGlass?.HasPresentedFrame==true,clipboardListener=true,noticesShown=_zuzongNotices,mediaAvailable=_currentMediaInfo!=null,playing=_currentMediaInfo?.IsPlaying==true,artworkAvailable=_currentMediaInfo?.HasThumbnail==true,artworkWidth=_currentMediaInfo?.Thumbnail?.PixelWidth,artworkHeight=_currentMediaInfo?.Thumbnail?.PixelHeight,fullFeatures=true,updatedUtc=DateTime.UtcNow}));File.Move(temp,file,true);}catch{}}
 void ApplyZuzongColor(){
  if(IsLiquidGlassEnabled){GlassTintOverlay.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(ZuzongIntegration.Tint));}
  else{var black=new SolidColorBrush(Colors.Black);black.Freeze();NotchBackground.Background=black;Resources["NotchGradient"]=black;ExpandedContent.Background=black;}
  LeftEar.Fill=RightEar.Fill=IsLiquidGlassEnabled?GlassTintOverlay.Background:Brushes.Black;
  ZuzongLogoOverlay.Visibility=ZuzongIntegration.ShowLogo&&!_isExpanded?Visibility.Visible:Visibility.Collapsed;
 }
 bool ZuzongTargetEnabled(NotchView target)=>target switch {NotchView.Secondary=>_settings.VisibleNavTabs.Split(',').Contains("Secondary"),NotchView.Timer=>_settings.VisibleNavTabs.Split(',').Contains("Timer"),NotchView.AudioMixer=>_settings.VisibleNavTabs.Split(',').Contains("AudioMixer"),NotchView.Camera=>!_settings.HideCamera&&_settings.VisibleNavTabs.Split(',').Contains("Camera"),_=>true};
 internal void ApplyZuzongFeatures(){
  ZuzongIntegration.MusicEnabled=_settings.ZuzongMusic;ZuzongIntegration.HistoryEnabled=_settings.ZuzongClipboardHistory;
  if(!_settings.ZuzongMusic){_mediaService.Stop();MusicViz.IsPlaying=false;_currentMediaInfo=null;CancelPendingMusicCompactExit();ApplyMusicCompactMode(false,null);}else if(_postAppearInitializationStarted)_mediaService.Start();
  _clipboardHistory!.IsPaused=!_settings.ZuzongClipboardHistory;
  if(!_settings.ZuzongBluetooth)_bluetoothModule.Stop();else if(_coreModulesStarted)_bluetoothModule.Start();
  if(!_settings.ShowBatteryIndicator)_batteryModule.Stop();else if(_coreModulesStarted)_batteryModule.Start();
  if(!_settings.ZuzongCalendar)_calendarModule.Stop();
  if(!_settings.ZuzongSystemMonitor)_systemMonitorModule.Stop();
  if(!_settings.EnableWeather)_weatherModule.Stop();
  if(!_settings.VisibleNavTabs.Split(',').Contains("Timer")&&CountdownController.IsRunning)CountdownController.Pause();
  if(!_settings.VisibleNavTabs.Split(',').Contains("AudioMixer"))StopAudioPoll();
  if(_settings.HideCamera&&IsCameraPreviewLifecycleActive)StopCameraPreviewForViewExit();
  ApplyNavTabOrderAndVisibility();
  WhiteNoiseWidget.Visibility=_settings.ZuzongWhiteNoise?Visibility.Visible:Visibility.Collapsed;if(!_settings.ZuzongWhiteNoise)WhiteNoiseService.Pause();
  AllowDrop=_settings.VisibleNavTabs.Split(',').Contains("Secondary");
 }
 VNotch.Windows.ZuzongFeaturesWindow? _zuzongFeatures;
 internal void OpenZuzongFeatures(){if(_zuzongFeatures?.IsVisible==true){_zuzongFeatures.Activate();return;}_zuzongFeatures=new VNotch.Windows.ZuzongFeaturesWindow(this,_settings.Clone()){Owner=this};_zuzongFeatures.Show();}
 internal async Task SaveZuzongFeatures(NotchSettings next){await _settingsService.SaveAsync(next);OnSettingsWindowSettingsChanged(null,next);ZuzongIntegration.WriteBasicFromFull(next);ApplyZuzongFeatures();ApplyZuzongColor();}
 VNotch.Windows.ZuzongAppearanceWindow? _zuzongAppearance;
 internal void OpenZuzongAppearance(){if(_zuzongAppearance?.IsVisible==true){_zuzongAppearance.Activate();return;}_zuzongAppearance=new VNotch.Windows.ZuzongAppearanceWindow(this);_zuzongAppearance.Show();}
 internal void OpenZuzongAdvanced()=>OpenAppSettings();
 internal void ReloadZuzongAppearance()=>PollZuzong();
 void OpenAppearance_Click(object sender,RoutedEventArgs e)=>OpenZuzongAppearance();
}
