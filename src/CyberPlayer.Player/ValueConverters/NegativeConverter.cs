using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CyberPlayer.Player.ValueConverters;

public class NegativeConverter : IValueConverter
{
    public static readonly NegativeConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double num)
        {
            return num * -1;
        }
        else return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (double?)value * -1;
    }
}