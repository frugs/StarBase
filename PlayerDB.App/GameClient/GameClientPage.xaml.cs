using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
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

    public static IEnumerable<int> PlayerFilterRecentOptions {
        get
        {
            yield return (int)TimeSpan.FromDays(180).TotalSeconds;
            yield return (int)TimeSpan.FromDays(365).TotalSeconds;
            yield return (int)TimeSpan.FromDays(365 * 2).TotalSeconds;
            yield return (int)TimeSpan.FromDays(365 * 3).TotalSeconds;
            yield return (int)TimeSpan.FromDays(365 * 10).TotalSeconds;
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

    public static string RenderTimeSpan(int timeSpanSecs)
    {
        var timeSpan = TimeSpan.FromSeconds(timeSpanSecs);

        if (timeSpan.TotalDays <= 180)
        {
            return "6 months";
        }

        if (timeSpan.TotalDays <= 365)
        {
            return "1 year";
        }

        if (timeSpan.TotalDays <= 365 * 2)
        {
            return "2 years";
        }

        if (timeSpan.TotalDays <= 365 * 3)
        {
            return "3 years";
        }

        return "Don't filter";
    }

    public static Visibility MmrVisibility(int? mmr)
    {
        return mmr != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void PlayerFilterRecent_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;

        await ViewModel.OnPlayerFilterRecentSecsChanged();
    }
}