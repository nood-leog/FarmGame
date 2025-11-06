using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // For IDispatcher

namespace FarmGame.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private PlayerState _currentPlayerState; // Keep a reference to the current player state
        private IDispatcherTimer? _waterRefillTimer;

        private double _playerMoney;
        public double PlayerMoney
        {
            get => _playerMoney;
            set => SetProperty(ref _playerMoney, value);
        }

        private string? _waterCanStatus;
        public string? WaterCanStatus
        {
            get => _waterCanStatus;
            set => SetProperty(ref _waterCanStatus, value);
        }

        public ObservableCollection<DisplayInventoryItem> InventoryItems { get; } = new ObservableCollection<DisplayInventoryItem>();

        public InventoryViewModel(DatabaseService databaseService, IDispatcher dispatcher) : base(dispatcher)
        {
            _databaseService = databaseService;
        }

        public async Task LoadInventoryData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1) ?? new PlayerState(); // Ensure it's not null
            PlayerMoney = _currentPlayerState.Money;
            UpdateWaterCanStatus();
            StartWaterRefillTimer();

            InventoryItems.Clear();
            var inventory = await _databaseService.GetItemsAsync<InventoryItem>();
            foreach (var item in inventory)
            {
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
                    ProduceDefinitionId = item.ProduceDefinitionId,
                    Name = itemName,
                    Quantity = item.Quantity,
                    IsSeed = item.IsSeed,
                    BaseSellPrice = itemSellPrice
                });
            }
        }

        private void UpdateWaterCanStatus()
        {
            if (_currentPlayerState != null)
            {
                WaterCanStatus = $"{_currentPlayerState.CurrentWater:F0}/{_currentPlayerState.MaxWater:F0}";
            }
            else
            {
                WaterCanStatus = "N/A";
            }
        }

        private void StartWaterRefillTimer()
        {
            _waterRefillTimer?.Stop();

            // Only start if we have player state and a valid refill rate
            if (_currentPlayerState != null && _currentPlayerState.WaterRefillRate > 0)
            {
                _waterRefillTimer = _dispatcher.CreateTimer();
                _waterRefillTimer.Interval = TimeSpan.FromSeconds(1);
                _waterRefillTimer.Tick += async (s, e) =>
                {
                    if (_currentPlayerState.CurrentWater < _currentPlayerState.MaxWater)
                    {
                        _currentPlayerState.CurrentWater += _currentPlayerState.WaterRefillRate;
                        if (_currentPlayerState.CurrentWater > _currentPlayerState.MaxWater)
                        {
                            _currentPlayerState.CurrentWater = _currentPlayerState.MaxWater;
                        }
                        UpdateWaterCanStatus();
                        await _databaseService.SaveItemAsync(_currentPlayerState);
                    }
                    else
                    {
                        _waterRefillTimer.Stop();
                        await _databaseService.SaveItemAsync(_currentPlayerState); // Save final state
                    }
                };
                _waterRefillTimer.Start();
            }
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing(); // Stop message timer from base
            _waterRefillTimer?.Stop(); // Stop local timer
        }
    }
}