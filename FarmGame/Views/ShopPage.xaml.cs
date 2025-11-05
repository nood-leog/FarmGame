using FarmGame.Services;
using FarmGame.ViewModels;

namespace FarmGame.Views;

public partial class ShopPage : ContentPage // Ensure 'partial' keyword is present
{
    private readonly ShopViewModel _viewModel;

    public ShopPage(ShopViewModel viewModel)
    {
        InitializeComponent(); // This should now resolve
        _viewModel = viewModel;
        this.BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShopData();
    }
}