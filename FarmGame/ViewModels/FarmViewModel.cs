using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls; // For IDispatcher
using System.Linq; // For LINQ extensions

namespace FarmGame.ViewModels
{
    // DisplayPlot MUST have its own INotifyPropertyChanged implementation
    public class DisplayPlot : INotifyPropertyChanged
    {
        public Plot Plot { get; set; }

        private ImageSource? _imageSource;
        public ImageSource? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        private string? _plotStateText;
        public string? PlotStateText
        {
            get => _plotStateText;
            set => SetProperty(ref _plotStateText, value);
        }

        private bool _isTappable;
        public bool IsTappable
        {
            get => _isTappable;
            set => SetProperty(ref _isTappable, value);
        }

        public ICommand? PlotTappedCommand { get; set; }

        // --- INotifyPropertyChanged Implementation for DisplayPlot ---
        public event PropertyChangedEventHandler? PropertyChanged; // Essential for DisplayPlot

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) // Essential for DisplayPlot
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, // Essential for DisplayPlot
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

    public class FarmViewModel : BaseViewModel // Inherit from BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        // _dispatcher is now inherited from BaseViewModel
        private PlayerState _currentPlayerState;
        private IDispatcherTimer? _waterRefillTimer;
        private IDispatcherTimer? _cropGrowthTimer;
        // _messageTimer and Message property are now inherited from BaseViewModel

        // Player Stats
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

        // Farm Grid
        public ObservableCollection<DisplayPlot> FarmPlots { get; } = new ObservableCollection<DisplayPlot>();

        // Tools
        private ToolDefinition? _selectedHoe;
        public ToolDefinition? SelectedHoe
        {
            get => _selectedHoe;
            set => SetProperty(ref _selectedHoe, value);
        }

        private ToolDefinition? _selectedWateringCan;
        public ToolDefinition? SelectedWateringCan
        {
            get => _selectedWateringCan;
            set => SetProperty(ref _selectedWateringCan, value);
        }

        // Currently active interaction
        public enum FarmInteractionMode { None, Tilling, Planting, Watering, Harvesting }
        private FarmInteractionMode _currentInteractionMode = FarmInteractionMode.None;

        private SeedDefinition? _selectedSeedToPlant;

        // Available Seeds to Plant (from Inventory)
        public ObservableCollection<DisplayInventoryItem> AvailableSeeds { get; } = new ObservableCollection<DisplayInventoryItem>();

        // Commands
        public ICommand SelectToolCommand { get; }
        public ICommand SelectSeedToPlantCommand { get; }
        public ICommand ResetInteractionModeCommand { get; }
        public ICommand BuyPlotCommand { get; }

        public FarmViewModel(DatabaseService databaseService, IDispatcher dispatcher) : base(dispatcher) // Pass dispatcher to base
        {
            _databaseService = databaseService;
            // _dispatcher is now handled by BaseViewModel

            SelectToolCommand = new Command<string>(async (toolType) => await ExecuteSelectToolCommand(toolType));
            SelectSeedToPlantCommand = new Command<DisplayInventoryItem>(async (seedItem) => await ExecuteSelectSeedToPlantCommand(seedItem));
            ResetInteractionModeCommand = new Command(() =>
            {
                CurrentInteractionMode = FarmInteractionMode.None;
                ShowMessage("Action cancelled.");
            });
            BuyPlotCommand = new Command(async () => await ExecuteBuyPlotCommand());
        }

        public async Task LoadFarmData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1) ?? new PlayerState();
            PlayerMoney = _currentPlayerState.Money;

            if (_currentPlayerState.SelectedHoeToolId.HasValue)
            {
                SelectedHoe = await _databaseService.GetItemAsync<ToolDefinition>(_currentPlayerState.SelectedHoeToolId.Value);
            }
            if (_currentPlayerState.SelectedWaterToolId.HasValue)
            {
                SelectedWateringCan = await _databaseService.GetItemAsync<ToolDefinition>(_currentPlayerState.SelectedWaterToolId.Value);
            }

            UpdateWaterCanStatus();
            StartWaterRefillTimer();

            FarmPlots.Clear();
            var plots = await _databaseService.GetItemsAsync<Plot>();
            foreach (var plot in plots.OrderBy(p => p.PlotNumber))
            {
                var displayPlot = new DisplayPlot
                {
                    Plot = plot,
                    PlotTappedCommand = new Command<DisplayPlot>(async (p) => await OnPlotTapped(p!))
                };
                await UpdatePlotDisplay(displayPlot);
                FarmPlots.Add(displayPlot);
            }

            await LoadAvailableSeeds();
            StartCropGrowthTimer();
        }

        // --- Interaction Mode Management ---
        public FarmInteractionMode CurrentInteractionMode
        {
            get => _currentInteractionMode;
            set
            {
                SetProperty(ref _currentInteractionMode, value);
                OnPropertyChanged(nameof(IsTillingMode));
                OnPropertyChanged(nameof(IsPlantingMode));
                OnPropertyChanged(nameof(IsWateringMode));
                OnPropertyChanged(nameof(IsHarvestingMode));
                if (value != FarmInteractionMode.Planting)
                {
                    _selectedSeedToPlant = null;
                }
            }
        }

        public bool IsTillingMode => CurrentInteractionMode == FarmInteractionMode.Tilling;
        public bool IsPlantingMode => CurrentInteractionMode == FarmInteractionMode.Planting;
        public bool IsWateringMode => CurrentInteractionMode == FarmInteractionMode.Watering;
        public bool IsHarvestingMode => CurrentInteractionMode == FarmInteractionMode.Harvesting;


        // --- Tool/Seed Selection Commands ---
        private async Task ExecuteSelectToolCommand(string toolType)
        {
            _selectedSeedToPlant = null;

            switch (toolType)
            {
                case "Hoe":
                    CurrentInteractionMode = FarmInteractionMode.Tilling;
                    ShowMessage($"You selected the {SelectedHoe?.Name}.", false);
                    break;
                case "WateringCan":
                    CurrentInteractionMode = FarmInteractionMode.Watering;
                    ShowMessage($"You selected the {SelectedWateringCan?.Name}.", false);
                    break;
                case "Hand":
                    CurrentInteractionMode = FarmInteractionMode.Harvesting;
                    ShowMessage("You are ready to harvest!", false);
                    break;
                default:
                    CurrentInteractionMode = FarmInteractionMode.None;
                    ShowMessage("Action cancelled.", false);
                    break;
            }
        }

        private async Task ExecuteSelectSeedToPlantCommand(DisplayInventoryItem seedItem)
        {
            if (seedItem == null || seedItem.Quantity <= 0)
            {
                _selectedSeedToPlant = null;
                CurrentInteractionMode = FarmInteractionMode.None;
                ShowMessage("You don't have any of those seeds.", true);
                return;
            }

            var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(seedItem.ProduceDefinitionId);
            if (seedDef != null)
            {
                _selectedSeedToPlant = seedDef;
                CurrentInteractionMode = FarmInteractionMode.Planting;
                ShowMessage($"Ready to plant {seedDef.Name}.", false);
            }
        }


        // --- Plot Interaction ---
        private async Task OnPlotTapped(DisplayPlot displayPlot)
        {
            if (displayPlot == null || displayPlot.Plot == null) return;

            switch (CurrentInteractionMode)
            {
                case FarmInteractionMode.Tilling:
                    await TillPlot(displayPlot.Plot);
                    break;
                case FarmInteractionMode.Planting:
                    if (_selectedSeedToPlant != null)
                    {
                        await PlantSeed(displayPlot.Plot, _selectedSeedToPlant);
                    }
                    else
                    {
                        ShowMessage("No seed selected to plant.", true);
                    }
                    break;
                case FarmInteractionMode.Watering:
                    await WaterPlot(displayPlot.Plot);
                    break;
                case FarmInteractionMode.Harvesting:
                    await HarvestPlot(displayPlot.Plot);
                    break;
                case FarmInteractionMode.None:
                    ShowMessage($"Plot {displayPlot.Plot.PlotNumber}: {displayPlot.PlotStateText}", false);
                    break;
            }
            await UpdatePlotDisplay(displayPlot);
            await _databaseService.SaveItemAsync(displayPlot.Plot);
            await LoadAvailableSeeds();
            UpdateWaterCanStatus();
            PlayerMoney = _currentPlayerState.Money;
        }

        private async Task TillPlot(Plot plot)
        {
            if (!plot.IsTilled && plot.PlantedSeedDefinitionId == null)
            {
                plot.IsTilled = true;
                plot.PlantedSeedDefinitionId = null;
                plot.PlantTime = null;
                plot.GrowthProgress = 0;
                plot.IsWatered = false;
                ShowMessage($"Plot {plot.PlotNumber} tilled.", false);
            }
            else
            {
                ShowMessage($"Plot {plot.PlotNumber} cannot be tilled right now.", true);
            }
        }

        private async Task PlantSeed(Plot plot, SeedDefinition seed)
        {
            if (plot.IsTilled && plot.PlantedSeedDefinitionId == null)
            {
                var inventorySeedItem = (await _databaseService.GetItemsAsync<InventoryItem>())
                                        .FirstOrDefault(i => i.ProduceDefinitionId == seed.Id && i.IsSeed && i.Quantity > 0);

                if (inventorySeedItem != null)
                {
                    plot.PlantedSeedDefinitionId = seed.Id;
                    plot.PlantTime = DateTime.UtcNow;
                    plot.GrowthProgress = 0;
                    plot.IsWatered = false;
                    plot.IsTilled = false;

                    inventorySeedItem.Quantity--;
                    if (inventorySeedItem.Quantity <= 0)
                    {
                        await _databaseService.DeleteItemAsync(inventorySeedItem);
                    }
                    else
                    {
                        await _databaseService.SaveItemAsync(inventorySeedItem);
                    }
                    ShowMessage($"Planted {seed.Name} on Plot {plot.PlotNumber}.", false);
                }
                else
                {
                    ShowMessage($"You do not have any {seed.Name} to plant.", true);
                }
            }
            else
            {
                ShowMessage($"Plot {plot.PlotNumber} is not ready for planting.", true);
            }
        }

        private async Task WaterPlot(Plot plot)
        {
            if (plot.PlantedSeedDefinitionId.HasValue && !plot.IsWatered && _currentPlayerState.CurrentWater >= 1)
            {
                plot.IsWatered = true;
                _currentPlayerState.CurrentWater -= 1;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                ShowMessage($"Plot {plot.PlotNumber} watered.", false);
            }
            else if (_currentPlayerState.CurrentWater < 1)
            {
                ShowMessage("Not enough water in your watering can!", true);
            }
            else
            {
                ShowMessage($"Plot {plot.PlotNumber} does not need water or nothing is planted.", true);
            }
        }

        private async Task HarvestPlot(Plot plot)
        {
            if (plot.PlantedSeedDefinitionId.HasValue && plot.GrowthProgress >= 1.0)
            {
                var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(plot.PlantedSeedDefinitionId.Value);
                if (seedDef == null)
                {
                    ShowMessage($"Could not find seed definition for plot {plot.PlotNumber}.", true);
                    return;
                }

                var existingProduceItem = (await _databaseService.GetItemsAsync<InventoryItem>())
                                            .FirstOrDefault(i => i.ProduceDefinitionId == seedDef.HarvestsProduceDefinitionId && !i.IsSeed);

                if (existingProduceItem != null)
                {
                    existingProduceItem.Quantity++;
                    await _databaseService.SaveItemAsync(existingProduceItem);
                }
                else
                {
                    await _databaseService.SaveItemAsync(new InventoryItem
                    {
                        ProduceDefinitionId = seedDef.HarvestsProduceDefinitionId,
                        Quantity = 1,
                        IsSeed = false
                    });
                }

                plot.IsTilled = false;
                plot.PlantedSeedDefinitionId = null;
                plot.PlantTime = null;
                plot.GrowthProgress = 0;
                plot.IsWatered = false;

                ShowMessage($"Harvested {seedDef.Name} from Plot {plot.PlotNumber}.", false);
            }
            else
            {
                ShowMessage($"Nothing to harvest on Plot {plot.PlotNumber}.", true);
            }
        }

        // --- Buy Plot Command ---
        private async Task ExecuteBuyPlotCommand()
        {
            double plotCost = 100 + (FarmPlots.Count * 50);
            // This is still a Shell.Current.DisplayAlert as it's a confirmation, not a simple feedback message.
            bool confirm = await Shell.Current.DisplayAlert("Buy New Plot", $"Do you want to buy a new plot for ${plotCost:F2}?", "Yes", "No");
            if (confirm)
            {
                if (_currentPlayerState.Money >= plotCost)
                {
                    _currentPlayerState.Money -= plotCost;
                    await _databaseService.SaveItemAsync(_currentPlayerState);
                    PlayerMoney = _currentPlayerState.Money;

                    var newPlot = new Plot { PlotNumber = FarmPlots.Count, IsTilled = false, GrowthProgress = 0, IsWatered = false };
                    await _databaseService.SaveItemAsync(newPlot);

                    var displayPlot = new DisplayPlot
                    {
                        Plot = newPlot,
                        PlotTappedCommand = new Command<DisplayPlot>(async (p) => await OnPlotTapped(p!))
                    };
                    await UpdatePlotDisplay(displayPlot);
                    FarmPlots.Add(displayPlot);

                    ShowMessage($"New plot purchased for ${plotCost:F2}!", false);
                }
                else
                {
                    ShowMessage($"You need ${plotCost:F2} to buy a new plot.", true);
                }
            }
        }

        private async Task UpdatePlotDisplay(DisplayPlot displayPlot)
        {
            var plot = displayPlot.Plot;
            if (plot.PlantedSeedDefinitionId.HasValue)
            {
                var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(plot.PlantedSeedDefinitionId.Value);
                if (seedDef != null)
                {
                    var produceDef = await _databaseService.GetItemAsync<ProduceDefinition>(seedDef.HarvestsProduceDefinitionId);
                    if (produceDef != null)
                    {
                        if (plot.GrowthProgress >= 1.0)
                        {
                            displayPlot.ImageSource = "plant_grown.png";
                            displayPlot.PlotStateText = $"{produceDef.Name} (Ready!)";
                            displayPlot.IsTappable = true;
                        }
                        else if (plot.IsWatered)
                        {
                            displayPlot.ImageSource = "plant_watered.png";
                            displayPlot.PlotStateText = $"{produceDef.Name} ({plot.GrowthProgress:P0})";
                            displayPlot.IsTappable = true;
                        }
                        else
                        {
                            displayPlot.ImageSource = "plant_unwatered.png";
                            displayPlot.PlotStateText = $"{produceDef.Name} (Needs Water)";
                            displayPlot.IsTappable = true;
                        }
                    }
                }
            }
            else if (plot.IsTilled)
            {
                displayPlot.ImageSource = "plot_tilled.png";
                displayPlot.PlotStateText = "Tilled";
                displayPlot.IsTappable = true;
            }
            else
            {
                displayPlot.ImageSource = "plot_empty.png";
                displayPlot.PlotStateText = "Empty";
                displayPlot.IsTappable = true;
            }
        }

        private void UpdateWaterCanStatus()
        {
            if (_currentPlayerState != null && SelectedWateringCan != null)
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
                    }
                };
                _waterRefillTimer.Start();
            }
        }

        private void StartCropGrowthTimer()
        {
            _cropGrowthTimer?.Stop();

            _cropGrowthTimer = _dispatcher.CreateTimer();
            _cropGrowthTimer.Interval = TimeSpan.FromSeconds(1);
            _cropGrowthTimer.Tick += async (s, e) =>
            {
                foreach (var displayPlot in FarmPlots)
                {
                    var plot = displayPlot.Plot;
                    if (plot.PlantedSeedDefinitionId.HasValue && plot.IsWatered && plot.GrowthProgress < 1.0)
                    {
                        var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(plot.PlantedSeedDefinitionId.Value);
                        if (seedDef != null && seedDef.GrowTimeSeconds > 0 && plot.PlantTime.HasValue)
                        {
                            TimeSpan timeSincePlant = DateTime.UtcNow - plot.PlantTime.Value;
                            double newProgress = timeSincePlant.TotalSeconds / seedDef.GrowTimeSeconds;

                            if (newProgress > 1.0) newProgress = 1.0;

                            if (plot.GrowthProgress != newProgress)
                            {
                                plot.GrowthProgress = newProgress;
                                await UpdatePlotDisplay(displayPlot);
                                await _databaseService.SaveItemAsync(plot);
                            }
                        }
                    }
                }
            };
            _cropGrowthTimer.Start();
        }

        private async Task LoadAvailableSeeds()
        {
            AvailableSeeds.Clear();
            var inventoryItems = await _databaseService.GetItemsAsync<InventoryItem>();
            foreach (var item in inventoryItems.Where(i => i.IsSeed && i.Quantity > 0))
            {
                var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(item.ProduceDefinitionId);
                if (seedDef != null)
                {
                    AvailableSeeds.Add(new DisplayInventoryItem
                    {
                        Id = item.Id,
                        ProduceDefinitionId = item.ProduceDefinitionId,
                        Name = seedDef.Name,
                        Quantity = item.Quantity,
                        IsSeed = true,
                        BaseSellPrice = seedDef.ShopCost
                    });
                }
            }
        }

        public override void OnDisappearing() // Override OnDisappearing
        {
            base.OnDisappearing(); // Call base to stop message timer
            _waterRefillTimer?.Stop();
            _cropGrowthTimer?.Stop();
        }
    }
}