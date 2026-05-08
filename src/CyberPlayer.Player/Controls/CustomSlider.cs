using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CyberPlayer.Player.Helpers;

namespace CyberPlayer.Player.Controls;

public class CustomSlider : Slider
{
    public static readonly StyledProperty<bool> IsSeekingProperty =
        AvaloniaProperty.Register<CustomSlider, bool>(
            nameof(IsSeeking),
            defaultValue: false,
            defaultBindingMode: BindingMode.OneWayToSource);

    public static readonly StyledProperty<double> ToCoerceValueProperty =
        AvaloniaProperty.Register<CustomSlider, double>(
            nameof(ToCoerceValue),
            defaultValue: double.NaN,
            defaultBindingMode: BindingMode.OneWay);

    public static readonly StyledProperty<bool> ToCoerceValueIsMaxProperty =
        AvaloniaProperty.Register<CustomSlider, bool>(
            nameof(ToCoerceValueIsMax),
            defaultBindingMode: BindingMode.OneWay);

    public bool IsSeeking
    {
        get => GetValue(IsSeekingProperty);
        private set => SetValue(IsSeekingProperty, value);
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
    
    private Track? _track;
    private Button? _decreaseButton;
    private Button? _increaseButton;

    static CustomSlider()
    {
        Thumb.DragStartedEvent.AddClassHandler<CustomSlider>((x, _) =>
        {
            x.IsSeeking = true;
        });

        Thumb.DragCompletedEvent.AddClassHandler<CustomSlider>((x, _) =>
        {
            x.IsSeeking = false;
        });
        
        ValueProperty.OverrideMetadata<CustomSlider>(
            new StyledPropertyMetadata<double>(
                coerce: CoerceValue));
    }
    
    private static double CoerceValue(AvaloniaObject sender, double value)
    {
        if (!double.IsFinite(value)
            || double.IsInfinity(sender.GetValue(ToCoerceValueProperty))
            || sender.GetValue(ToCoerceValueIsMaxProperty) && value > sender.GetValue(ToCoerceValueProperty)
            || !sender.GetValue(ToCoerceValueIsMaxProperty) && value < sender.GetValue(ToCoerceValueProperty))
            return sender.GetValue(ValueProperty);
        
        return Math.Clamp(value, sender.GetValue(MinimumProperty), sender.GetValue(MaximumProperty));
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _track = e.NameScope.Find<Track>("PART_Track");
        _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
        _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");
        
        if (_decreaseButton is not null)
        {
            _decreaseButton.AddHandler(PointerPressedEvent, (_,_) => { IsSeeking = true; }, RoutingStrategies.Tunnel);
            _decreaseButton.AddHandler(PointerReleasedEvent, (_,_) => { IsSeeking = false; }, RoutingStrategies.Tunnel);
        }
        if (_increaseButton is not null)
        {
            _increaseButton.AddHandler(PointerPressedEvent, (_,_) => { IsSeeking = true; }, RoutingStrategies.Tunnel);
            _increaseButton.AddHandler(PointerReleasedEvent, (_,_) => { IsSeeking = false; }, RoutingStrategies.Tunnel);
        }
    }
    
    internal void MoveThumb(Point position)
    {
        if (_track is null)
            return;

        var orient = Orientation == Orientation.Horizontal;
        var thumbLength = (orient
            ? _track.Thumb?.Bounds.Width ?? 0.0
            : _track.Thumb?.Bounds.Height ?? 0.0) + double.Epsilon;
        var trackLength = (orient
            ? _track.Bounds.Width
            : _track.Bounds.Height) - thumbLength;
        var trackPos = orient ? position.X : position.Y;
        var logicalPos = Math.Clamp((trackPos - thumbLength * 0.5) / trackLength, 0.0d, 1.0d);
        var invert = orient ?
            IsDirectionReversed ? 1 : 0 :
            IsDirectionReversed ? 0 : 1;
        var calcVal = Math.Abs(invert - logicalPos);
        var range = Maximum - Minimum;
        var finalValue = calcVal * range + Minimum;

        SetCurrentValue(ValueProperty, IsSnapToTickEnabled ? SnapToTick(finalValue) : finalValue);
    }
    
    private double SnapToTick(double value)
    {
        if (!IsSnapToTickEnabled) return value;
        
        var num1 = Minimum;
        var num2 = Maximum;
        var ticks = Ticks;
        if (ticks is not null && ticks.Count > 0)
        {
            foreach (var num3 in ticks)
            {
                if (MathUtilities.AreClose(num3, value))
                    return value;
                if (MathUtilities.LessThan(num3, value) && MathUtilities.GreaterThan(num3, num1))
                    num1 = num3;
                else if (MathUtilities.GreaterThan(num3, value) && MathUtilities.LessThan(num3, num2))
                    num2 = num3;
            }
        }
        else if (MathUtilities.GreaterThan(TickFrequency, 0.0))
        {
            num1 = Minimum + Math.Round((value - Minimum) / TickFrequency) * TickFrequency;
            num2 = Math.Min(Maximum, num1 + TickFrequency);
        }
        value = MathUtilities.GreaterThanOrClose(value, (num1 + num2) * 0.5) ? num2 : num1;
        return value;
    }
}
