using FarmGame.Services; // Add this line

namespace FarmGame;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;

    public App(DatabaseService databaseService) // Inject DatabaseService here
    {
        InitializeComponent();

        _databaseService = databaseService;
        // IMPORTANT: Call and await InitializeAsync() here.
        // It's crucial to ensure tables are created before UI tries to read from them.
        Task.Run(async () => await _databaseService.InitializeAsync()).Wait(); // Blocking wait for app startup

        MainPage = new AppShell();
    }
}