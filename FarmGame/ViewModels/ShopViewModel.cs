using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input; // For ICommand

namespace FarmGame.ViewModels
{
    // Helper classes for displaying shop items with purchase logic
    public class ShopSeedDisplayItem : INotifyPropertyChanged
    {
        public SeedDefinition Seed { get; set; }
        private bool _canAfford;
        public bool CanAfford
        {
            get => _canAfford;
            set => SetProperty(ref _canAfford, value);
        }

        public ICommand BuyCommand { get; set; } // Command to buy this specific item

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
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
        private PlayerState _currentPlayerState; // Keep a reference to the current player state

        // Properties for UI binding
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

        // For the "Sell" tab (we'll implement this later)
        public ObservableCollection<DisplayInventoryItem> ItemsToSell { get; } = new ObservableCollection<DisplayInventoryItem>();
        public ICommand SellAllProduceCommand { get; } // A command to sell all non-seed inventory

        public ShopViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            SellAllProduceCommand = new Command(async () => await ExecuteSellAllProduceCommand());
        }

        public async Task LoadShopData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1);
            if (_currentPlayerState != null)
            {
                PlayerMoney = _currentPlayerState.Money;
            }

            // Get player owned tools/machines for "IsOwned" status
            var playerOwnedTools = await _databaseService.GetItemsAsync<PlayerOwnedTool>();
            var playerOwnedMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();

            // --- Load Seeds ---
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

            // --- Load Tools (Hoes and Watering Cans) ---
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

            // --- Load Machines ---
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
            await LoadSellableItems(); // Load items for the sell tab
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
                PlayerMoney = _currentPlayerState.Money; // Update UI

                // Add seed to inventory
                var existingInventoryItem = (await _databaseService.GetItemsAsync<InventoryItem>())
                                                .FirstOrDefault(i => i.ProduceDefinitionId == seed.HarvestsProduceDefinitionId && i.IsSeed);

                if (existingInventoryItem != null)
                {
                    existingInventoryItem.Quantity++;
                    await _databaseService.SaveItemAsync(existingInventoryItem);
                }
                else
                {
                    await _databaseService.SaveItemAsync(new InventoryItem
                    {
                        ProduceDefinitionId = seed.HarvestsProduceDefinitionId, // IMPORTANT: refers to the produce it grows
                        Quantity = 1,
                        IsSeed = true
                    });
                }
                UpdateCanAffordStatus();
                await LoadSellableItems(); // Refresh sellable items, though buying seeds usually doesn't affect it.
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {seed.Name}.", "OK");
            }
        }

        private async Task BuyTool(ToolDefinition tool)
        {
            if (_currentPlayerState.Money >= tool.ShopCost)
            {
                var playerOwnedTools = await _databaseService.GetItemsAsync<PlayerOwnedTool>();
                if (playerOwnedTools.Any(pot => pot.ToolDefinitionId == tool.Id))
                {
                    await Application.Current.MainPage.DisplayAlert("Already Owned", $"You already own the {tool.Name}.", "OK");
                    return;
                }

                _currentPlayerState.Money -= tool.ShopCost;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                await _databaseService.SaveItemAsync(new PlayerOwnedTool { ToolDefinitionId = tool.Id });

                // Update selected tool if it's an upgrade for the currently selected one
                if (tool.Type == "Hoe" && _currentPlayerState.SelectedHoeToolId < tool.Id) // Simple upgrade logic based on ID
                {
                    _currentPlayerState.SelectedHoeToolId = tool.Id;
                }
                else if (tool.Type == "WateringCan" && _currentPlayerState.SelectedWaterToolId < tool.Id) // Simple upgrade logic
                {
                    _currentPlayerState.SelectedWaterToolId = tool.Id;
                    _currentPlayerState.MaxWater = tool.MaxWaterCapacity ?? _currentPlayerState.MaxWater; // Update max water
                    _currentPlayerState.WaterRefillRate = tool.WaterRefillRate ?? _currentPlayerState.WaterRefillRate; // Update refill rate
                    // Consider resetting CurrentWater if you want to refill on upgrade or keep it current
                    await _databaseService.SaveItemAsync(_currentPlayerState); // Save updated water stats
                }

                UpdateCanAffordStatus();
                // Update IsOwned status for the specific tool item
                var shopToolItem = HoesForSale.FirstOrDefault(t => t.Tool.Id == tool.Id)
                                 ?? WateringCansForSale.FirstOrDefault(t => t.Tool.Id == tool.Id);
                if (shopToolItem != null) shopToolItem.IsOwned = true; // Update UI
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {tool.Name}.", "OK");
            }
        }

        private async Task BuyMachine(MachineDefinition machine)
        {
            if (_currentPlayerState.Money >= machine.ShopCost)
            {
                var playerOwnedMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();
                if (playerOwnedMachines.Any(pom => pom.MachineDefinitionId == machine.Id))
                {
                    await Application.Current.MainPage.DisplayAlert("Already Owned", $"You already own the {machine.Name}.", "OK");
                    return;
                }

                _currentPlayerState.Money -= machine.ShopCost;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                PlayerMoney = _currentPlayerState.Money;

                await _databaseService.SaveItemAsync(new PlayerOwnedMachine { MachineDefinitionId = machine.Id, IsProcessing = false });

                UpdateCanAffordStatus();
                var shopMachineItem = MachinesForSale.FirstOrDefault(m => m.Machine.Id == machine.Id);
                if (shopMachineItem != null) shopMachineItem.IsOwned = true; // Update UI
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Cannot Afford", $"You don't have enough money to buy {machine.Name}.", "OK");
            }
        }

        // --- Sell Logic ---
        private async Task LoadSellableItems()
        {
            ItemsToSell.Clear();
            var inventory = await _databaseService.GetItemsAsync<InventoryItem>();
            foreach (var item in inventory.Where(i => !i.IsSeed && i.Quantity > 0)) // Only non-seed items with quantity > 0
            {
                var produceDefinition = await _databaseService.GetItemAsync<ProduceDefinition>(item.ProduceDefinitionId);
                if (produceDefinition != null)
                {
                    ItemsToSell.Add(new DisplayInventoryItem
                    {
                        Id = item.Id, // This Id refers to the InventoryItem's PK
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
                _currentPlayerState.Money += itemToSell.BaseSellPrice; // Sell one unit
                _currentPlayerState.Money = Math.Round(_currentPlayerState.Money, 2); // Round money to 2 decimal places
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

                await LoadSellableItems(); // Refresh the list
                UpdateCanAffordStatus(); // Update buy buttons
            }
        }

        private async Task ExecuteSellAllProduceCommand()
        {
            // Simple approach: Iterate and sell one by one. For performance, could do a batch update.
            foreach (var item in ItemsToSell.ToList()) // ToList to avoid modification during enumeration
            {
                var inventoryItem = await _databaseService.GetItemAsync<InventoryItem>(item.Id);
                if (inventoryItem != null && inventoryItem.Quantity > 0)
                {
                    _currentPlayerState.Money += (item.BaseSellPrice * inventoryItem.Quantity);
                    inventoryItem.Quantity = 0; // Clear quantity
                    await _databaseService.SaveItemAsync(inventoryItem); // Update item quantity to 0 in DB
                }
            }
            _currentPlayerState.Money = Math.Round(_currentPlayerState.Money, 2); // Round money
            await _databaseService.SaveItemAsync(_currentPlayerState);
            PlayerMoney = _currentPlayerState.Money;

            await LoadSellableItems(); // Refresh the list
            UpdateCanAffordStatus(); // Update buy buttons
        }

        // INotifyPropertyChanged boilerplate
        public event PropertyChangedEventHandler? PropertyChanged;

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
}