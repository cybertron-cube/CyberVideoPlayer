using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CyberPlayer.Player.ValueConverters;

public class DoublePercentConverter : IValueConverter
{
    public static readonly DoublePercentConverter Instance = new();
    //Enum to string
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return Math.Round(d, 0).ToString("0") + '%';
        }
        else return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    //String to enum
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}