// BoolToColorConverter.cs
using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls; // Add this for IValueConverter

namespace FarmGame.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) // Add '?'
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var parts = paramString.Split(',');
                if (parts.Length == 2)
                {
                    return boolValue ? Color.FromArgb(parts[0]) : Color.FromArgb(parts[1]);
                }
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) // Add '?'
        {
            throw new NotImplementedException();
        }
    }
}