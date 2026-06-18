using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Threading.Tasks;

using TryLiquidGlass.Helpers;

using Windows.Graphics.DirectX;
using Windows.Storage.Streams;

namespace TryLiquidGlass;

public partial class FrameworkElementCanvasBitmapSource : IDisposable
{
    public FrameworkElementCanvasBitmapSource(FrameworkElement target)
    {
        _target = target;
        _target.SizeChanged += OnSizeChanged;
        _renderTargetBitmapSource = new RenderTargetBitmapSource(target);
    }

    private readonly FrameworkElement _target;
    private readonly RenderTargetBitmapSource _renderTargetBitmapSource;

    public event EventHandler? Invalidated;

    public void Invalidate()
    {
        _renderTargetBitmapSource.Invalidate();
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<CanvasBitmap> GetAsync(ICanvasResourceCreator canvasResourceCreator)
    {
        RenderTargetBitmap renderTargetBitmap = await _renderTargetBitmapSource.GetAsync();
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        return CanvasBitmap.CreateFromBytes(
            canvasResourceCreator,
            pixelBuffer.ToBytes(),
            renderTargetBitmap.PixelWidth,
            renderTargetBitmap.PixelHeight,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            (float)(_target.XamlRoot.RasterizationScale * 96));
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Invalidate();
    }

    #region IDisposable

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                _target.SizeChanged -= OnSizeChanged;
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~RenderTargetBitmapSource()
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
