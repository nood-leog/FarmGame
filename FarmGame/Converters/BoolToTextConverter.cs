using System.Globalization;
using Microsoft.Maui.Controls; // Add this using directive for IValueConverter

namespace FarmGame.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        // Add '?' to value and parameter
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var parts = paramString.Split(','); // Expected format: "TrueText,FalseText"
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value; // Fallback
        }

        // Add '?' to value and parameter
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}