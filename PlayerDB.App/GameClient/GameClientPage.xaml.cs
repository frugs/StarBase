using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace PlayerDB.App.GameClient;

public sealed partial class GameClientPage : Page
{
    private IGameClientPageViewModel? _viewModel;

    public GameClientPage()
    {
        InitializeComponent();
    }

    public IGameClientPageViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != null) _viewModel.IsActive = false;
            if (value != null) value.IsActive = true;


            _viewModel = value;

            Bindings.Update();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IGameClientPageViewModel viewModel) ViewModel = viewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (ViewModel != null) ViewModel.IsActive = false;
    }

    private async void MatchesListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null &&
            e.AddedItems.FirstOrDefault() is PlayerMatchItem item)
        {
            var player = await ViewModel.LoadPlayerDetails(item.PlayerId);

            if (player != null)
                await ViewModel.PlayerDetailsViewModel.SetPlayer(player, item.PlayerRace, item.OpponentRace);
            else
                ViewModel.PlayerDetailsViewModel.ClearPlayer();
        }
    }

    public static string RenderClanName(string? clanName)
    {
        return string.IsNullOrEmpty(clanName) ? "" : $"[{clanName}]";
    }
}