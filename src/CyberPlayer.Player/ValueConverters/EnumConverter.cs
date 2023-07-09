using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CyberPlayer.Player.ValueConverters;

public class EnumConverter : IValueConverter
{
    public static readonly EnumConverter Instance = new();
    //Enum to string
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value != null)
        {
            return value.ToString();
        }
        else return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    //String to enum
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}