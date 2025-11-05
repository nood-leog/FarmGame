using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls; // Add this for Shell.Current.DisplayAlert

namespace FarmGame.ViewModels
{
    // Helper classes for displaying shop items with purchase logic (add '?' for nullable events)
    public class ShopSeedDisplayItem : INotifyPropertyChanged
    {
        public SeedDefinition Seed { get; set; }
        private bool _canAfford;
        public bool CanAfford
        {
            get => _canAfford;
            set => SetProperty(ref _canAfford, value);
        }

        public ICommand BuyCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged; // Add '?'
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => // Add '?'
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "") // Add '?'
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ShopToolDisplayItem : INotifyPropertyChanged
    {
        public ToolDefinition Tool { get; set; }
        private bool _canAfford;
        public bool CanAfford
        {
            get => _canAfford;
            set => SetProperty(ref _canAfford, value);
        }
        private bool _isOwned;
        public bool IsOwned
        {
            get => _isOwned;
            set => SetProperty(ref _isOwned, value);
        }

        public ICommand BuyCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged; // Add '?'
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => // Add '?'
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "") // Add '?'
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ShopMachineDisplayItem : INotifyPropertyChanged
    {
        public MachineDefinition Machine { get; set; }
        private bool _canAfford;
        public bool CanAfford
        {
            get => _canAfford;
            set => SetProperty(ref _canAfford, value);
        }
        private bool _isOwned;
        public bool IsOwned
        {
            get => _isOwned;
            set => SetProperty(ref _isOwned, value);
        }

        public ICommand BuyCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged; // Add '?'
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => // Add '?'
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "") // Add '?'
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ShopViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private PlayerState _currentPlayerState;

        private double _playerMoney;
        public double PlayerMoney
        {
            get => _playerMoney;
            set => SetProperty(ref _playerMoney, value);
        }

        public ObservableCollection<ShopSeedDisplayItem> SeedsForSale { get; } = new ObservableCollection<ShopSeedDisplayItem>();
        public ObservableCollection<ShopToolDisplayItem> HoesForSale { get; } = new ObservableCollection<ShopToolDisplayItem>();
        public ObservableCollection<ShopToolDisplayItem> WateringCansForSale { get; } = new ObservableCollection<ShopToolDisplayItem>();
        public ObservableCollection<ShopMachineDisplayItem> MachinesForSale { get; } = new ObservableCollection<ShopMachineDisplayItem>();

        public ObservableCollection<DisplayInventoryItem> ItemsToSell { get; } = new ObservableCollection<DisplayInventoryItem>();
        public ICommand SellAllProduceCommand { get; }
        public ICommand SellItemCommand { get; } // Command for individual selling

        public ShopViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            SellAllProduceCommand = new Command(async () => await ExecuteSellAllProduceCommand());
            SellItemCommand = new Command<DisplayInventoryItem>(async (item) => await SellItem(item)); // Initialize individual sell command
        }

        public async Task LoadShopData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1);
            if (_currentPlayerState != null)
            {
                PlayerMoney = _currentPlayerState.Money;
            }

            var playerOwnedTools = await _databaseService.GetItemsAsync<PlayerOwnedTool>();
            var playerOwnedMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();

            SeedsForSale.Clear();
            var seedDefinitions = await _databaseService.GetItemsAsync<SeedDefinition>();
            foreach (var seedDef in seedDefinitions)
            {
                SeedsForSale.Add(new ShopSeedDisplayItem
                {
                    Seed = seedDef,
                    BuyCommand = new Command<SeedDefinition>(async (s) => await BuySeed(s))
                });
            }

            HoesForSale.Clear();
            WateringCansForSale.Clear();
            var toolDefinitions = await _databaseService.GetItemsAsync<ToolDefinition>();
            foreach (var toolDef in toolDefinitions)
            {
                var isOwned = playerOwnedTools.Any(pot => pot.ToolDefinitionId == toolDef.Id);
                var displayItem = new ShopToolDisplayItem
                {
                    Tool = toolDef,
                    IsOwned = isOwned,
                    BuyCommand = new Command<ToolDefinition>(async (t) => await BuyTool(t))
                };

                if (toolDef.Type == "Hoe")
                {
                    HoesForSale.Add(displayItem);
                }
                else if (toolDef.Type == "WateringCan")
                {
                    WateringCansForSale.Add(displayItem);
                }
            }

            MachinesForSale.Clear();
            var machineDefinitions = await _databaseService.GetItemsAsync<MachineDefinition>();
            foreach (var machineDef in machineDefinitions)
            {
                var isOwned = playerOwnedMachines.Any(pom => pom.MachineDefinitionId == machineDef.Id);
                MachinesForSale.Add(new ShopMachineDisplayItem
                {
                    Machine = machineDef,
                    IsOwned = isOwned,
                    BuyCommand = new Command<MachineDefinition>(async (m) => await BuyMachine(m))
                });
            }

            UpdateCanAffordStatus();
            await LoadSellableItems();
        }

        private void UpdateCanAffordStatus()
        {
            foreach (var seedItem in SeedsForSale)
            {
                seedItem.CanAfford = PlayerMoney >= seedItem.Seed.ShopCost;
            }
            foreach (var toolItem in HoesForSale)
            {
                toolItem.CanAfford = PlayerMoney >= toolItem.Tool.ShopCost;
            }
            foreach (var toolItem in WateringCansForSale)
            {
                toolItem.CanAfford = PlayerMoney >= toolItem.Tool.ShopCost;
            }
            foreach (var machineItem in MachinesForSale)
            {
                machineItem.CanAfford = PlayerMoney >= machineItem.Machine.ShopCost;
            }
        }

        // --- Buy Logic ---
        private async Task BuySeed(SeedDefinition seed)
        {
            if (_currentPlayerState.Money >= seed.ShopCost)
            {
                _currentPlayerState.Money -= seed.ShopCost;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                var existingInventoryItem = (await _databaseService.GetItemsAsync<InventoryItem>())
                                                .FirstOrDefault(i => i.ProduceDefinitionId == seed.Id && i.IsSeed); // Changed to seed.Id

                if (existingInventoryItem != null)
                {
                    existingInventoryItem.Quantity++;
                    await _databaseService.SaveItemAsync(existingInventoryItem);
                }
                else
                {
                    await _databaseService.SaveItemAsync(new InventoryItem
                    {
                        ProduceDefinitionId = seed.Id, // Store SeedDefinition.Id here when IsSeed is true
                        Quantity = 1,
                        IsSeed = true
                    });
                }
                UpdateCanAffordStatus();
                await Shell.Current.DisplayAlert("Purchased", $"You bought 1x {seed.Name}.", "OK"); // Use Shell.Current
                await LoadSellableItems();
            }
            else
            {
                await Shell.Current.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {seed.Name}.", "OK"); // Use Shell.Current
            }
        }

        private async Task BuyTool(ToolDefinition tool)
        {
            if (_currentPlayerState.Money >= tool.ShopCost)
            {
                var playerOwnedTools = await _databaseService.GetItemsAsync<PlayerOwnedTool>();
                if (playerOwnedTools.Any(pot => pot.ToolDefinitionId == tool.Id))
                {
                    await Shell.Current.DisplayAlert("Already Owned", $"You already own the {tool.Name}.", "OK"); // Use Shell.Current
                    return;
                }

                _currentPlayerState.Money -= tool.ShopCost;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                await _databaseService.SaveItemAsync(new PlayerOwnedTool { ToolDefinitionId = tool.Id });

                if (tool.Type == "Hoe" && (_currentPlayerState.SelectedHoeToolId == null || _currentPlayerState.SelectedHoeToolId < tool.Id))
                {
                    _currentPlayerState.SelectedHoeToolId = tool.Id;
                }
                else if (tool.Type == "WateringCan" && (_currentPlayerState.SelectedWaterToolId == null || _currentPlayerState.SelectedWaterToolId < tool.Id))
                {
                    _currentPlayerState.SelectedWaterToolId = tool.Id;
                    _currentPlayerState.MaxWater = tool.MaxWaterCapacity ?? _currentPlayerState.MaxWater;
                    _currentPlayerState.WaterRefillRate = tool.WaterRefillRate ?? _currentPlayerState.WaterRefillRate;
                    await _databaseService.SaveItemAsync(_currentPlayerState);
                }

                UpdateCanAffordStatus();
                var shopToolItem = HoesForSale.FirstOrDefault(t => t.Tool.Id == tool.Id)
                                 ?? WateringCansForSale.FirstOrDefault(t => t.Tool.Id == tool.Id);
                if (shopToolItem != null) shopToolItem.IsOwned = true;
                await Shell.Current.DisplayAlert("Purchased", $"You bought the {tool.Name}.", "OK"); // Use Shell.Current
            }
            else
            {
                await Shell.Current.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {tool.Name}.", "OK"); // Use Shell.Current
            }
        }

        private async Task BuyMachine(MachineDefinition machine)
        {
            if (_currentPlayerState.Money >= machine.ShopCost)
            {
                var playerOwnedMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();
                if (playerOwnedMachines.Any(pom => pom.MachineDefinitionId == machine.Id))
                {
                    await Shell.Current.DisplayAlert("Already Owned", $"You already own the {machine.Name}.", "OK"); // Use Shell.Current
                    return;
                }

                _currentPlayerState.Money -= machine.ShopCost;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                await _databaseService.SaveItemAsync(new PlayerOwnedMachine { MachineDefinitionId = machine.Id, IsProcessing = false });

                UpdateCanAffordStatus();
                var shopMachineItem = MachinesForSale.FirstOrDefault(m => m.Machine.Id == machine.Id);
                if (shopMachineItem != null) shopMachineItem.IsOwned = true;
                await Shell.Current.DisplayAlert("Purchased", $"You bought the {machine.Name}.", "OK"); // Use Shell.Current
            }
            else
            {
                await Shell.Current.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {machine.Name}.", "OK"); // Use Shell.Current
            }
        }

        // --- Sell Logic ---
        private async Task LoadSellableItems()
        {
            ItemsToSell.Clear();
            var inventory = await _databaseService.GetItemsAsync<InventoryItem>();
            foreach (var item in inventory.Where(i => !i.IsSeed && i.Quantity > 0))
            {
                var produceDefinition = await _databaseService.GetItemAsync<ProduceDefinition>(item.ProduceDefinitionId);
                if (produceDefinition != null)
                {
                    ItemsToSell.Add(new DisplayInventoryItem
                    {
                        Id = item.Id,
                        ProduceDefinitionId = item.ProduceDefinitionId, // Add ProduceDefinitionId here
                        Name = produceDefinition.Name,
                        Quantity = item.Quantity,
                        IsSeed = item.IsSeed,
                        BaseSellPrice = produceDefinition.BaseSellPrice
                    });
                }
            }
        }

        public async Task SellItem(DisplayInventoryItem itemToSell)
        {
            var inventoryItem = await _databaseService.GetItemAsync<InventoryItem>(itemToSell.Id);
            if (inventoryItem != null && inventoryItem.Quantity > 0)
            {
                _currentPlayerState.Money += itemToSell.BaseSellPrice;
                _currentPlayerState.Money = Math.Round(_currentPlayerState.Money, 2);
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                inventoryItem.Quantity--;
                if (inventoryItem.Quantity <= 0)
                {
                    await _databaseService.DeleteItemAsync(inventoryItem);
                }
                else
                {
                    await _databaseService.SaveItemAsync(inventoryItem);
                }
                await Shell.Current.DisplayAlert("Sold", $"You sold 1x {itemToSell.Name}.", "OK"); // Use Shell.Current
                await LoadSellableItems();
                UpdateCanAffordStatus();
            }
        }

        private async Task ExecuteSellAllProduceCommand()
        {
            if (!ItemsToSell.Any())
            {
                await Shell.Current.DisplayAlert("Nothing to Sell", "Your inventory is empty of sellable produce.", "OK");
                return;
            }

            double totalSoldValue = 0;
            foreach (var item in ItemsToSell.ToList())
            {
                var inventoryItem = await _databaseService.GetItemAsync<InventoryItem>(item.Id);
                if (inventoryItem != null && inventoryItem.Quantity > 0)
                {
                    totalSoldValue += (item.BaseSellPrice * inventoryItem.Quantity);
                    inventoryItem.Quantity = 0;
                    await _databaseService.SaveItemAsync(inventoryItem);
                }
            }
            _currentPlayerState.Money += totalSoldValue;
            _currentPlayerState.Money = Math.Round(_currentPlayerState.Money, 2);
            await _databaseService.SaveItemAsync(_currentPlayerState);
            PlayerMoney = _currentPlayerState.Money;

            await Shell.Current.DisplayAlert("Sold All", $"You sold all produce for ${totalSoldValue:F2}.", "OK");
            await LoadSellableItems();
            UpdateCanAffordStatus();
        }

        public event PropertyChangedEventHandler? PropertyChanged; // Add '?'

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) // Add '?'
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
}