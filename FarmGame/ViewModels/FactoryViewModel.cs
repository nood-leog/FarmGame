using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls; // For Shell.Current.DisplayAlert and IDispatcher
using System.Linq; // For LINQ extensions

namespace FarmGame.ViewModels
{
    public class FactoryViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly IDispatcher _dispatcher;
        private PlayerState _currentPlayerState;
        private IDispatcherTimer? _factoryProcessingTimer;

        // Player Money (for context, though not directly used in Factory logic currently)
        private double _playerMoney;
        public double PlayerMoney
        {
            get => _playerMoney;
            set => SetProperty(ref _playerMoney, value);
        }

        public ObservableCollection<DisplayMachine> OwnedMachines { get; } = new ObservableCollection<DisplayMachine>();

        public FactoryViewModel(DatabaseService databaseService, IDispatcher dispatcher)
        {
            _databaseService = databaseService;
            _dispatcher = dispatcher;
        }

        public async Task LoadFactoryData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1) ?? new PlayerState(); // Ensure it's not null
            PlayerMoney = _currentPlayerState.Money;

            OwnedMachines.Clear();
            var playerMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();
            foreach (var playerMachine in playerMachines)
            {
                var machineDef = await _databaseService.GetItemAsync<MachineDefinition>(playerMachine.MachineDefinitionId);
                if (machineDef != null)
                {
                    var displayMachine = new DisplayMachine
                    {
                        PlayerOwnedMachine = playerMachine,
                        MachineDefinition = machineDef,
                        StartProcessingCommand = new Command<DisplayMachine>(async (dm) => await StartProcessing(dm!)), // Null-forgiving
                        CollectCommand = new Command<DisplayMachine>(async (dm) => await CollectOutput(dm!)) // Null-forgiving
                    };
                    await UpdateMachineDisplay(displayMachine); // Update its status and interactivity
                    OwnedMachines.Add(displayMachine);
                }
            }
            StartFactoryProcessingTimer(); // Start the timer to update processing progress
        }

        private async Task UpdateMachineDisplay(DisplayMachine displayMachine)
        {
            var playerMachine = displayMachine.PlayerOwnedMachine;
            var machineDef = displayMachine.MachineDefinition;

            displayMachine.CanCollect = false;
            displayMachine.CanStartProcessing = false;
            displayMachine.ProcessingProgress = 0;

            if (playerMachine.IsProcessing)
            {
                if (playerMachine.ProcessStartTime.HasValue && machineDef.ProcessingTimeSeconds > 0)
                {
                    TimeSpan elapsed = DateTime.UtcNow - playerMachine.ProcessStartTime.Value;
                    double progress = elapsed.TotalSeconds / machineDef.ProcessingTimeSeconds;

                    if (progress >= 1.0)
                    {
                        displayMachine.ProcessingProgress = 1.0;
                        displayMachine.StatusText = $"Ready to Collect {machineDef.OutputQuantity}x {(await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.OutputProduceDefinitionId))?.Name}";
                        displayMachine.CanCollect = true;
                    }
                    else
                    {
                        displayMachine.ProcessingProgress = progress;
                        string inputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.InputProduceDefinitionId))?.Name ?? "Unknown";
                        displayMachine.StatusText = $"Processing {inputProduceName} ({progress:P0})";
                    }
                }
                else
                {
                    // Should not happen, but reset if data is inconsistent
                    playerMachine.IsProcessing = false;
                    playerMachine.ProcessStartTime = null;
                    displayMachine.StatusText = "Error: Resetting...";
                    await _databaseService.SaveItemAsync(playerMachine);
                    await UpdateMachineDisplay(displayMachine); // Re-evaluate
                }
            }
            else // Machine is idle
            {
                string inputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.InputProduceDefinitionId))?.Name ?? "Unknown";
                var inputInventory = (await _databaseService.GetItemsAsync<InventoryItem>())
                                     .FirstOrDefault(i => i.ProduceDefinitionId == machineDef.InputProduceDefinitionId && !i.IsSeed);

                if (inputInventory != null && inputInventory.Quantity >= machineDef.InputQuantity)
                {
                    displayMachine.StatusText = $"Idle (Ready for {inputProduceName})";
                    displayMachine.CanStartProcessing = true;
                }
                else
                {
                    displayMachine.StatusText = $"Idle (Need {machineDef.InputQuantity}x {inputProduceName})";
                    displayMachine.CanStartProcessing = false;
                }
            }
        }

        private async Task StartProcessing(DisplayMachine displayMachine)
        {
            var playerMachine = displayMachine.PlayerOwnedMachine;
            var machineDef = displayMachine.MachineDefinition;

            if (playerMachine.IsProcessing)
            {
                await Shell.Current.DisplayAlert("Busy", $"{machineDef.Name} is already processing.", "OK");
                return;
            }

            var inputInventory = (await _databaseService.GetItemsAsync<InventoryItem>())
                                 .FirstOrDefault(i => i.ProduceDefinitionId == machineDef.InputProduceDefinitionId && !i.IsSeed);

            if (inputInventory == null || inputInventory.Quantity < machineDef.InputQuantity)
            {
                string inputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.InputProduceDefinitionId))?.Name ?? "Unknown";
                await Shell.Current.DisplayAlert("Not Enough Input", $"You need {machineDef.InputQuantity}x {inputProduceName} to start processing.", "OK");
                return;
            }

            // Consume input produce
            inputInventory.Quantity -= machineDef.InputQuantity;
            if (inputInventory.Quantity <= 0)
            {
                await _databaseService.DeleteItemAsync(inputInventory);
            }
            else
            {
                await _databaseService.SaveItemAsync(inputInventory);
            }

            // Start processing
            playerMachine.IsProcessing = true;
            playerMachine.ProcessStartTime = DateTime.UtcNow;
            playerMachine.InputProduceInMachineId = machineDef.InputProduceDefinitionId; // Store what's being processed
            playerMachine.InputQuantityInMachine = machineDef.InputQuantity;
            await _databaseService.SaveItemAsync(playerMachine);

            await UpdateMachineDisplay(displayMachine); // Update UI
            StartFactoryProcessingTimer(); // Ensure timer is running
            await Shell.Current.DisplayAlert("Processing Started", $"{machineDef.Name} started processing.", "OK");
        }

        private async Task CollectOutput(DisplayMachine displayMachine)
        {
            var playerMachine = displayMachine.PlayerOwnedMachine;
            var machineDef = displayMachine.MachineDefinition;

            if (!playerMachine.IsProcessing || !playerMachine.ProcessStartTime.HasValue ||
                (DateTime.UtcNow - playerMachine.ProcessStartTime.Value).TotalSeconds < machineDef.ProcessingTimeSeconds)
            {
                await Shell.Current.DisplayAlert("Not Ready", $"{machineDef.Name} is not finished processing yet.", "OK");
                return;
            }

            // Add output produce to inventory
            var existingOutputInventory = (await _databaseService.GetItemsAsync<InventoryItem>())
                                          .FirstOrDefault(i => i.ProduceDefinitionId == machineDef.OutputProduceDefinitionId && !i.IsSeed);

            if (existingOutputInventory != null)
            {
                existingOutputInventory.Quantity += machineDef.OutputQuantity;
                await _databaseService.SaveItemAsync(existingOutputInventory);
            }
            else
            {
                await _databaseService.SaveItemAsync(new InventoryItem
                {
                    ProduceDefinitionId = machineDef.OutputProduceDefinitionId,
                    Quantity = machineDef.OutputQuantity,
                    IsSeed = false
                });
            }

            // Reset machine state
            playerMachine.IsProcessing = false;
            playerMachine.ProcessStartTime = null;
            playerMachine.InputProduceInMachineId = null;
            playerMachine.InputQuantityInMachine = null;
            await _databaseService.SaveItemAsync(playerMachine);

            await UpdateMachineDisplay(displayMachine); // Update UI
            await Shell.Current.DisplayAlert("Collected", $"Collected {machineDef.OutputQuantity}x {(await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.OutputProduceDefinitionId))?.Name} from {machineDef.Name}.", "OK");
        }

        // --- Factory Processing Timer ---
        private void StartFactoryProcessingTimer()
        {
            _factoryProcessingTimer?.Stop();

            _factoryProcessingTimer = _dispatcher.CreateTimer();
            _factoryProcessingTimer.Interval = TimeSpan.FromSeconds(1); // Update progress every second
            _factoryProcessingTimer.Tick += async (s, e) =>
            {
                bool anyMachineProcessing = false;
                foreach (var displayMachine in OwnedMachines)
                {
                    if (displayMachine.PlayerOwnedMachine.IsProcessing)
                    {
                        anyMachineProcessing = true;
                        await UpdateMachineDisplay(displayMachine); // Update its progress
                    }
                }
                if (!anyMachineProcessing)
                {
                    _factoryProcessingTimer.Stop(); // Stop if no machines are processing
                }
            };
            _factoryProcessingTimer.Start();
        }

        public void OnDisappearing()
        {
            _factoryProcessingTimer?.Stop();
        }

        // INotifyPropertyChanged boilerplate
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string? propertyName = null,
            Action? onChanged = null)
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