using Microsoft.Extensions.Logging;
using FarmGame.Services;
using FarmGame.Views;
using FarmGame.ViewModels;
using Microsoft.Maui.Controls;

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
        builder.Services.AddTransient<InventoryPage>(); // InventoryPage itself
        builder.Services.AddTransient<InventoryViewModel>(serviceProvider => // Register its ViewModel
        {
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            var dispatcher = Application.Current!.Dispatcher;
            return new InventoryViewModel(databaseService, dispatcher);
        });

        builder.Services.AddTransient<ShopPage>();
        builder.Services.AddTransient<ShopViewModel>(serviceProvider =>
        {
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            var dispatcher = Application.Current!.Dispatcher;
            return new ShopViewModel(databaseService, dispatcher);
        });

        builder.Services.AddTransient<FarmPage>();
        builder.Services.AddTransient<FarmViewModel>(serviceProvider =>
        {
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            var dispatcher = Application.Current!.Dispatcher;
            return new FarmViewModel(databaseService, dispatcher);
        });

        builder.Services.AddTransient<FactoryPage>();
        builder.Services.AddTransient<FactoryViewModel>(serviceProvider =>
        {
            var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            var dispatcher = Application.Current!.Dispatcher;
            return new FactoryViewModel(databaseService, dispatcher);
        });


        return builder.Build();
    }
}