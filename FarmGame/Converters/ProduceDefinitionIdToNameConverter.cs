using System.Globalization;
using FarmGame.Services; // Need access to the database service
using FarmGame.Models; // Need access to ProduceDefinition

namespace FarmGame.Converters
{
    public class ProduceDefinitionIdToNameConverter : IValueConverter
    {
        // This converter needs a reference to the DatabaseService.
        // We'll inject it via a constructor or property, but for IValueConverter,
        // it's easier to fetch it from the DI container if it's a singleton.
        private DatabaseService _databaseService;

        public ProduceDefinitionIdToNameConverter()
        {
            // This is a common way to resolve singletons in converters,
            // but it relies on Application.Current.Handler.MauiContext being available,
            // which it should be when UI elements are being created.
            _databaseService = Application.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>()!;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int produceDefId && _databaseService != null)
            {
                // Synchronously get the name (usually discouraged, but for converters, often tolerated)
                // In a real game, if performance issues arise, you might pre-cache all definitions.
                ProduceDefinition? produce = _databaseService.GetItemAsync<ProduceDefinition>(produceDefId).Result;
                return produce?.Name ?? $"ID:{produceDefId}";
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}