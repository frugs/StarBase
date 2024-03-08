using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace PlayerDB.App.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public ISettingsPageViewModel? ViewModel { get; set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ISettingsPageViewModel viewModel) ViewModel = viewModel;
    }

#pragma warning disable IDE0051
    private async void ClearPlayersAndBuildOrdersConfirmationButton_OnClick(object sender, RoutedEventArgs e)

    {
        if (ViewModel == null) return;

        ClearPlayersAndBuildOrdersConfirmationFlyoutProgressBar.Visibility = Visibility.Visible;
        await Task.WhenAll(ViewModel.ClearPlayersAndBuildOrders(), Task.Delay(750));
        ClearPlayersAndBuildOrdersConfirmationFlyoutProgressBar.Visibility = Visibility.Collapsed;
        ClearPlayersAndBuildOrdersConfirmationFlyout.Hide();
    }

    private async void ResetSettingsConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        ResetSettingsConfirmationFlyoutProgressBar.Visibility = Visibility.Visible;
        await Task.WhenAll(ViewModel.ResetSettings(), Task.Delay(750));
        ResetSettingsConfirmationFlyoutProgressBar.Visibility = Visibility.Collapsed;
        ResetSettingsConfirmationFlyout.Hide();
    }
#pragma warning restore IDE0051
}