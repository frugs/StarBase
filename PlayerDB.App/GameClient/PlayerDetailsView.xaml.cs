using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlayerDB.DataModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PlayerDB.App.GameClient;

public sealed partial class PlayerDetailsView : UserControl
{
    private IPlayerDetailsViewModel? _viewModel;

    public PlayerDetailsView()
    {
        InitializeComponent();
    }

    public IPlayerDetailsViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            Bindings.Update();
        }
    }

#pragma warning disable CA1822 // Mark members as static
    private bool ContainsTerran(IReadOnlyCollection<StarCraftRace> races)
    {
        return races.Contains(StarCraftRace.Terran);
    }

    private bool ContainsProtoss(IReadOnlyCollection<StarCraftRace> races)
    {
        return races.Contains(StarCraftRace.Protoss);
    }

    private bool ContainsZerg(IReadOnlyCollection<StarCraftRace> races)
    {
        return races.Contains(StarCraftRace.Zerg);
    }
#pragma warning restore CA1822 // Mark members as static


    private void SetPlayerPlaysTerran(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.PlayerRaces.Contains(StarCraftRace.Terran) == value) return;

        _viewModel.PlayerRaces = value == true
            ? [.. _viewModel.PlayerRaces, StarCraftRace.Terran]
            : _viewModel.PlayerRaces.Where(x => x != StarCraftRace.Terran).ToList();
    }

    private void SetPlayerPlaysProtoss(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.PlayerRaces.Contains(StarCraftRace.Protoss) == value) return;

        _viewModel.PlayerRaces = value == true
            ? [.. _viewModel.PlayerRaces, StarCraftRace.Protoss]
            : _viewModel.PlayerRaces.Where(x => x != StarCraftRace.Protoss).ToList();
    }

    private void SetPlayerPlaysZerg(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.PlayerRaces.Contains(StarCraftRace.Zerg) == value) return;

        _viewModel.PlayerRaces = value == true
            ? [.. _viewModel.PlayerRaces, StarCraftRace.Zerg]
            : _viewModel.PlayerRaces.Where(x => x != StarCraftRace.Zerg).ToList();
    }

    private void SetOpponentPlaysTerran(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.OpponentRaces.Contains(StarCraftRace.Terran) == value) return;

        _viewModel.OpponentRaces = value == true
            ? [.. _viewModel.OpponentRaces, StarCraftRace.Terran]
            : _viewModel.OpponentRaces.Where(x => x != StarCraftRace.Terran).ToList();
    }

    private void SetOpponentPlaysProtoss(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.OpponentRaces.Contains(StarCraftRace.Protoss) == value) return;

        _viewModel.OpponentRaces = value == true
            ? [.. _viewModel.OpponentRaces, StarCraftRace.Protoss]
            : _viewModel.OpponentRaces.Where(x => x != StarCraftRace.Protoss).ToList();
    }

    private void SetOpponentPlaysZerg(bool? value)
    {
        if (_viewModel == null || value == null ||
            _viewModel.OpponentRaces.Contains(StarCraftRace.Zerg) == value) return;

        _viewModel.OpponentRaces = value == true
            ? [.. _viewModel.OpponentRaces, StarCraftRace.Zerg]
            : _viewModel.OpponentRaces.Where(x => x != StarCraftRace.Zerg).ToList();
    }

    private async void RaceToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        await _viewModel.UpdateBuildOrders();
    }

    private async void PlayerIsMeCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        await _viewModel.SavePlayerIsMe();
    }
}