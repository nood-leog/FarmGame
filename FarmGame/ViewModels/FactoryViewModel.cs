using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Linq;

namespace FarmGame.ViewModels
{
    // DisplayMachine remains the same
    public class DisplayMachine : INotifyPropertyChanged
    {
        public PlayerOwnedMachine PlayerOwnedMachine { get; set; }
        public MachineDefinition MachineDefinition { get; set; }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private double _processingProgress;
        public double ProcessingProgress
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        private bool _canStartProcessing;
        public bool CanStartProcessing
        {
            get => _canStartProcessing;
            set => SetProperty(ref _canStartProcessing, value);
        }

        private bool _canCollect;
        public bool CanCollect
        {
            get => _canCollect;
            set => SetProperty(ref _canCollect, value);
        }

        public List<ProduceDefinition> AvailableInputProduce { get; set; } = new List<ProduceDefinition>();
        private ProduceDefinition? _selectedInputProduce;
        public ProduceDefinition? SelectedInputProduce
        {
            get => _selectedInputProduce;
            set => SetProperty(ref _selectedInputProduce, value);
        }

        public ICommand StartProcessingCommand { get; set; }
        public ICommand CollectCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

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
            set => SetProperty(ref _playerMoney, value);
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
                    var displayMachine = new DisplayMachine
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
                ShowMessage($"{machineDef.Name} is already processing.", true); // Replaced DisplayAlert
                return;
            }

            var inputInventory = (await _databaseService.GetItemsAsync<InventoryItem>())
                                 .FirstOrDefault(i => i.ProduceDefinitionId == machineDef.InputProduceDefinitionId && !i.IsSeed);

            if (inputInventory == null || inputInventory.Quantity < machineDef.InputQuantity)
            {
                string inputProduceName = (await _databaseService.GetItemAsync<ProduceDefinition>(machineDef.InputProduceDefinitionId))?.Name ?? "Unknown";
                ShowMessage($"You need {machineDef.InputQuantity}x {inputProduceName} to start processing.", true); // Replaced DisplayAlert
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
            ShowMessage($"{machineDef.Name} started processing.", false); // Replaced DisplayAlert
        }

        private async Task CollectOutput(DisplayMachine displayMachine)
        {
            var playerMachine = displayMachine.PlayerOwnedMachine;
            var machineDef = displayMachine.MachineDefinition;

            if (!playerMachine.IsProcessing || !playerMachine.ProcessStartTime.HasValue ||
                (DateTime.UtcNow - playerMachine.ProcessStartTime.Value).TotalSeconds < machineDef.ProcessingTimeSeconds)
            {
                ShowMessage($"{machineDef.Name} is not finished processing yet.", true); // Replaced DisplayAlert
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
            ShowMessage($"Collected {machineDef.OutputQuantity}x {outputProduceName} from {machineDef.Name}.", false); // Replaced DisplayAlert
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