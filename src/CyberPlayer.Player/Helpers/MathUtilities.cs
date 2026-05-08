using System;

namespace CyberPlayer.Player.Helpers;

public static class MathUtilities
{
    public static bool GreaterThanOrClose(double value1, double value2) =>
        value1 > value2 || AreClose(value1, value2);
    
    public static bool GreaterThan(double value1, double value2) =>
        value1 > value2 && !AreClose(value1, value2);
    
    public static bool LessThan(double value1, double value2) =>
        value1 < value2 && !AreClose(value1, value2);
    
    public static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
            return true;
        var num1 = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.220446049250313E-16;
        var num2 = value1 - value2;
        return -num1 < num2 && num1 > num2;
    }
}
