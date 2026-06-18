using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage.Streams;
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
        }
        if (e.NewValue is FrameworkElement newValue)
        {
            newValue.SizeChanged += OnBackgroundSourceSizeChanged;
        }
    }

    private async void OnBackgroundSourceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _isBackdropBitmapValid = false;
        await UpdateLiquidGlassAsync();
    }

    private CanvasControl? PART_CanvasControl;

    private CanvasRenderTarget? _displacementMap;
    private bool _isDisplacementMapValid;

    private readonly RenderTargetBitmap _backdropRenderTargetBitmap = new();
    private CanvasBitmap? _backdropBitmap;
    private bool _isBackdropBitmapValid;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        PART_CanvasControl = GetTemplateChild(nameof(PART_CanvasControl)) as CanvasControl;
        if (PART_CanvasControl is not null)
        {
            PART_CanvasControl?.Draw += OnDraw;
        }
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
        if (!IsLoaded || BackgroundSource is null || PART_CanvasControl is null)
            return false;

        await _backdropRenderTargetBitmap.RenderAsync(BackgroundSource);
        IBuffer pixelBuffer = await _backdropRenderTargetBitmap.GetPixelsAsync();
        byte[] pixelBytes = new byte[pixelBuffer.Length];
        using (DataReader dataReader = DataReader.FromBuffer(pixelBuffer))
        {
            dataReader.ReadBytes(pixelBytes);
        }
        _backdropBitmap = CanvasBitmap.CreateFromBytes(
            PART_CanvasControl,
            pixelBytes,
            _backdropRenderTargetBitmap!.PixelWidth,
            _backdropRenderTargetBitmap!.PixelHeight,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            (float)(XamlRoot.RasterizationScale * 96));
        return _backdropRenderTargetBitmap.PixelWidth > 0 && _backdropRenderTargetBitmap.PixelHeight > 0;
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

    private async void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
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
        int r, r2;
        Dictionary<int, int> mapXToYAtTopLeft = new(cornerRadius[0]);
        Dictionary<int, int> mapYToXAtTopLeft = new(cornerRadius[0]);
        Dictionary<int, int> mapXToYAtTopRight = new(cornerRadius[1]);
        Dictionary<int, int> mapYToXAtTopRight = new(cornerRadius[1]);
        Dictionary<int, int> mapXToYAtBottomRight = new(cornerRadius[2]);
        Dictionary<int, int> mapYToXAtBottomRight = new(cornerRadius[2]);
        Dictionary<int, int> mapXToYAtBottomLeft = new(cornerRadius[3]);
        Dictionary<int, int> mapYToXAtBottomLeft = new(cornerRadius[3]);

        r = cornerRadius[0];
        r2 = r * r;
        for (int x = 0; x < r; x++)
        {
            mapXToYAtTopLeft[x] = (int)(r - Math.Sqrt(r2 - Square(x - r)));
        }
        for (int y = 0; y < r; y++)
        {
            mapYToXAtTopLeft[y] = (int)(r - Math.Sqrt(r2 - Square(y - r)));
        }

        r = cornerRadius[1];
        r2 = r * r;
        for (int x = width - r; x < width; x++)
        {
            mapXToYAtTopRight[x] = (int)(r - Math.Sqrt(r2 - Square((width - x) - r)));
        }
        for (int y = 0; y < r; y++)
        {
            mapYToXAtTopRight[y] = width - (int)(r - Math.Sqrt(r2 - Square(y - r)));
        }

        r = cornerRadius[2];
        r2 = r * r;
        for (int x = width - r; x < width; x++)
        {
            mapXToYAtBottomRight[x] = height - (int)(r - Math.Sqrt(r2 - Square((width - x) - r)));
        }
        for (int y = height - r; y < height; y++)
        {
            mapYToXAtBottomRight[y] = width - (int)(r - Math.Sqrt(r2 - Square((height - y) - r)));
        }

        r = cornerRadius[3];
        r2 = r * r;
        for (int x = 0; x < r; x++)
        {
            mapXToYAtBottomLeft[x] = height - (int)(r - Math.Sqrt(r2 - Square(x - r)));
        }
        for (int y = height - r; y < height; y++)
        {
            mapYToXAtBottomLeft[y] = (int)(r - Math.Sqrt(r2 - Square((height - y) - r)));
        }

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int xLeft = 0;
                int xRight = width - 1;
                int yTop = 0;
                int yBottom = height - 1;

                if (x < cornerRadius[0] && y < cornerRadius[0])  // 左上角
                {
                    xLeft = mapYToXAtTopLeft[y];
                    yTop = mapXToYAtTopLeft[x];
                }
                else if (x >= width - cornerRadius[1] && y < cornerRadius[1])  // 右上角
                {
                    xRight = mapYToXAtTopRight[y];
                    yTop = mapXToYAtTopRight[x];
                }
                else if (x >= width - cornerRadius[2] && y >= height - cornerRadius[2])  // 右下角
                {
                    xRight = mapYToXAtBottomRight[y];
                    yBottom = mapXToYAtBottomRight[x];
                }
                else if (x < cornerRadius[3] && y >= height - cornerRadius[3])  // 左下角
                {
                    xLeft = mapYToXAtBottomLeft[y];
                    yBottom = mapXToYAtBottomLeft[x];
                }

                int w = xRight - xLeft + 1;
                int h = yBottom - yTop + 1;
                EllipticDisplacementFunction xDisplacementFunction = new(thickness, w, 0.618 * w);
                EllipticDisplacementFunction yDisplacementFunction = new(thickness, h, 0.618 * h);

                double displacementX = 0;
                double displacementY = 0;
                if ((xLeft <= x && x <= xLeft + thickness) || (xRight - thickness <= x && x <= xRight))
                {
                    displacementX = xDisplacementFunction.Calculate(x - xLeft);
                }
                if ((yTop <= y && y <= yTop + thickness) || (yBottom - thickness <= y && y <= yBottom))
                {
                    displacementY = yDisplacementFunction.Calculate(y - yTop);
                }
                pixels[y * width + x] = Color.FromArgb(
                    byte.MaxValue,
                    (byte)(128 + byte.MaxValue * displacementX / width / 2),
                    (byte)(128 + byte.MaxValue * displacementY / height / 2),
                    128);
            }
        }
        return pixels;
    }

    private static int Square(int x) => x * x;

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
