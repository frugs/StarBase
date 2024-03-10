using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using PlayerDB.Core.Replay;
using PlayerDB.Core.Settings;
using PlayerDB.DataModel;

namespace PlayerDB.App.Replays;

public sealed partial class ReplaysPageViewModel(
    DispatcherQueue dispatcher,
    IReplayManager replayManager,
    ISettingsService settingsService)
    : ObservableRecipient, IReplaysPageViewModel, IDisposable
{
    private readonly CompositeDisposable _sub = [];

    [ObservableProperty] private string? _currentReplay;
    [ObservableProperty] private bool _isLoadingReplays;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private int? _loadedReplays;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _scanFoldersOnAppStart;
    [ObservableProperty] private int? _totalReplays;
    [ObservableProperty] private bool _watchReplayFolders;

    public void Dispose()
    {
        IsActive = false;
    }

    public ObservableCollection<IReplayFolderItemViewModel> ReplayFolders { get; } = [];

    public Task AddReplayFolder(string replayFolder)
    {
        var item = new ReplaysFolderItemViewModel
        {
            ReplayFolderFilePath = replayFolder
        };

        ReplayFolders.Add(item);

        return SearchForAndLoadReplaysImpl([item]);
    }

    public async Task LoadReplay(string replayFilePath)
    {
        IsLoadingReplays = true;

        try
        {
            var request = await replayManager.LoadReplay(replayFilePath);
            await request.Task;
        }
        finally
        {
            IsLoadingReplays = false;
        }
    }

    public Task SearchForAndLoadReplays()
    {
        return SearchForAndLoadReplaysImpl(
            ReplayFolders.SelectMany(x => x is ReplaysFolderItemViewModel item
                    ? new[] { item }
                    : [])
                .ToList());
    }

    private async Task SearchForAndLoadReplaysImpl(ICollection<ReplaysFolderItemViewModel> replayFolders)
    {
        IsLoadingReplays = true;

        try
        {
            var request = await replayManager.SearchForAndLoadReplays(
                replayFolders.SelectMany(x =>
                        x.ReplayFolderFilePath is not null and not ""
                            ? new[] { x.ReplayFolderFilePath! }
                            : [])
                    .ToList());
            await request.Task;
        }
        finally
        {
            IsLoadingReplays = false;
        }
    }

    private void UpdateLoadingState(ILoadReplaysRequest request)
    {
        var replayFolderItems = ReplayFolders.SelectMany(
            x => x is ReplaysFolderItemViewModel item
                ? new[] { item }
                : []);

        foreach (var replayFolderItem in replayFolderItems)
            replayFolderItem.IsLoadingReplays =
                request.RequestedPaths.Contains(replayFolderItem.ReplayFolderFilePath);

        UpdateLoadingState(request.CurrentLoadingState);
    }

    private void UpdateLoadingState(ReplayLoadingState state)
    {
        IsLoadingReplays = !state.IsLoadingComplete;
        LoadedReplays = state.LoadedReplays;
        TotalReplays = state.TotalReplays;
        CurrentReplay = state.ReplayFilePath;

        IsProgressIndeterminate = state.LoadedReplays == null || state.TotalReplays == null || state.TotalReplays == 0;

        if (state is { LoadedReplays: { } loadedReplays, TotalReplays: not 0 and { } totalReplays })
            Progress = Math.Round(100d * loadedReplays / totalReplays);
        else
            Progress = 0;
    }

    protected override async void OnActivated()
    {
        var currentSettings = await settingsService.GetCurrentSettings();

        foreach (var path in currentSettings.ReplayFolderPaths ?? DefaultSettings.ReplayFolderPaths)
            ReplayFolders.Add(new ReplaysFolderItemViewModel
            {
                ReplayFolderFilePath = path
            });

        ScanFoldersOnAppStart = currentSettings.ScanReplayFoldersOnAppStart ?? DefaultSettings.ScanReplayFoldersOnAppStart;
        WatchReplayFolders = currentSettings.WatchReplayFolders ?? DefaultSettings.WatchReplayFolders;

        _sub.Add(settingsService.SettingsChangedObservable.Subscribe(
            Observer.Create<(string SettingsKey, DataModel.Settings Change)>(args => dispatcher.TryEnqueue(
                () =>
                {
                    switch (args.SettingsKey)
                    {
                        case nameof(DataModel.Settings.ReplayFolderPaths):
                        {
                            var newFolderPaths = args.Change.ReplayFolderPaths;
                            switch (newFolderPaths)
                            {
                                case not null when newFolderPaths.SequenceEqual(
                                    ReplayFolders.Select(x => x.ReplayFolderFilePath)):
                                    break;
                                case null:
                                    ReplayFolders.Clear();
                                    break;
                                default:
                                {
                                    ReplayFolders.Clear();

                                    foreach (var path in newFolderPaths)
                                        ReplayFolders.Add(new ReplaysFolderItemViewModel
                                        {
                                            ReplayFolderFilePath = path
                                        });

                                    break;
                                }
                            }

                            break;
                        }

                        case nameof(DataModel.Settings.ScanReplayFoldersOnAppStart):
                        {
                            ScanFoldersOnAppStart = args.Change.ScanReplayFoldersOnAppStart ?? DefaultSettings.ScanReplayFoldersOnAppStart;
                            break;
                        }

                        case nameof(DataModel.Settings.WatchReplayFolders):
                        {
                            WatchReplayFolders = args.Change.WatchReplayFolders ?? DefaultSettings.WatchReplayFolders;
                            break;
                        }
                    }
                }))));

        _sub.Add(replayManager.LoadReplaysRequestsObservable.Subscribe(
            Observer.Create<ILoadReplaysRequest>(request =>
            {
                dispatcher.EnqueueAsync(async () =>
                {
                    do
                    {
                        UpdateLoadingState(request);

                        await Task.WhenAny(Task.Delay(500), request.Task);
                    } while (!request.Task.IsCompleted);
                });
            })));

        ReplayFolders.CollectionChanged += OnReplayFoldersCollectionChanged;
        PropertyChanged += OnSelfPropertyChanged;
    }

    protected override void OnDeactivated()
    {
        _sub.Dispose();
        ReplayFolders.CollectionChanged -= OnReplayFoldersCollectionChanged;
        PropertyChanged -= OnSelfPropertyChanged;
    }

    private void OnReplayFoldersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        settingsService.ApplySettingsChange(
            nameof(DataModel.Settings.ReplayFolderPaths),
            new DataModel.Settings { ReplayFolderPaths = ReplayFolders.Select(x => x.ReplayFolderFilePath!).ToList() });
    }

    private async void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IReplaysPageViewModel.ScanFoldersOnAppStart):
                await settingsService.ApplySettingsChange(nameof(DataModel.Settings.ScanReplayFoldersOnAppStart),
                    new DataModel.Settings { ScanReplayFoldersOnAppStart = ScanFoldersOnAppStart });
                break;
            case nameof(IReplaysPageViewModel.WatchReplayFolders):
                await settingsService.ApplySettingsChange(nameof(DataModel.Settings.WatchReplayFolders),
                    new DataModel.Settings { WatchReplayFolders = WatchReplayFolders });
                break;
        }
    }

    private partial class ReplaysFolderItemViewModel : ObservableRecipient,
        IReplayFolderItemViewModel
    {
        [ObservableProperty] private bool _isLoadingReplays;
        [ObservableProperty] private string? _replayFolderFilePath;
    }
}