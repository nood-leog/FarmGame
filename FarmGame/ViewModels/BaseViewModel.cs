using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls; // For IDispatcher

namespace FarmGame.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected readonly IDispatcher _dispatcher;
        private IDispatcherTimer? _messageTimer;

        // Constructor will take IDispatcher, to be passed from derived ViewModels
        public BaseViewModel(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        private string? _message;
        public string? Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        protected void ShowMessage(string message, bool isError = false)
        {
            Message = message;
            _messageTimer?.Stop();
            _messageTimer = _dispatcher.CreateTimer();
            _messageTimer.Interval = TimeSpan.FromSeconds(isError ? 4 : 2); // Longer for errors
            _messageTimer.Tick += (s, e) =>
            {
                Message = null; // Clear message
                _messageTimer?.Stop();
            };
            _messageTimer.Start();
        }

        // Virtual method to allow derived ViewModels to clean up their own timers
        public virtual void OnDisappearing()
        {
            _messageTimer?.Stop();
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