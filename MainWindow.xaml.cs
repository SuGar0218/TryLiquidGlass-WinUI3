using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;

using System;

using TryLiquidGlass.Helpers;

using Windows.Foundation;

namespace TryLiquidGlass;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.Dark;
        InitializeComponent();
        _wallpaperBitmapSource = FrameworkElementCanvasBitmapSourceManager.GetBitmapSourceFor(PART_Wallpaper);
    }

    private bool _isPointerPressed;
    private Point _previousPosition;
    private readonly FrameworkElementCanvasBitmapSource _wallpaperBitmapSource;  // Currently, FrameworkElementCanvasBitmapSource serves as the backgroung image provider for Liquid Glass.

    private void LiquidGlassContentControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        UIElement element = (UIElement)sender;
        _isPointerPressed = true;
        _previousPosition = e.GetCurrentPoint(PART_RootGrid).Position;
        element.CapturePointer(e.Pointer);
    }

    private async void LiquidGlassContentControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerPressed)
            return;

        UIElement element = (UIElement)sender;
        Point currentPosition = e.GetCurrentPoint(PART_RootGrid).Position;
        Canvas.SetLeft(element, Canvas.GetLeft(element) + currentPosition.X - _previousPosition.X);
        Canvas.SetTop(element, Canvas.GetTop(element) + currentPosition.Y - _previousPosition.Y);
        _previousPosition = currentPosition;
    }

    private void LiquidGlassContentControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        UIElement element = (UIElement)sender;
        _isPointerPressed = false;
        element.ReleasePointerCapture(e.Pointer);
    }

    private async void OnWallpaperDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        FileOpenPicker picker = new(AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            FileTypeFilter = { ".png", ".jpg", ".jpeg" }
        };
        PickFileResult? result = await picker.PickSingleFileAsync();
        if (!string.IsNullOrWhiteSpace(result?.Path))
        {
            BitmapImage image = new(new Uri(result.Path));
            image.ImageOpened += OnImageOpened;
            PART_Wallpaper.Source = image;
        }
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(_wallpaperBitmapSource.Invalidate);
    }
}
