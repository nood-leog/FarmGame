using FarmGame.ViewModels; // Ensure this is present

namespace FarmGame.Views;

public partial class FactoryPage : ContentPage
{
    private readonly FactoryViewModel _viewModel;

    public FactoryPage(FactoryViewModel viewModel) // Inject FactoryViewModel
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.BindingContext = _viewModel; // Set the ViewModel as the BindingContext
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFactoryData(); // Load data when the page appears
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing(); // Stop timers when page disappears
    }
}