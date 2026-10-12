using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ExamLex.Services;

/// <summary>Theme acrylic for non-modal dropdown and menu surfaces only.</summary>
public static class MicaSurfaceService
{
    private static readonly HashSet<FrameworkElement> Roots=new();
    public static void Attach(FrameworkElement root)
    {
        if(!Roots.Add(root))return;
        // Acrylic brushes come from theme resources and track light/dark theme;
        // avoid overriding them with an opaque gray ComboBox brush.
    }
}
