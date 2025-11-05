using Microsoft.Extensions.Logging;
using FarmGame.Services;
using FarmGame.Views;
using FarmGame.ViewModels;
using Microsoft.Maui.Controls; // Add this for Application.Current.Dispatcher

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
        builder.Services.AddTransient<ShopPage>();
        builder.Services.AddTransient<ShopViewModel>();

        // Register FarmPage and FarmViewModel, passing the dispatcher
        builder.Services.AddTransient<FarmPage>();
        builder.Services.AddTransient<FarmViewModel>(serviceProvider =>
        {
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            var dispatcher = Application.Current.Dispatcher; // Get the current application's dispatcher
            return new FarmViewModel(databaseService, dispatcher);
        });

        return builder.Build();
    }
}