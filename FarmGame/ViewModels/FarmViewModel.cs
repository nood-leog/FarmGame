using FarmGame.Models;
using FarmGame.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls; // Add this for Shell.Current.DisplayAlert and IDispatcher
using System.Linq; // Add this for LINQ extensions

namespace FarmGame.ViewModels
{
    public class DisplayPlot : INotifyPropertyChanged
    {
        public Plot Plot { get; set; }

        private ImageSource? _imageSource; // Add '?'
        public ImageSource? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        private string? _plotStateText; // Add '?'
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

        public ICommand? PlotTappedCommand { get; set; } // Add '?'

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

    public class FarmViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly IDispatcher _dispatcher; // ADD THIS FIELD
        private PlayerState _currentPlayerState;
        private IDispatcherTimer? _waterRefillTimer; // Add '?'
        private IDispatcherTimer? _cropGrowthTimer; // Add '?'

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

        public ObservableCollection<DisplayPlot> FarmPlots { get; } = new ObservableCollection<DisplayPlot>();

        private ToolDefinition? _selectedHoe; // Add '?'
        public ToolDefinition? SelectedHoe
        {
            get => _selectedHoe;
            set => SetProperty(ref _selectedHoe, value);
        }

        private ToolDefinition? _selectedWateringCan; // Add '?'
        public ToolDefinition? SelectedWateringCan
        {
            get => _selectedWateringCan;
            set => SetProperty(ref _selectedWateringCan, value);
        }

        public enum FarmInteractionMode { None, Tilling, Planting, Watering, Harvesting }
        private FarmInteractionMode _currentInteractionMode = FarmInteractionMode.None;

        private SeedDefinition? _selectedSeedToPlant; // Add '?'

        public ObservableCollection<DisplayInventoryItem> AvailableSeeds { get; } = new ObservableCollection<DisplayInventoryItem>();

        public ICommand SelectToolCommand { get; }
        public ICommand SelectSeedToPlantCommand { get; }
        public ICommand ResetInteractionModeCommand { get; }
        public ICommand BuyPlotCommand { get; } // Add this command for the toolbar item

        public FarmViewModel(DatabaseService databaseService, IDispatcher dispatcher) // INJECT IDispatcher
        {
            _databaseService = databaseService;
            _dispatcher = dispatcher; // ASSIGN IT

            SelectToolCommand = new Command<string>(async (toolType) => await ExecuteSelectToolCommand(toolType));
            SelectSeedToPlantCommand = new Command<DisplayInventoryItem>(async (seedItem) => await ExecuteSelectSeedToPlantCommand(seedItem)); // MAKE ASYNC
            ResetInteractionModeCommand = new Command(() => CurrentInteractionMode = FarmInteractionMode.None);
            BuyPlotCommand = new Command(async () => await ExecuteBuyPlotCommand()); // Initialize BuyPlotCommand
        }

        public async Task LoadFarmData()
        {
            _currentPlayerState = await _databaseService.GetItemAsync<PlayerState>(1);
            if (_currentPlayerState != null)
            {
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
            }

            FarmPlots.Clear();
            var plots = await _databaseService.GetItemsAsync<Plot>();
            foreach (var plot in plots.OrderBy(p => p.PlotNumber))
            {
                var displayPlot = new DisplayPlot
                {
                    Plot = plot,
                    PlotTappedCommand = new Command<DisplayPlot>(async (p) => await OnPlotTapped(p!)) // Using null-forgiving operator
                };
                await UpdatePlotDisplay(displayPlot); // Make sure this is awaited
                FarmPlots.Add(displayPlot);
            }

            await LoadAvailableSeeds(); // Ensure this is awaited
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
                    await Shell.Current.DisplayAlert("Tool Selected", $"You selected the {SelectedHoe?.Name}.", "OK"); // Use Shell.Current
                    break;
                case "WateringCan":
                    CurrentInteractionMode = FarmInteractionMode.Watering;
                    await Shell.Current.DisplayAlert("Tool Selected", $"You selected the {SelectedWateringCan?.Name}.", "OK"); // Use Shell.Current
                    break;
                case "Hand":
                    CurrentInteractionMode = FarmInteractionMode.Harvesting;
                    await Shell.Current.DisplayAlert("Tool Selected", "You are ready to harvest!", "OK"); // Use Shell.Current
                    break;
                default:
                    CurrentInteractionMode = FarmInteractionMode.None;
                    break;
            }
        }

        private async Task ExecuteSelectSeedToPlantCommand(DisplayInventoryItem seedItem) // MAKE ASYNC TASK
        {
            if (seedItem == null || seedItem.Quantity <= 0)
            {
                _selectedSeedToPlant = null;
                CurrentInteractionMode = FarmInteractionMode.None;
                await Shell.Current.DisplayAlert("No Seeds", "You don't have any of those seeds.", "OK"); // Use Shell.Current
                return;
            }

            var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(seedItem.ProduceDefinitionId); // Now points to SeedDefinition.Id
            if (seedDef != null)
            {
                _selectedSeedToPlant = seedDef;
                CurrentInteractionMode = FarmInteractionMode.Planting;
                await Shell.Current.DisplayAlert("Seed Selected", $"Ready to plant {seedDef.Name}.", "OK"); // Use Shell.Current
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
                        await Shell.Current.DisplayAlert("Error", "No seed selected to plant.", "OK"); // Use Shell.Current
                    }
                    break;
                case FarmInteractionMode.Watering:
                    await WaterPlot(displayPlot.Plot);
                    break;
                case FarmInteractionMode.Harvesting:
                    await HarvestPlot(displayPlot.Plot);
                    break;
                case FarmInteractionMode.None:
                    await Shell.Current.DisplayAlert("Plot Info", $"Plot {displayPlot.Plot.PlotNumber}: {displayPlot.PlotStateText}", "OK"); // Use Shell.Current
                    break;
            }
            await UpdatePlotDisplay(displayPlot); // Make sure this is awaited
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
                await Shell.Current.DisplayAlert("Action", $"Plot {plot.PlotNumber} tilled.", "OK"); // Use Shell.Current
            }
            else
            {
                await Shell.Current.DisplayAlert("Action", $"Plot {plot.PlotNumber} cannot be tilled right now.", "OK"); // Use Shell.Current
            }
        }

        private async Task PlantSeed(Plot plot, SeedDefinition seed)
        {
            if (plot.IsTilled && plot.PlantedSeedDefinitionId == null)
            {
                var inventorySeedItem = (await _databaseService.GetItemsAsync<InventoryItem>())
                                        .FirstOrDefault(i => i.ProduceDefinitionId == seed.Id && i.IsSeed && i.Quantity > 0); // Use seed.Id

                if (inventorySeedItem != null)
                {
                    plot.PlantedSeedDefinitionId = seed.Id; // Store SeedDefinition.Id in Plot
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
                    await Shell.Current.DisplayAlert("Action", $"Planted {seed.Name} on Plot {plot.PlotNumber}.", "OK"); // Use Shell.Current
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", $"You do not have any {seed.Name} to plant.", "OK"); // Use Shell.Current
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Plot {plot.PlotNumber} is not ready for planting.", "OK"); // Use Shell.Current
            }
        }

        private async Task WaterPlot(Plot plot)
        {
            if (plot.PlantedSeedDefinitionId.HasValue && !plot.IsWatered && _currentPlayerState.CurrentWater >= 1)
            {
                plot.IsWatered = true;
                _currentPlayerState.CurrentWater -= 1;
                await _databaseService.SaveItemAsync(_currentPlayerState);
                await Shell.Current.DisplayAlert("Action", $"Plot {plot.PlotNumber} watered.", "OK"); // Use Shell.Current
            }
            else if (_currentPlayerState.CurrentWater < 1)
            {
                await Shell.Current.DisplayAlert("Error", "Not enough water in your watering can!", "OK"); // Use Shell.Current
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Plot {plot.PlotNumber} does not need water or nothing is planted.", "OK"); // Use Shell.Current
            }
        }

        private async Task HarvestPlot(Plot plot)
        {
            if (plot.PlantedSeedDefinitionId.HasValue && plot.GrowthProgress >= 1.0)
            {
                var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(plot.PlantedSeedDefinitionId.Value);
                if (seedDef == null)
                {
                    await Shell.Current.DisplayAlert("Error", $"Could not find seed definition for plot {plot.PlotNumber}.", "OK"); // Use Shell.Current
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

                await Shell.Current.DisplayAlert("Action", $"Harvested {seedDef.Name} from Plot {plot.PlotNumber}.", "OK"); // Use Shell.Current
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Nothing to harvest on Plot {plot.PlotNumber}.", "OK"); // Use Shell.Current
            }
        }

        // --- Buy Plot Command ---
        private async Task ExecuteBuyPlotCommand()
        {
            double plotCost = 100 + (FarmPlots.Count * 50); // Example increasing cost
            if (_currentPlayerState.Money >= plotCost)
            {
                bool confirm = await Shell.Current.DisplayAlert("Buy New Plot", $"Do you want to buy a new plot for ${plotCost:F2}?", "Yes", "No");
                if (confirm)
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

                    await Shell.Current.DisplayAlert("Purchased", $"New plot purchased for ${plotCost:F2}!", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Cannot Afford", $"You need ${plotCost:F2} to buy a new plot.", "OK");
            }
        }


        // --- Plot Display Update Logic ---
        private async Task UpdatePlotDisplay(DisplayPlot displayPlot) // Make async Task
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

        // --- Water Refill Timer ---
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
                _waterRefillTimer = _dispatcher.CreateTimer(); // Use _dispatcher instance
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

        // --- Crop Growth Timer ---
        private void StartCropGrowthTimer()
        {
            _cropGrowthTimer?.Stop();

            _cropGrowthTimer = _dispatcher.CreateTimer(); // Use _dispatcher instance
            _cropGrowthTimer.Interval = TimeSpan.FromSeconds(1);
            _cropGrowthTimer.Tick += async (s, e) =>
            {
                // Removed 'plotsUpdated' variable as it was unused.
                foreach (var displayPlot in FarmPlots)
                {
                    var plot = displayPlot.Plot;
                    if (plot.PlantedSeedDefinitionId.HasValue && plot.IsWatered && plot.GrowthProgress < 1.0)
                    {
                        var seedDef = await _databaseService.GetItemAsync<SeedDefinition>(plot.PlantedSeedDefinitionId.Value);
                        if (seedDef != null && seedDef.GrowTimeSeconds > 0 && plot.PlantTime.HasValue) // Check PlantTime.HasValue
                        {
                            TimeSpan timeSincePlant = DateTime.UtcNow - plot.PlantTime.Value;
                            double newProgress = timeSincePlant.TotalSeconds / seedDef.GrowTimeSeconds;

                            if (newProgress > 1.0) newProgress = 1.0;

                            if (plot.GrowthProgress != newProgress)
                            {
                                plot.GrowthProgress = newProgress;
                                await UpdatePlotDisplay(displayPlot); // Make sure this is awaited
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
                        ProduceDefinitionId = item.ProduceDefinitionId, // Include ProduceDefinitionId
                        Name = seedDef.Name,
                        Quantity = item.Quantity,
                        IsSeed = true,
                        BaseSellPrice = seedDef.ShopCost
                    });
                }
            }
        }

        public void OnDisappearing()
        {
            _waterRefillTimer?.Stop();
            _cropGrowthTimer?.Stop();
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