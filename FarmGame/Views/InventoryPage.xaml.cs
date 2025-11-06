using FarmGame.ViewModels;

namespace FarmGame.Views;

// REMOVE : INotifyPropertyChanged
public partial class InventoryPage : ContentPage // NO INotifyPropertyChanged here anymore
{
    private readonly InventoryViewModel _viewModel;

    public InventoryPage(InventoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInventoryData();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}