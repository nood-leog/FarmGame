using FarmGame.Services;
using FarmGame.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FarmGame.Views;

public partial class InventoryPage : ContentPage, INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;

    private double _playerMoney;
    public double PlayerMoney
    {
        get => _playerMoney;
        set => SetProperty(ref _playerMoney, value);
    }

    private string? _waterCanStatus; // Add '?'
    public string? WaterCanStatus
    {
        get => _waterCanStatus;
        set => SetProperty(ref _waterCanStatus, value);
    }

    public ObservableCollection<DisplayInventoryItem> InventoryItems { get; } = new ObservableCollection<DisplayInventoryItem>();

    public InventoryPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        this.BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInventoryData();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _waterTimer?.Stop();
    }

    private async Task LoadInventoryData()
    {
        var playerState = await _databaseService.GetItemAsync<PlayerState>(1);
        if (playerState != null)
        {
            PlayerMoney = playerState.Money;
            WaterCanStatus = $"{playerState.CurrentWater:F0}/{playerState.MaxWater:F0}";

            StartWaterRefillTimer(playerState);
        }

        InventoryItems.Clear();
        var inventory = await _databaseService.GetItemsAsync<InventoryItem>();
        foreach (var item in inventory)
        {
            // If it's a seed, we need its SeedDefinition name. If it's produce, ProduceDefinition name.
            string itemName = "Unknown";
            double itemSellPrice = 0;

            if (item.IsSeed)
            {
                var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(item.ProduceDefinitionId);
                if (seedDef != null)
                {
                    itemName = seedDef.Name;
                    itemSellPrice = seedDef.ShopCost; // For seeds, display purchase price.
                }
            }
            else
            {
                var produceDefinition = await _databaseService.GetItemAsync<ProduceDefinition>(item.ProduceDefinitionId);
                if (produceDefinition != null)
                {
                    itemName = produceDefinition.Name;
                    itemSellPrice = produceDefinition.BaseSellPrice;
                }
            }

            InventoryItems.Add(new DisplayInventoryItem
            {
                Id = item.Id,
                ProduceDefinitionId = item.ProduceDefinitionId, // Include ProduceDefinitionId
                Name = itemName,
                Quantity = item.Quantity,
                IsSeed = item.IsSeed,
                BaseSellPrice = itemSellPrice
            });
        }
    }

    private IDispatcherTimer? _waterTimer; // Add '?'

    private void StartWaterRefillTimer(PlayerState playerState)
    {
        _waterTimer?.Stop();

        _waterTimer = Dispatcher.CreateTimer(); // Dispatcher property is available on ContentPage
        _waterTimer.Interval = TimeSpan.FromSeconds(1);
        _waterTimer.Tick += async (s, e) =>
        {
            if (playerState.CurrentWater < playerState.MaxWater)
            {
                playerState.CurrentWater += playerState.WaterRefillRate;
                if (playerState.CurrentWater > playerState.MaxWater)
                {
                    playerState.CurrentWater = playerState.MaxWater;
                }
                WaterCanStatus = $"{playerState.CurrentWater:F0}/{playerState.MaxWater:F0}";

                if (playerState.CurrentWater < playerState.MaxWater || Math.Abs(playerState.CurrentWater - playerState.MaxWater) < 0.01) // Save when full or changed
                {
                    await _databaseService.SaveItemAsync(playerState);
                }
            }
            else
            {
                _waterTimer.Stop();
                await _databaseService.SaveItemAsync(playerState);
            }
        };
        _waterTimer.Start();
    }

    public new event PropertyChangedEventHandler? PropertyChanged; // Add 'new' and '?'

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null) // Add 'new' and '?'
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string? propertyName = null, // Add '?'
        Action? onChanged = null) // Add '?'
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
}