using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Threading.Tasks;

namespace TryLiquidGlass.Helpers;

public class RenderTargetBitmapSource
{
    public RenderTargetBitmapSource(UIElement target)
    {
        _target = target;
        _renderTargetBitmap = new RenderTargetBitmap();
    }

    private readonly UIElement _target;
    private readonly RenderTargetBitmap _renderTargetBitmap;
    private bool _isValid;
    private Task? _renderingTask;

    public event EventHandler? Invalidated;

    public void Invalidate()
    {
        _isValid = false;
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<RenderTargetBitmap> GetAsync()
    {
        if (!_isValid)
        {
            _renderingTask ??= RenderAsync();
            await _renderingTask;
            _renderingTask = null;
            _isValid = true;
        }
        return _renderTargetBitmap;
    }

    private async Task RenderAsync() => await _renderTargetBitmap.RenderAsync(_target);
}
