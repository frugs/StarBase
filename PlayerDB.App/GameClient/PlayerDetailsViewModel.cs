using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using PlayerDB.Core.BuildOrder;
using PlayerDB.Core.Player;
using PlayerDB.Core.Settings;
using PlayerDB.DataModel;
using PlayerDB.DataStorage;

namespace PlayerDB.App.GameClient;

public sealed partial class PlayerDetailsViewModel : ObservableObject, IPlayerDetailsViewModel, IDisposable
{
    private readonly IBuildOrderManager _buildOrderManager;
    private readonly IPlayerManager _playerManager;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISettingsService _settingsService;
    private readonly CompositeDisposable _sub;

    private IReadOnlyCollection<BuildOrder> _allBuildOrders = [];

    [ObservableProperty] private IReadOnlyList<BuildOrder> _buildOrders = [];
    [ObservableProperty] private string _clanName = "";
    [ObservableProperty] private bool _isAvailable;
    [ObservableProperty] private IReadOnlyCollection<StarCraftRace> _opponentRaces = [];
    [ObservableProperty] private bool _playerIsMe;
    [ObservableProperty] private string _playerName = "";
    [ObservableProperty] private string _playerNotes = "Some player notes.";
    [ObservableProperty] private IReadOnlyCollection<StarCraftRace> _playerRaces = [];
    [ObservableProperty] private string _toon = "";

    private CancellationTokenSource? _updateBuildOrdersCancellation;

    public PlayerDetailsViewModel(
        DispatcherQueue dispatcher,
        IBuildOrderManager buildOrderManager,
        IPlayerManager playerManager,
        IPlayerRepository playerRepository,
        ISettingsService settingsService)
    {
        _buildOrderManager = buildOrderManager;
        _playerManager = playerManager;
        _playerRepository = playerRepository;
        _settingsService = settingsService;

        _sub =
        [
            settingsService.SettingsChangedObservable.Subscribe(args => dispatcher.TryEnqueue(() =>
            {
                if (args.SettingsKey != nameof(DataModel.Settings.PlayerToons)) return;

                PlayerIsMe = (args.Change.PlayerToons ?? []).Contains(Toon);
            })),
            playerRepository.PlayerAddedOrUpdatedObservable.Subscribe(player => dispatcher.EnqueueAsync<Task>(() => 
                player.Toon != Toon ? Task.CompletedTask : SetPlayer(player)))
        ];
    }

    public void Dispose()
    {
        _sub.Dispose();

        _updateBuildOrdersCancellation?.Cancel(false);
        _updateBuildOrdersCancellation?.Dispose();
    }

    public async Task SetPlayer(Player player, StarCraftRace? playerRace = null, StarCraftRace? opponentRace = null)
    {
        const int maxBuildOrdersPerMatchUp = 6;

        IsAvailable = true;

        ClanName = player.ClanName ?? "";
        PlayerName = player.Name ?? "";
        Toon = player.Toon ?? "";

        PlayerRaces = playerRace.IsKnownAndNotRandom()
            ? [playerRace!.Value]
            : [StarCraftRace.Terran, StarCraftRace.Protoss, StarCraftRace.Zerg];

        OpponentRaces = opponentRace != StarCraftRace.Unknown
            ? [opponentRace!.Value]
            : [StarCraftRace.Terran, StarCraftRace.Protoss, StarCraftRace.Zerg];

        BuildOrders = [];

        var settings = await _settingsService.GetCurrentSettings();
        PlayerIsMe = (settings.PlayerToons ?? DefaultSettings.PlayerToons).Contains(Toon);

        var fullPlayer = await _playerRepository.FindPlayerByIdFull(player.Id);
        if (fullPlayer == null) return;

        _allBuildOrders = _buildOrderManager.RecentValidBuildOrdersForPlayer(fullPlayer, maxBuildOrdersPerMatchUp);

        await UpdateBuildOrders();
    }

    public async Task UpdateBuildOrders()
    {
        var cancellation = new CancellationTokenSource();
        _updateBuildOrdersCancellation?.Cancel(true);
        _updateBuildOrdersCancellation = cancellation;

        try
        {
            await UpdateBuildOrdersImpl(cancellation.Token);
        }
        finally
        {
            cancellation.Dispose();
            if (_updateBuildOrdersCancellation == cancellation) _updateBuildOrdersCancellation = null;
        }
    }

    public Task SavePlayerIsMe()
    {
        return _playerManager.SetPlayerIsMe(Toon, PlayerIsMe);
    }

    public void ClearPlayer()
    {
        IsAvailable = false;
    }

    private async Task UpdateBuildOrdersImpl(CancellationToken cancellation = default)
    {
        var matchUpBuildOrders = _allBuildOrders
            .Where(x => x.IsValid && PlayerRaces.Contains(x.PlayerRace) && OpponentRaces.Contains(x.OpponentRace))
            .OrderByDescending(x => x.ReplayStartTimeUtc)
            .ToList();

        BuildOrders = matchUpBuildOrders.Take(3).ToList();

        if (matchUpBuildOrders.All(x => x.BuildOrderActions is not null and not [])) return;

        foreach (var buildOrder in matchUpBuildOrders)
            try
            {
                await _buildOrderManager.EnsureBuildOrderActions(buildOrder, cancellation);
                if (cancellation.IsCancellationRequested) return;
            }
            catch (TaskCanceledException)
            {
                // expected, so swallow
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // Continue with others even if we failed to load actions for this one
            }

        BuildOrders = matchUpBuildOrders.Where(x => x is { IsValid: true, BuildOrderActions: not null and not [] })
            .Take(3).ToList();
    }
}