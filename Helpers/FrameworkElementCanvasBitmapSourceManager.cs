using Microsoft.UI.Xaml;

using System.Runtime.CompilerServices;

namespace TryLiquidGlass.Helpers;

public class FrameworkElementCanvasBitmapSourceManager
{
    public static FrameworkElementCanvasBitmapSource GetBitmapSourceFor(FrameworkElement target)
    {
        if (!_sources.TryGetValue(target, out FrameworkElementCanvasBitmapSource? bitmapSource))
        {
            bitmapSource = new FrameworkElementCanvasBitmapSource(target);
            _sources.Add(target, bitmapSource);
        }
        return bitmapSource;
    }

    private static readonly ConditionalWeakTable<FrameworkElement, FrameworkElementCanvasBitmapSource> _sources = [];
}
