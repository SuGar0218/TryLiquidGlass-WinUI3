using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Foundation;

namespace TryLiquidGlass;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private bool _isPointerPressed;
    private Point _previousPosition;

    private void LiquidGlassContentControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPointerPressed = true;
        _previousPosition = e.GetCurrentPoint(PART_RootGrid).Position;
        PART_LiquidGlassSample.CapturePointer(e.Pointer);
    }

    private async void LiquidGlassContentControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerPressed)
            return;

        Point currentPosition = e.GetCurrentPoint(PART_RootGrid).Position;
        Canvas.SetLeft(PART_LiquidGlassSample, Canvas.GetLeft(PART_LiquidGlassSample) + currentPosition.X - _previousPosition.X);
        Canvas.SetTop(PART_LiquidGlassSample, Canvas.GetTop(PART_LiquidGlassSample) + currentPosition.Y - _previousPosition.Y);
        _previousPosition = currentPosition;
    }

    private void LiquidGlassContentControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPointerPressed = false;
        PART_LiquidGlassSample.ReleasePointerCapture(e.Pointer);
    }
}
