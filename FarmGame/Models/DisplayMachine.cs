using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input; // For ICommand
using System.Collections.Generic; // Added for List<ProduceDefinition>

namespace FarmGame.Models // <--- Ensure this is correct
{
    public class DisplayMachine : INotifyPropertyChanged
    {
        public PlayerOwnedMachine PlayerOwnedMachine { get; set; } // The runtime instance of the player's machine
        public MachineDefinition MachineDefinition { get; set; } // The static definition of the machine

        // Properties derived from PlayerOwnedMachine or for UI display
        private string _statusText;
        public string StatusText // E.g., "Idle", "Processing Wheat (50%)", "Ready to Collect Flour"
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private double _processingProgress;
        public double ProcessingProgress // 0.0 to 1.0 for progress bar
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        private bool _canStartProcessing;
        public bool CanStartProcessing // True if machine is idle and player has enough input
        {
            get => _canStartProcessing;
            set => SetProperty(ref _canStartProcessing, value);
        }

        private bool _canCollect;
        public bool CanCollect // True if processing is complete
        {
            get => _canCollect;
            set => SetProperty(ref _canCollect, value);
        }

        // For display in picker/dropdown: Which input produce to process
        public List<ProduceDefinition> AvailableInputProduce { get; set; } = new List<ProduceDefinition>();
        private ProduceDefinition? _selectedInputProduce;
        public ProduceDefinition? SelectedInputProduce
        {
            get => _selectedInputProduce;
            set => SetProperty(ref _selectedInputProduce, value);
        }


        // Commands for UI interaction
        public ICommand StartProcessingCommand { get; set; }
        public ICommand CollectCommand { get; set; }

        // INotifyPropertyChanged boilerplate
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
}