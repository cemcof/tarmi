using UnitsNet;

namespace Betrian.Models;

public static class UnitsExtensions
{
    public static Angle NormalizeAngle(this Angle self, bool toFullCircle = true)
    {
        double r = self.Radians;
        if (toFullCircle)
        {
            if (r > Math.PI * 2)
            {
                r %= (Math.PI * 2);
            }

            if (r < 0)
            {
                r = -(Math.Abs(r) % (Math.PI * 2));
                if (r < 0)
                {
                    r += Math.PI * 2;
                }
            }
        }
        else
        {
            if (r > Math.PI)
            {
                r %= (Math.PI * 2);
                if (r > Math.PI)
                {
                    r -= Math.PI * 2;
                }
            }

            if (r < -Math.PI)
            {
                r = -(Math.Abs(r) % (Math.PI * 2));
                if (r < -Math.PI)
                {
                    r += Math.PI * 2;
                }
            }
        }

        return Angle.FromRadians(r).ToUnit(self.Unit);
    }

    public static Angle Difference(this Angle angle, Angle otherAngle)
    {
        double val = angle.Radians - otherAngle.Radians;
        return Angle.FromRadians(val).ToUnit(angle.Unit).NormalizeAngle(false);
    }

    public static TUnit InvertSign<TUnit>(this TUnit value)
        where TUnit : IQuantity
            => (TUnit)Quantity.From((double)value.Value * -1, value.Unit);

    private static readonly Angle DefaultAngleToleration = Angle.FromRadians(0.01);

    /// <summary>
    /// Is angle in tolerance.
    /// </summary>
    /// <param name="angle">Angle to compare.</param>
    /// <param name="comparisonAngle">Angle to compare against.</param>
    /// <param name="toleration">Angle toleration.</param>
    /// <returns>True if angle is in toleration.</returns>
    public static bool IsInTolerance(this Angle angle, Angle comparisonAngle, Angle? toleration = null)
    {
        toleration ??= DefaultAngleToleration;
        return Math.Abs(angle.NormalizeAngle(true).Difference(comparisonAngle.NormalizeAngle(true)).Radians) <= toleration.Value.NormalizeAngle(true).Radians;
    }
}
