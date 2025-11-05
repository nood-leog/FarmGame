using Microsoft.Extensions.Logging;
using FarmGame.Services;
using FarmGame.Views; // Add this line

namespace FarmGame;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Register the DatabaseService as a Singleton
        builder.Services.AddSingleton<DatabaseService>();

        // Register InventoryPage as a Transient page so DI can inject its dependencies
        builder.Services.AddTransient<InventoryPage>(); // Add this line

        return builder.Build();
    }
}