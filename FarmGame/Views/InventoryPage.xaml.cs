using FarmGame.Services;
using FarmGame.Models;
using System.Collections.ObjectModel; // For ObservableCollection
using System.ComponentModel; // For INotifyPropertyChanged
using System.Runtime.CompilerServices; // For CallerMemberName attribute

namespace FarmGame.Views;

public partial class InventoryPage : ContentPage, INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;

    // Properties for UI binding
    private double _playerMoney;
    public double PlayerMoney
    {
        get => _playerMoney;
        set => SetProperty(ref _playerMoney, value);
    }

    private string _waterCanStatus;
    public string WaterCanStatus
    {
        get => _waterCanStatus;
        set => SetProperty(ref _waterCanStatus, value);
    }

    public ObservableCollection<DisplayInventoryItem> InventoryItems { get; } = new ObservableCollection<DisplayInventoryItem>();

    public InventoryPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        this.BindingContext = this; // Set the page's code-behind as its own BindingContext
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInventoryData();
    }

    private async Task LoadInventoryData()
    {
        // 1. Load Player State
        var playerState = await _databaseService.GetItemAsync<PlayerState>(1);
        if (playerState != null)
        {
            PlayerMoney = playerState.Money;
            WaterCanStatus = $"{playerState.CurrentWater:F0}/{playerState.MaxWater:F0}";

            // Optional: Start a timer to update water if on this page
            // For a real game, this would be in a game loop service
            StartWaterRefillTimer(playerState);
        }

        // 2. Load Inventory Items
        var inventory = await _databaseService.GetItemsAsync<InventoryItem>();
        InventoryItems.Clear(); // Clear existing items before repopulating

        foreach (var item in inventory)
        {
            var produceDefinition = await _databaseService.GetItemAsync<ProduceDefinition>(item.ProduceDefinitionId);
            if (produceDefinition != null)
            {
                InventoryItems.Add(new DisplayInventoryItem
                {
                    Id = item.Id,
                    Name = produceDefinition.Name,
                    Quantity = item.Quantity,
                    IsSeed = item.IsSeed,
                    BaseSellPrice = produceDefinition.BaseSellPrice
                });
            }
        }
    }

    // --- Water Refill Logic (Simple Page-level Timer) ---
    private IDispatcherTimer _waterTimer;

    private void StartWaterRefillTimer(PlayerState playerState)
    {
        // Stop any existing timer to avoid multiple timers running
        _waterTimer?.Stop();

        _waterTimer = Dispatcher.CreateTimer();
        _waterTimer.Interval = TimeSpan.FromSeconds(1); // Update every second
        _waterTimer.Tick += async (s, e) =>
        {
            playerState.CurrentWater += playerState.WaterRefillRate;
            if (playerState.CurrentWater > playerState.MaxWater)
            {
                playerState.CurrentWater = playerState.MaxWater;
            }
            WaterCanStatus = $"{playerState.CurrentWater:F0}/{playerState.MaxWater:F0}";

            // Only save if water actually changed (or periodically, depends on game loop)
            if (playerState.CurrentWater < playerState.MaxWater)
            {
                await _databaseService.SaveItemAsync(playerState);
            }
            else
            {
                _waterTimer.Stop(); // Stop timer if full
                await _databaseService.SaveItemAsync(playerState); // Save final state
            }
        };
        _waterTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _waterTimer?.Stop(); // Stop the timer when leaving the page
    }


    // --- INotifyPropertyChanged Implementation ---
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string propertyName = "",
        Action onChanged = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
}