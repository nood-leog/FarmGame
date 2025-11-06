using FarmGame.Models; // This using is crucial for referencing Models.DisplayMachine
using FarmGame.Services;
using System.Collections.ObjectModel;
// No longer needs: using System.ComponentModel; using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Linq;

namespace FarmGame.ViewModels
{
    // The DisplayMachine class definition should NO LONGER BE HERE.
    // It should ONLY be in Models/DisplayMachine.cs

    public class FactoryViewModel : BaseViewModel // Inherit from BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        // _dispatcher is now handled by BaseViewModel
        private PlayerState _currentPlayerState;
        private IDispatcherTimer? _factoryProcessingTimer;

        private double _playerMoney;
        public double PlayerMoney
        {
            get => _playerMoney;
            set => SetProperty(ref _playerMoney, value); // SetProperty is from BaseViewModel
        }

        public ObservableCollection<DisplayMachine> OwnedMachines { get; } = new ObservableCollection<DisplayMachine>();

        public FactoryViewModel(DatabaseService databaseService, IDispatcher dispatcher) : base(dispatcher) // Pass dispatcher to base
        {
            _databaseService = databaseService;
        }

        public async Task LoadFactoryData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1) ?? new PlayerState();
            PlayerMoney = _currentPlayerState.Money;

            OwnedMachines.Clear();
            var playerMachines = await _databaseService.GetItemsAsync<PlayerOwnedMachine>();
            foreach (var playerMachine in playerMachines)
            {
                var machineDef = await _databaseService.GetItemAsync<MachineDefinition>(playerMachine.MachineDefinitionId);
                if (machineDef != null)
                {
                    var displayMachine = new DisplayMachine // This now correctly references FarmGame.Models.DisplayMachine
                    {
                        PlayerOwnedMachine = playerMachine,
                        MachineDefinition = machineDef,
                        StartProcessingCommand = new Command<DisplayMachine>(async (dm) => await StartProcessing(dm!)),
                        CollectCommand = new Command<DisplayMachine>(async (dm) => await CollectOutput(dm!))
                    };
                    await UpdateMachineDisplay(displayMachine);
                    OwnedMachines.Add(displayMachine);
                }
            }
            StartFactoryProcessingTimer();
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
                        string outputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.OutputProduceDefinitionId))?.Name ?? "Unknown";
                        displayMachine.StatusText = $"Ready to Collect {machineDef.OutputQuantity}x {outputProduceName}";
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
                    playerMachine.IsProcessing = false;
                    playerMachine.ProcessStartTime = null;
                    displayMachine.StatusText = "Error: Resetting...";
                    await _databaseService.SaveItemAsync(playerMachine);
                    await UpdateMachineDisplay(displayMachine);
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
                ShowMessage($"{machineDef.Name} is already processing.", true);
                return;
            }

            var inputInventory = (await _databaseService.GetItemsAsync<InventoryItem>())
                                 .FirstOrDefault(i => i.ProduceDefinitionId == machineDef.InputProduceDefinitionId && !i.IsSeed);

            if (inputInventory == null || inputInventory.Quantity < machineDef.InputQuantity)
            {
                string inputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.InputProduceDefinitionId))?.Name ?? "Unknown";
                ShowMessage($"You need {machineDef.InputQuantity}x {inputProduceName} to start processing.", true);
                return;
            }

            inputInventory.Quantity -= machineDef.InputQuantity;
            if (inputInventory.Quantity <= 0)
            {
                await _databaseService.DeleteItemAsync(inputInventory);
            }
            else
            {
                await _databaseService.SaveItemAsync(inputInventory);
            }

            playerMachine.IsProcessing = true;
            playerMachine.ProcessStartTime = DateTime.UtcNow;
            playerMachine.InputProduceInMachineId = machineDef.InputProduceDefinitionId;
            playerMachine.InputQuantityInMachine = machineDef.InputQuantity;
            await _databaseService.SaveItemAsync(playerMachine);

            await UpdateMachineDisplay(displayMachine);
            StartFactoryProcessingTimer();
            ShowMessage($"{machineDef.Name} started processing.", false);
        }

        private async Task CollectOutput(DisplayMachine displayMachine)
        {
            var playerMachine = displayMachine.PlayerOwnedMachine;
            var machineDef = displayMachine.MachineDefinition;

            if (!playerMachine.IsProcessing || !playerMachine.ProcessStartTime.HasValue ||
                (DateTime.UtcNow - playerMachine.ProcessStartTime.Value).TotalSeconds < machineDef.ProcessingTimeSeconds)
            {
                ShowMessage($"{machineDef.Name} is not finished processing yet.", true);
                return;
            }

            string outputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.OutputProduceDefinitionId))?.Name ?? "Unknown";

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

            playerMachine.IsProcessing = false;
            playerMachine.ProcessStartTime = null;
            playerMachine.InputProduceInMachineId = null;
            playerMachine.InputQuantityInMachine = null;
            await _databaseService.SaveItemAsync(playerMachine);

            await UpdateMachineDisplay(displayMachine);
            ShowMessage($"Collected {machineDef.OutputQuantity}x {outputProduceName} from {machineDef.Name}.", false);
        }

        // --- Factory Processing Timer ---
        private void StartFactoryProcessingTimer()
        {
            _factoryProcessingTimer?.Stop();

            _factoryProcessingTimer = _dispatcher.CreateTimer();
            _factoryProcessingTimer.Interval = TimeSpan.FromSeconds(1);
            _factoryProcessingTimer.Tick += async (s, e) =>
            {
                bool anyMachineProcessing = false;
                foreach (var displayMachine in OwnedMachines)
                {
                    if (displayMachine.PlayerOwnedMachine.IsProcessing)
                    {
                        anyMachineProcessing = true;
                        await UpdateMachineDisplay(displayMachine);
                    }
                }
                if (!anyMachineProcessing)
                {
                    _factoryProcessingTimer.Stop();
                }
            };
            _factoryProcessingTimer.Start();
        }

        public override void OnDisappearing() // Override OnDisappearing
        {
            base.OnDisappearing(); // Call base to stop message timer
            _factoryProcessingTimer?.Stop();
        }
    }
}