using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VNotch;
public partial class MainWindow {
 DispatcherTimer? _zuzongNoticeTimer;
 Visibility _zuzongPriorClock,_zuzongPriorMusic;
 bool _zuzongNoticeVisible;
 internal void ShowZuzongNotice(string text){
  if(_isExpanded||_transitionCoordinator.IsTransitionActive||!IsEffectivelyNotchVisible)return;
  _zuzongNoticeTimer?.Stop();if(!_zuzongNoticeVisible){_zuzongPriorClock=CollapsedContent.Visibility;_zuzongPriorMusic=MusicCompactContent.Visibility;}
  _zuzongNoticeVisible=true;CollapsedContent.Visibility=MusicCompactContent.Visibility=Visibility.Hidden;ZuzongNoticeText.Text=text;ZuzongNoticePanel.Visibility=Visibility.Visible;
  ZuzongNoticePanel.BeginAnimation(OpacityProperty,new DoubleAnimation(.15,1,TimeSpan.FromMilliseconds(220)));
  if(_zuzongNoticeTimer==null){_zuzongNoticeTimer=new DispatcherTimer(DispatcherPriority.Normal){Interval=TimeSpan.FromSeconds(2.7)};_zuzongNoticeTimer.Tick+=(_,_)=>HideZuzongNotice();Closed+=(_,_)=>_zuzongNoticeTimer.Stop();_transitionCoordinator.StateChanged+=(_,_)=>{if(_transitionCoordinator.IsTransitionActive)HideZuzongNotice();};}
  _zuzongNoticeTimer.Start();
 }
 void HideZuzongNotice(){_zuzongNoticeTimer?.Stop();if(!_zuzongNoticeVisible)return;_zuzongNoticeVisible=false;ZuzongNoticePanel.Visibility=Visibility.Collapsed;CollapsedContent.Visibility=_zuzongPriorClock;MusicCompactContent.Visibility=_zuzongPriorMusic;}
}
