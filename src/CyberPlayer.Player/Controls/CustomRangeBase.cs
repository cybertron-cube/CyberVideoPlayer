using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Utilities;

namespace CyberPlayer.Player.Controls;

public abstract class CustomRangeBase : RangeBase
{
    public static new readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<CustomRangeBase, double>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceValue);

    public static readonly StyledProperty<double> ToCoerceValueProperty = AvaloniaProperty.Register<CustomRangeBase, double>(
        nameof(ToCoerceValue), double.NaN);

    public static readonly StyledProperty<bool> ToCoerceValueIsMaxProperty = AvaloniaProperty.Register<CustomRangeBase, bool>(
        nameof(ToCoerceValueIsMax), default);

    public new double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public double ToCoerceValue
    {
        get => GetValue(ToCoerceValueProperty);
        set => SetValue(ToCoerceValueProperty, value);
    }
    public bool ToCoerceValueIsMax
    {
        get => GetValue(ToCoerceValueIsMaxProperty);
        set => SetValue(ToCoerceValueIsMaxProperty, value);
    }
    private static double CoerceValue(AvaloniaObject sender, double value)
    {
        if (ValidateDouble(value))
        {
            if (!double.IsNaN(sender.GetValue(ToCoerceValueProperty)))
            {
                if (sender.GetValue(ToCoerceValueIsMaxProperty) && value < sender.GetValue(ToCoerceValueProperty))
                {
                    return MathUtilities.Clamp(value, sender.GetValue(MinimumProperty), sender.GetValue(MaximumProperty));
                }
                else if (!sender.GetValue(ToCoerceValueIsMaxProperty) && value > sender.GetValue(ToCoerceValueProperty))
                {
                    return MathUtilities.Clamp(value, sender.GetValue(MinimumProperty), sender.GetValue(MaximumProperty));
                }
                else
                {
                    return sender.GetValue(ValueProperty);
                } 
            }
            else
            {
                return MathUtilities.Clamp(value, sender.GetValue(MinimumProperty), sender.GetValue(MaximumProperty));
            }
        }
        else
        {
            return sender.GetValue(ValueProperty);
        }
    }
    private static bool ValidateDouble(double value)
    {
        return !double.IsInfinity(value) && !double.IsNaN(value);
    }
}
