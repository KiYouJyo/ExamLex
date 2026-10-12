using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ExamLex.Services;

/// <summary>
/// Different transient surfaces need different treatment:
/// ComboBox option lists must be opaque and readable; ContentDialogs use
/// a Mica-backed, lightly tinted panel with system fallbacks.
/// </summary>
public static class MicaSurfaceService
{
    private static readonly HashSet<ComboBox> HookedCombos = new();
    private static readonly HashSet<FrameworkElement> HookedRoots = new();

    public static void Attach(FrameworkElement root)
    {
        if (!HookedRoots.Add(root)) return;
        root.Loaded += (_, _) => AttachControls(root);
        // Elements can appear after initial load (for example in data-bound cards).
        root.LayoutUpdated += (_, _) => AttachControls(root);
    }

    private static void AttachControls(DependencyObject item)
    {
        if (item is ComboBox combo && HookedCombos.Add(combo))
        {
            SetOpaqueComboBackground(combo);
            combo.DropDownOpened += (_, _) => SetOpaqueComboBackground(combo);
        }

        // Fluent transient surfaces use a proper system backdrop; they should
        // not share the ComboBox transparent resource override.
        if (item is Button button && button.Flyout is { } flyout)
            flyout.SystemBackdrop ??= new MicaBackdrop { Kind = MicaKind.BaseAlt };
        if (item is FrameworkElement element &&
            FlyoutBase.GetAttachedFlyout(element) is { } attached)
            attached.SystemBackdrop ??= new MicaBackdrop { Kind = MicaKind.BaseAlt };

        int count = VisualTreeHelper.GetChildrenCount(item);
        for (int i = 0; i < count; i++)
            AttachControls(VisualTreeHelper.GetChild(item, i));
    }

    private static void SetOpaqueComboBackground(ComboBox combo)
    {
        bool light = combo.ActualTheme == ElementTheme.Light;
        Color color = light
            ? Color.FromArgb(255, 248, 248, 249)
            : Color.FromArgb(255, 42, 44, 45);
        // ComboBox flyouts are deliberately NOT transparent and do not receive
        // a Popup.SystemBackdrop. The theme key is used by the WinUI template.
        combo.Resources["ComboBoxDropDownBackground"] = new SolidColorBrush(color);
    }

    private static void ApplyMicaToDialogPopups(XamlRoot root)
    {
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
            popup.SystemBackdrop ??= new MicaBackdrop { Kind = MicaKind.BaseAlt };
    }

    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        // A Window Mica backdrop alone does not style separate dialog Popup
        // surfaces. Set both the popup backdrop AND the content panel brush;
        // an opaque ContentDialogBackground would hide any backdrop.
        bool light = dialog.XamlRoot?.Content is FrameworkElement root &&
                     root.ActualTheme == ElementTheme.Light;
        var tint = light
            ? new SolidColorBrush(Color.FromArgb(203, 244, 246, 248))
            : new SolidColorBrush(Color.FromArgb(190, 38, 42, 44));
        dialog.Background = tint;
        dialog.Resources["ContentDialogBackground"] = tint;
        dialog.Opened += (_, _) =>
        {
            if (dialog.XamlRoot is { } xr)
            {
                ApplyMicaToDialogPopups(xr);
                dialog.DispatcherQueue.TryEnqueue(() => ApplyMicaToDialogPopups(xr));
            }
        };
        return await dialog.ShowAsync();
    }
}
