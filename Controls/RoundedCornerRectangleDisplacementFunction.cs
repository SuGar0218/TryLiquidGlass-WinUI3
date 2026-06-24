#pragma warning disable CA2208

using System;

namespace TryLiquidGlass.Controls;

public class RoundedCornerRectangleDisplacementFunction
{
    public RoundedCornerRectangleDisplacementFunction(
        DisplacementDistanceFunction displacementDistanceFunction,
        double width,
        double height,
        double[] cornerRadius)
    {
#if DEBUG
        ArgumentOutOfRangeException.ThrowIfNotEqual(cornerRadius.Length, 4);
#endif
        _displacementDistanceFunction = displacementDistanceFunction;
        _width = width;
        _height = height;
        _cornerRadius = cornerRadius;
    }

    private readonly DisplacementDistanceFunction _displacementDistanceFunction;
    private readonly double _width;
    private readonly double _height;
    private readonly double[] _cornerRadius;

    private (double, double) DisplaceAtTopLeftCorner(double x, double y, double cornerRadius)  // 0 <= x <= r && 0 <= y <= r
    {
        double r = cornerRadius;
#if DEBUG
        if (x < 0 || x > r || y < 0 || y > r)
            throw new ArgumentOutOfRangeException("0 <= x <= r && 0 <= y <= r");
#endif
        double d = r - Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r));  // distance to edge
        if (d < 0)  // (x, y) outside the rounded corner
            return (0, 0);

        double k = (_displacementDistanceFunction.Calculate(d) - d) / (d - r);
        return (k * (x - r), k * (y - r));
    }

    private (double, double) DisplaceAtTopLeftQuarter(double x, double y, double cornerRadius)
    {
        double r = cornerRadius;
#if DEBUG
        if (x < 0 || x > _width / 2 || y < 0 || y > _height / 2)
            throw new ArgumentOutOfRangeException("0 <= x <= halfWidth && 0 <= y <= halfHeight");
#endif
        if (x <= r && y <= r)
            return DisplaceAtTopLeftCorner(x, y, r);

        if (x < y)
            return (_displacementDistanceFunction.Calculate(x) - x, 0);

        return (0, _displacementDistanceFunction.Calculate(y) - y);
    }

    public (double, double) Calculate(double x, double y)
    {
#if DEBUG
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            throw new ArgumentOutOfRangeException("0 <= x < width && 0 <= y < height");
#endif
        double halfWidth = _width / 2;
        double halfHeight = _height / 2;
        double displaceX = 0;
        double displaceY = 0;
        if (x < halfWidth)
        {
            if (y < halfHeight)  // top-left quarter
            {
                (displaceX, displaceY) = DisplaceAtTopLeftQuarter(x, y, _cornerRadius[0]);
            }
            else  // bottom-left quarter
            {
                (displaceX, displaceY) = DisplaceAtTopLeftQuarter(x, _height - y, _cornerRadius[3]);
                displaceY = -displaceY;
            }
        }
        else
        {
            if (y < halfHeight) // top-right quarter
            {
                (displaceX, displaceY) = DisplaceAtTopLeftQuarter(_width - x, y, _cornerRadius[1]);
                displaceX = -displaceX;
            }
            else  // bottom-right quarter
            {
                (displaceX, displaceY) = DisplaceAtTopLeftQuarter(_width - x, _height - y, _cornerRadius[2]);
                displaceX = -displaceX;
                displaceY = -displaceY;
            }
        }
        return (displaceX, displaceY);
    }
}
