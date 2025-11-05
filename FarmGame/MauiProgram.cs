using Microsoft.Extensions.Logging;
using FarmGame.Services; // Add this line

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
        // This ensures only one instance of the database service is created and used throughout the app
        builder.Services.AddSingleton<DatabaseService>();

        // Register your views if they need access to the DatabaseService directly
        // (Often ViewModels consume services, but for simplicity we can register pages too if needed)
        // builder.Services.AddTransient<Views.FarmPage>(); // Example if FarmPage directly needs DatabaseService

        return builder.Build();
    }
}