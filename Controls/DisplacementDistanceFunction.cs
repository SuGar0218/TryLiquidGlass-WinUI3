using System;

namespace TryLiquidGlass.Controls;

/// <summary>
/// 距离边缘 d 的点应该显示位于距离多远的点
/// </summary>
public class DisplacementDistanceFunction
{
    public DisplacementDistanceFunction(double thickness, double maxDistance)
    {
        t = thickness;
        m = maxDistance;
        a = (m - t) / Math.Pow(t, 4);
    }

    private readonly double t, m, a;

    private double F(double x)
    {
        double temp = x - t;
        return a * Math.Pow(temp, 4) + t;
    }

    public double Calculate(double distance)
    {
#if DEBUG
        ArgumentOutOfRangeException.ThrowIfNegative(distance);
#endif
        return distance > t ? distance : F(distance);
    }
}
