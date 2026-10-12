using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;

namespace ExamLex.Controls;

public sealed partial class PinToggleControl : UserControl
{
    public PinToggleControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowPinService.PinnedChanged -= UpdateVisual;
        WindowPinService.PinnedChanged += UpdateVisual;
        UpdateVisual(WindowPinService.IsPinned);
    }
    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        WindowPinService.PinnedChanged -= UpdateVisual;
    private void OnPinClicked(object sender, RoutedEventArgs e) =>
        WindowPinService.SetPinned(PinToggle.IsChecked == true);
    private void UpdateVisual(bool pinned)
    {
        PinToggle.IsChecked = pinned;
        UnpinnedIcon.Visibility = pinned ? Visibility.Collapsed : Visibility.Visible;
        PinnedIcon.Visibility = pinned ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(PinToggle, pinned ? "取消窗口置顶" : "窗口置顶");
    }
}
