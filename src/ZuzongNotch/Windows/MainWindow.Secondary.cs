using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VNotch.Controllers;
using VNotch.Models;
using VNotch.Services;

namespace VNotch;

public partial class MainWindow
{
    private ClipboardHotkeyController? _clipboardHotkey;
    internal string? ClipboardHotkeyRegistrationError => _clipboardHotkey?.RegistrationError;
    private void DisposeClipboardHotkey() => _clipboardHotkey?.Dispose();
    private ClipboardHistoryController? _clipboardHistory;
    private VNotch.Controls.LiteFileShelf? _clipboardTrayView;
    internal VNotch.Controls.LiteFileShelf ClipboardTrayView
    {
        get
        {
            if (_clipboardTrayView != null) return _clipboardTrayView;
            ObjectDisposedException.ThrowIf(_cleanedUp, this);
            var view = new VNotch.Controls.LiteFileShelf { Name = nameof(ClipboardTrayView) };
            _clipboardTrayView = view;
            view.SetBinding(VNotch.Controls.LiteFileShelf.NavigationWidthProperty,
                new System.Windows.Data.Binding(nameof(ActualWidth)) { Source = NavTabsStackPanel });
            System.Windows.Data.BindingOperations.SetBinding(view.HeaderTranslate,
                System.Windows.Media.TranslateTransform.YProperty,
                new System.Windows.Data.Binding(nameof(System.Windows.Media.TranslateTransform.Y)) { Source = NavIconsTranslate });
            view.IsVisibleChanged += (_, _) => ApplyGlassContentShadow(IsLiquidGlassEnabled);
            ClipboardTrayHost.Content = view;
            RegisterName(nameof(ClipboardTrayView), view);
            LocalizedPresentation.Apply(view);
            AnimationPrimitives.ApplyFpsToTree(view);
            if (ClipboardTrayHost.IsVisible)
            {
                ClipboardTrayHost.ApplyTemplate();
                ClipboardTrayHost.UpdateLayout();
            }
            return view;
        }
    }
    private bool _isSecondaryView { get; set; }

    private bool _isCameraView;
    private DateTime _lastViewSwitchUtc = DateTime.MinValue;
    private static readonly TimeSpan ViewSwitchCooldown = TimeSpan.FromMilliseconds(600);
    private bool _isScrollSessionLocked;
    private UiScheduledTimer? _scrollSessionResetTimer;
    private double ClipboardTrayWidth
    {
        get
        {
            var screen = MonitorSelection.Resolve(_settings);
            return Math.Min(920, Math.Max(420, screen.WorkingArea.Width / MonitorSelection.GetScale(screen) - 80));
        }
    }
    private const double ClipboardTrayHeight = 268;
    private const double CameraViewWidth = 520;
    private const double CameraViewHeight = 328;

    private void InitializeClipboardHistory(VNotch.Services.Clipboard.ClipboardHistoryStore? store)
    {
        _clipboardHistory = null;
        _clipboardHotkey = new ClipboardHotkeyController(this, () =>
        {
            if (_cleanedUp || _isGreetingActive || _isAnimating) return;
            if (!_isNotchVisible) ToggleNotch_Click(this, new RoutedEventArgs());
            if (_isExpanded && _isSecondaryView) CollapseNotch();
            else
            {
                _focusNotchAfterExpand = true;
                _transitionCoordinator.RequestView(NotchView.Secondary, "ClipboardHotkey");
            }
        });
        _clipboardHotkey.Apply(_settings.ClipboardHotkey);
    }
    private void HandleClipboardUpdated()
    {
        if(_settings.ZuzongClipboardHistory)_clipboardHistory?.NotifyClipboardUpdated();
        _clipboardListener.NotifyClipboardUpdated();
    }
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_isAnimating)
        {
            e.Handled = true;
            if (_isSecondaryView && ClipboardTrayView.TryDismissOverlay()) return;
            CollapseNotch();
        }
    }
    private void CameraIconButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (!_settings.HideCamera) NavigateToNotchView(NotchView.Camera);
    }
}
