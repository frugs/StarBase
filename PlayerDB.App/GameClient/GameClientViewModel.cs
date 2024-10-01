using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using PlayerDB.Core.Game;
using PlayerDB.Core.Settings;
using PlayerDB.DataModel;
using PlayerDB.DataStorage;
using PlayerDB.Game;
using PlayerDB.Utilities;

namespace PlayerDB.App.GameClient;

public sealed partial class GameClientPageViewModel(
    IPlayerDetailsViewModel playerDetailsViewModel,
    DispatcherQueue dispatcher,
    IPlayerRepository playerRepository,
    ISettingsService settingsService,
    IGameClientPollingService gameClientPollingService) : ObservableRecipient, IGameClientPageViewModel, IDisposable
{
    [ObservableProperty] private bool _isConnectedToGameClient;
    [ObservableProperty] private bool _isWaitingForPlayer;
    private List<string> _playerToons = [];
    [ObservableProperty] private PlayerMatchItem? _selectedMatch;
    private CompositeDisposable? _sub;
    private GameData? _cachedGameData;
    private readonly object _lock = new();

    public void Dispose()
    {
        IsActive = false;
    }

    public ObservableCollection<PlayerMatchItem> Matches { get; } = [];

    public IPlayerDetailsViewModel PlayerDetailsViewModel => playerDetailsViewModel;

    public Task<Player?> LoadPlayerDetails(int playerId)
    {
        return playerRepository.FindPlayerByIdPartial(playerId);
    }

    protected override async void OnActivated()
    {
        SelectedMatch = Matches.FirstOrDefault();
        UpdateGameClientConnectionState();

        _sub =
        [
            settingsService.SettingsChangedObservable.Subscribe(
                Observer.Create<(string SettingsKey, DataModel.Settings Change)>(args =>
                {
                    if (args.SettingsKey != nameof(DataModel.Settings.PlayerToons)) return;

                    UpdatePlayerToons(args.Change);
                })),
            gameClientPollingService.GameClientObservable.Subscribe(Observer.Create<GameData>(gameData => Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_cachedGameData == gameData) return;

                    _cachedGameData = gameData;
                }

                OnGameData(gameData);
            }))),
            playerRepository.PlayerAddedOrUpdatedObservable.Subscribe(Observer.Create<Player>(_ => Task.Run(() =>
            {
                lock (_lock)
                {
                    _cachedGameData = null;
                }
            })))
        ];

        var currentSettings = await settingsService.GetCurrentSettings();
        UpdatePlayerToons(currentSettings);
    }

    private void UpdateGameClientConnectionState()
    {
        IsConnectedToGameClient = gameClientPollingService.IsStarted;
        IsWaitingForPlayer = gameClientPollingService.IsStarted && Matches.Count == 0;
    }

    protected override void OnDeactivated()
    {
        _sub?.Dispose();
        _sub = null;
    }

    private void UpdatePlayerToons(DataModel.Settings settings)
    {
        var value = (settings.PlayerToons ?? DefaultSettings.PlayerToons).ToList();
        var prev = Interlocked.Exchange(ref _playerToons, value);

        if (prev.SequenceEqual(value)) return;

        dispatcher.TryEnqueue(SortMatches);
    }

    // TODO: This should be implemented via the ICollectionView interface
    private void SortMatches()
    {
        var removed = new List<PlayerMatchItem>();
        var selected = SelectedMatch;

        for (var i = Matches.Count - 1; i >= 0; i--)
        {
            if (!_playerToons.Contains(Matches[i].Toon)) continue;

            removed.Add(Matches[i]);
            Matches.RemoveAt(i);
        }

        if (selected != null && removed.Contains(selected))
        {
            selected = null;
        }

        removed.Reverse();

        var orderedByMmr = Matches.OrderByDescending(x => x.Mmr ?? 0).ToList();
        
        Matches.Clear();
        Matches.AddAll(orderedByMmr);
        Matches.AddAll(removed);

        SelectedMatch = selected != null && Matches.Contains(selected) ? selected : Matches.FirstOrDefault();
    }

    private async void OnGameData(GameData gameData)
    {
        var playerMatches = (await Task.WhenAll(
                gameData.Players.Select(x => Task.Run(() => MatchPlayersByName(x, gameData))))
            ).FoldToList();

        dispatcher.TryEnqueue(() =>
        {
            if (Matches.SequenceEqual(playerMatches)) return;

            var selected = SelectedMatch;
            Matches.OverwriteWith(playerMatches);

            if (selected != null && Matches.Contains(selected))
            {
                SelectedMatch = selected;
            }

            SortMatches();
            UpdateGameClientConnectionState();
        });
    }

    private async Task<IEnumerable<PlayerMatchItem>> MatchPlayersByName(GamePlayerData playerData, GameData gameData)
    {
        var playerMatches = await playerRepository.MatchPlayersByName(playerData.PlayerName);
        return playerMatches
            .Select(player => new PlayerMatchItem(
                    player.Id,
                    player.ClanName ?? "",
                    player.Name ?? "",
                    player.Toon ?? "",
                    Mmr: playerData.Race switch
                    {
                        StarCraftRace.Terran => player.MostRecentMmrT,
                        StarCraftRace.Protoss => player.MostRecentMmrP,
                        StarCraftRace.Zerg => player.MostRecentMmrZ,
                        _ => null,
                    },
                    playerData.Race,
                    playerData.GetOpponent(gameData.Players)?.Race ?? StarCraftRace.Unknown
                )
            );
    }

    partial void OnIsConnectedToGameClientChanged(bool value)
    {
        gameClientPollingService.IsStarted = value;
        UpdateGameClientConnectionState();
    }
}