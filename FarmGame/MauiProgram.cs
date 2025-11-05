using Microsoft.Extensions.Logging;
using FarmGame.Services;
using FarmGame.Views;
using FarmGame.ViewModels; // Add this line

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
        builder.Services.AddSingleton<DatabaseService>();

        // Register pages and viewmodels for DI
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<ShopPage>();       // Add this line
        builder.Services.AddTransient<ShopViewModel>();  // Add this line

        return builder.Build();
    }
}