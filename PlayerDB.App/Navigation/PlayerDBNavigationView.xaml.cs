using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlayerDB.App.GameClient;
using PlayerDB.App.Players;
using PlayerDB.App.Replays;
using PlayerDB.App.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PlayerDB.App.Navigation;

public sealed partial class PlayerDBNavigationView : NavigationView
{
    private IPlayerDBNavigationViewModel? _viewModel;

    public PlayerDBNavigationView()
    {
        InitializeComponent();
    }

    public IPlayerDBNavigationViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;

            switch (ContentFrame.Content)
            {
                case GameClientPage gameClientPage:
                    gameClientPage.ViewModel = _viewModel?.GameClientPageViewModel;
                    break;
                case ReplaysPage replaysPage:
                    replaysPage.ViewModel = _viewModel?.ReplaysPageViewModel;
                    break;
            }
        }
    }

    private void PlayerDBNavigationView_OnLoaded(object sender, RoutedEventArgs e)
    {
        SelectedItem = MenuItems.FirstOrDefault();
    }

    private void PlayerDBNavigationView_OnSelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        var (destinationPageType, destinationNavigationParameter) = args.SelectedItemContainer switch
        {
            var container when container == GameClient => (typeof(GameClientPage), (object?)ViewModel?.GameClientPageViewModel),
            var container when container == Players => (typeof(PlayersPage), null),
            var container when container == Replays => (typeof(ReplaysPage), ViewModel?.ReplaysPageViewModel),
            _ when args.IsSettingsSelected => (typeof(SettingsPage), ViewModel?.SettingsPageViewModel),
            _ => (null, null)
        };

        if (destinationPageType != null) ContentFrame.Navigate(destinationPageType, destinationNavigationParameter);
    }
}