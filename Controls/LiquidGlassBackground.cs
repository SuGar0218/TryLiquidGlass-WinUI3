using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using TryLiquidGlass.Helpers;

using Windows.Foundation;
using Windows.UI;

namespace TryLiquidGlass.Controls;

[TemplatePart(Name = nameof(PART_CanvasControl), Type = typeof(CanvasControl))]
public sealed partial class LiquidGlassBackground : Control, IDisposable
{
    public LiquidGlassBackground()
    {
        DefaultStyleKey = typeof(LiquidGlassBackground);
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        CompositionTarget.Rendered += OnCompositionTargetRendered;
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness),
        typeof(double),
        typeof(LiquidGlassBackground),
        new PropertyMetadata(default(double), (d, e) => ((LiquidGlassBackground)d).OnThicknessChanged(e))
    );

    private async void OnThicknessChanged(DependencyPropertyChangedEventArgs e)
    {
        _isDisplacementMapValid = false;
        await UpdateLiquidGlassAsync();
    }

    public UIElement BackgroundSource
    {
        get => (UIElement)GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    public static readonly DependencyProperty BackgroundSourceProperty = DependencyProperty.Register(
        nameof(BackgroundSource),
        typeof(UIElement),
        typeof(LiquidGlassBackground),
        new PropertyMetadata(default(UIElement), (d, e) => ((LiquidGlassBackground)d).OnBackgroundSourceChanged(e))
    );

    private void OnBackgroundSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        _isBackdropBitmapValid = false;
        if (e.OldValue is FrameworkElement oldValue)
        {
            oldValue.SizeChanged -= OnBackgroundSourceSizeChanged;
            _backdropBitmapSource?.Invalidated -= OnBackdropBitmapSourceInvalidated;
            _backdropBitmapSource = null;
        }
        if (e.NewValue is FrameworkElement newValue)
        {
            newValue.SizeChanged += OnBackgroundSourceSizeChanged;
            _backdropBitmapSource = FrameworkElementCanvasBitmapSourceManager.GetBitmapSourceFor(newValue);
            _backdropBitmapSource.Invalidated += OnBackdropBitmapSourceInvalidated;
        }
    }

    private async void OnBackgroundSourceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _isBackdropBitmapValid = false;
        await UpdateLiquidGlassAsync();
    }

    private void OnBackdropBitmapSourceInvalidated(object? sender, EventArgs e)
    {
        _isBackdropBitmapValid = false;
        DispatcherQueue.TryEnqueue(async () => await UpdateLiquidGlassAsync());
    }

    private CanvasControl? PART_CanvasControl;

    private CanvasRenderTarget? _displacementMap;
    private bool _isDisplacementMapValid;

    private FrameworkElementCanvasBitmapSource? _backdropBitmapSource;
    private CanvasBitmap? _backdropBitmap;
    private bool _isBackdropBitmapValid;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        PART_CanvasControl = GetTemplateChild(nameof(PART_CanvasControl)) as CanvasControl;
        PART_CanvasControl?.Draw += OnCanvasDraw;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(async () => await UpdateLiquidGlassAsync());
    }

    private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _isDisplacementMapValid = false;
        await UpdateLiquidGlassAsync();
    }

    private Point _positionToBackgroundSource;

    private async void OnCompositionTargetRendered(object? sender, RenderedEventArgs e)
    {
        Point position = TransformToVisual(BackgroundSource).TransformPoint(new Point(0, 0));
        if (position == _positionToBackgroundSource)
            return;

        _positionToBackgroundSource = position;
        await UpdateLiquidGlassAsync();
    }

    public async Task UpdateLiquidGlassAsync()
    {
        if (_isUpdatingLiquidGlass)
            return;

        _isUpdatingLiquidGlass = true;
        if (!_isBackdropBitmapValid)
        {
            _isBackdropBitmapValid = await UpdateBackdropImage();
        }
        if (!_isDisplacementMapValid)
        {
            _isDisplacementMapValid = UpdateDisplacementMap();
        }
        if (_isBackdropBitmapValid && _isDisplacementMapValid)
        {
            PART_CanvasControl?.Invalidate();
        }
        _isUpdatingLiquidGlass = false;
    }

    private bool _isUpdatingLiquidGlass;

    private async Task<bool> UpdateBackdropImage()
    {
        if (!IsLoaded || _backdropBitmapSource is null || PART_CanvasControl is null)
            return false;

        _backdropBitmap = await _backdropBitmapSource.GetAsync(PART_CanvasControl);
        bool isValid = _backdropBitmap.SizeInPixels.Width > 0 && _backdropBitmap.SizeInPixels.Height > 0;
        if (!isValid)
        {
            _backdropBitmapSource.Invalidate();
        }
        return isValid;
    }

    private bool UpdateDisplacementMap()
    {
        if (!IsLoaded || PART_CanvasControl is null)
            return false;

        _displacementMap ??= new CanvasRenderTarget(PART_CanvasControl, RenderSize);
        double dpiScale = XamlRoot.RasterizationScale;
        int pixelWidth = (int)_displacementMap.SizeInPixels.Width;
        int pixelHeight = (int)_displacementMap.SizeInPixels.Height;
        int pixelThickness = (int)(Thickness * dpiScale);
        _displacementMap.SetPixelColors(BuildDisplacementPixels(pixelWidth, pixelHeight, pixelThickness, new int[]
        {
            (int)(CornerRadius.TopLeft * dpiScale),
            (int)(CornerRadius.TopRight * dpiScale),
            (int)(CornerRadius.BottomRight * dpiScale),
            (int)(CornerRadius.BottomLeft * dpiScale)
        }));
        return pixelWidth > 0 && pixelHeight > 0;
    }

    private readonly AtlasEffect _atlasEffect = new();
    private readonly DisplacementMapEffect _displacementMapEffect = new();
    private readonly GaussianBlurEffect _gaussianBlurEffect = new();

    private async void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_backdropBitmap is null || _backdropBitmap.Size.Width == 0 && _backdropBitmap.Size.Height == 0)
            return;

        if (_displacementMap is null)
            return;

        Point position = TransformToVisual(BackgroundSource).TransformPoint(new Point(0, 0));
        Rect backdropClipRect = new(new Point(position.X, position.Y), RenderSize);
        Vector2 drawingOffset = new(0, 0);
        if (position.X < 0)
        {
            backdropClipRect.X = 0;
            backdropClipRect.Width = Math.Max(0, backdropClipRect.Width + position.X);
            drawingOffset.X = (float)-position.X;
        }
        if (position.Y < 0)
        {
            backdropClipRect.Y = 0;
            backdropClipRect.Height = Math.Max(0, backdropClipRect.Height + position.Y);
            drawingOffset.Y = (float)-position.Y;
        }

        _atlasEffect.Source = _backdropBitmap;
        _atlasEffect.SourceRectangle = backdropClipRect;

        CanvasCommandList atlasOffsetCommandList = new(PART_CanvasControl);
        using (CanvasDrawingSession session = atlasOffsetCommandList.CreateDrawingSession())
        {
            session.DrawImage(_atlasEffect, drawingOffset);
        }

        _displacementMapEffect.Source = atlasOffsetCommandList;
        _displacementMapEffect.Displacement = _displacementMap;
        _displacementMapEffect.XChannelSelect = EffectChannelSelect.Red;
        _displacementMapEffect.YChannelSelect = EffectChannelSelect.Green;
        _displacementMapEffect.Amount = Math.Min(_displacementMap.SizeInPixels.Width, _displacementMap.SizeInPixels.Height);

        _gaussianBlurEffect.Source = _displacementMapEffect;
        _gaussianBlurEffect.BlurAmount = 2.0f;

        using CanvasDrawingSession drawingSession = args.DrawingSession;
        drawingSession.DrawImage(_gaussianBlurEffect);
    }

    private static Color[] BuildDisplacementPixels(int width, int height, int thickness, int[] cornerRadius)
    {
        int maxDisplacement = Math.Min(width, height);
        DisplacementDistanceFunction displacementDistanceFunction = new(thickness, maxDisplacement);
        RoundedCornerRectangleDisplacementFunction displacementFunction = new(displacementDistanceFunction, width, height, [cornerRadius[0], cornerRadius[1], cornerRadius[2], cornerRadius[3]]);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (double displacementX, double displacementY) = displacementFunction.Calculate(x, y);
                pixels[y * width + x] = Color.FromArgb(
                    byte.MaxValue,
                    (byte)(128 + byte.MaxValue * displacementX / maxDisplacement / 2),
                    (byte)(128 + byte.MaxValue * displacementY / maxDisplacement / 2),
                    128);
            }
        }
        return pixels;
    }

    #region IDisposable

    private bool disposedValue;

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                PART_CanvasControl?.RemoveFromVisualTree();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            PART_CanvasControl = null;
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~LiquidGlassBackground()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    void IDisposable.Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
