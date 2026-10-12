using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace ExamLex.Services;

/// <summary>
/// Surface-level Mica for native WinUI3 popups, combo menus and content dialogs.
/// The window backdrop alone does not affect these Popup HWND surfaces.
/// Windows 10 and disabled transparency can fall back to an opaque theme brush.
/// </summary>
public static class MicaSurfaceService
{
    private static readonly HashSet<ComboBox> HookedCombos=new();
    private static readonly HashSet<FrameworkElement> HookedRoots=new();

    public static void Attach(FrameworkElement root)
    {
        if (!HookedRoots.Add(root)) return;
        root.Loaded += (_,_)=>AttachControls(root);
        root.LayoutUpdated += (_,_)=>AttachControls(root);
    }

    private static void AttachControls(DependencyObject obj)
    {
        if (obj is ComboBox combo && HookedCombos.Add(combo))
        {
            combo.DropDownOpened += (_,_) =>
            {
                if (combo.XamlRoot is not null)
                {
                    ApplyOpenPopups(combo.XamlRoot);
                    combo.DispatcherQueue.TryEnqueue(()=>ApplyOpenPopups(combo.XamlRoot));
                }
            };
        }
        if(obj is Button button && button.Flyout is { } flyout)
            flyout.SystemBackdrop ??= new MicaBackdrop();
        if(obj is FrameworkElement elt)
        {
            if(FlyoutBase.GetAttachedFlyout(elt) is { } attached)
                attached.SystemBackdrop ??= new MicaBackdrop();
        }
        int count=VisualTreeHelper.GetChildrenCount(obj);
        for(int i=0;i<count;i++) AttachControls(VisualTreeHelper.GetChild(obj,i));
    }

    private static void ApplyOpenPopups(XamlRoot root)
    {
        foreach(var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
            popup.SystemBackdrop ??= new MicaBackdrop();
    }

    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        dialog.Opened += (_,_) =>
        {
            if(dialog.XamlRoot is { } root) ApplyOpenPopups(root);
        };
        return await dialog.ShowAsync();
    }
}
