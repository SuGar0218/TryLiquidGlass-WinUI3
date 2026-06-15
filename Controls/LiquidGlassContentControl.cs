using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Threading.Tasks;

namespace TryLiquidGlass.Controls;

[TemplatePart(Name = nameof(PART_LiquidGlassBackground), Type = typeof(LiquidGlassBackground))]
public sealed partial class LiquidGlassContentControl : ContentControl
{
    public LiquidGlassContentControl()
    {
        DefaultStyleKey = typeof(LiquidGlassContentControl);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness),
        typeof(double),
        typeof(LiquidGlassContentControl),
        new PropertyMetadata(default(double))
    );

    public UIElement BackgroundSource
    {
        get => (UIElement)GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    public static readonly DependencyProperty BackgroundSourceProperty = DependencyProperty.Register(
        nameof(BackgroundSource),
        typeof(UIElement),
        typeof(LiquidGlassContentControl),
        new PropertyMetadata(default(UIElement))
    );

    private LiquidGlassBackground? PART_LiquidGlassBackground;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        PART_LiquidGlassBackground = GetTemplateChild(nameof(PART_LiquidGlassBackground)) as LiquidGlassBackground;
    }

    public Task UpdateLiquidGlassAsync() => PART_LiquidGlassBackground?.UpdateLiquidGlassAsync() ?? Task.CompletedTask;
}
