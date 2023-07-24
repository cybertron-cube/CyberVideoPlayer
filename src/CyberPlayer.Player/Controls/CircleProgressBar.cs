using Avalonia;
using Avalonia.Controls;

namespace CyberPlayer.Player.Controls;

public class CircleProgressBar : ContentControl
{
    private double _value;

    public static readonly DirectProperty<CircleProgressBar, double> ValueProperty = AvaloniaProperty.RegisterDirect<CircleProgressBar, double>(
        nameof(Value), o => o.Value, (o, v) => o.Value = v);

    public double Value
    {
        get => _value;
        set
        {
            var convertedValue = (value / _maximum) * 360;
            SetAndRaise(ValueProperty, ref _value, convertedValue);
        }
    }

    private double _maximum = 1;

    public static readonly DirectProperty<CircleProgressBar, double> MaximumProperty = AvaloniaProperty.RegisterDirect<CircleProgressBar, double>(
        nameof(Maximum), o => o.Maximum, (o, v) => o.Maximum = v);

    public double Maximum
    {
        get => _maximum;
        set => SetAndRaise(MaximumProperty, ref _maximum, value);
    }

    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<CircleProgressBar, double>(
        nameof(StrokeWidth));

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }
}