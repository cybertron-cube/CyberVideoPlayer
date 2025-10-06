using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CyberPlayer.Player.ValueConverters;

public class NegativeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i * -1,
            double d => d * -1,
            _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
        };
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i * -1,
            double d => d * -1,
            _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
        };
    }
}
