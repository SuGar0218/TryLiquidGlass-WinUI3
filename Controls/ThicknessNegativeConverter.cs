using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using System;

namespace TryLiquidGlass.Controls;

internal partial class ThicknessNegativeConverter : IValueConverter
{
    public static Thickness Convert(Thickness thickness) => new(-thickness.Left, -thickness.Top, -thickness.Right, -thickness.Bottom);

    public static Thickness ConvertBack(Thickness thickness) => Convert(thickness);

    public object Convert(object value, Type targetType, object parameter, string language) => value is Thickness thickness ? Convert(thickness) : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is Thickness thickness ? ConvertBack(thickness) : value;
}
