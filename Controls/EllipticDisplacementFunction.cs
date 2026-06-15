using System;

namespace TryLiquidGlass.Controls;

public class EllipticDisplacementFunction
{
    public EllipticDisplacementFunction(double thickness, double length)  // length >= 2 * thickness
    {
        t = thickness;
        l = length;
    }

    private readonly double t;
    private readonly double l;

    public double Thickness => t;
    public double Length => l;

    private double F(double x)
    {
        double a = x / t - 1;
        return l * (1 - Math.Sqrt(1 - a * a));
    }

    public double Calculate(double x)
    {
        if (0 <= x && x <= t)
            return F(x);

        if (l - t - 1 <= x && x <= l)
            return -F(l - x);

        throw new ArgumentOutOfRangeException(nameof(x), "0 <= x <= Thickness or Length - Thickness <= x <= Length");
    }
}
