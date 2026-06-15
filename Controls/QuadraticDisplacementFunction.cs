using System;

namespace TryLiquidGlass.Controls;

public class QuadraticDisplacementFunction
{
    public QuadraticDisplacementFunction(double thickness, double length)  // length >= 2 * thickness
    {
        Thickness = thickness;
        Length = length;

        // f'(t) = -2
        //a = (length - 2 * thickness) / (thickness * thickness);
        //b = -2 * (length - thickness) / thickness;

        // f'(t) = -1
        //a = (length - thickness) / (thickness * thickness);
        //b = 1 - (2 * length) / thickness;

        // f'(t) = 0
        a = length / thickness / thickness;
        b = -2 * length / thickness;
    }

    public double Thickness { get; }
    public double Length { get; }

    private readonly double a;
    private readonly double b;

    private double F(double x) => a * x * x + b * x + Length;

    public double Calculate(double x)
    {
        if (0 <= x && x <= Thickness)
            return F(x);

        if (Length - Thickness - 1 <= x && x <= Length)
            return -F(Length - x);

        throw new ArgumentOutOfRangeException(nameof(x), "0 <= x <= Thickness or Length - Thickness <= x <= Length");
    }
}
