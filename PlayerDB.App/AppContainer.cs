using System;
using System.Net.Http;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.UI.Dispatching;
using PlayerDB.App.GameClient;
using PlayerDB.App.Navigation;
using PlayerDB.App.Replays;
using PlayerDB.App.Settings;
using PlayerDB.Core.BuildOrder;
using PlayerDB.Core.FileSystem;
using PlayerDB.Core.Game;
using PlayerDB.Core.Player;
using PlayerDB.Core.Replay;
using PlayerDB.Core.Settings;
using PlayerDB.DataStorage.LiteDB;
using PlayerDB.Game.StarCraft2;
using PlayerDB.LifeCycle;
using PlayerDB.Replay.StarCraft2;
using PlayerDB.SettingsStorage.ApplicationData;

namespace PlayerDB.App;

public sealed class AppContainer(
    GameClientPollingService gameClientPollingService,
    HttpClient httpClient,
    LiteDatabase liteDatabase,
    ParallelLiteDBRunner parallelLiteDBRunner,
    SettingsService settingsService,
    ReplayManager replayManager,
    ReplayWatcherController replayWatcherController,
    GameClientPageViewModel gameClientPageViewModel,
    ReplayWatcher replayWatcher,
    ReplaysPageViewModel replaysViewModel,
    IMainPageViewModel mainPageViewModel)
    : IDisposable, IShutDown
{
    public ISettingsService SettingsService { get; } = settingsService;

    public IReplayManager ReplayManager { get; } = replayManager;

    public IReplayWatcherController ReplayWatcherController { get; } = replayWatcherController;

    public IMainPageViewModel MainPageViewModel { get; } = mainPageViewModel;

    public void Dispose()
    {
        gameClientPollingService.Dispose();
        httpClient.Dispose();
        liteDatabase.Dispose();
        settingsService.Dispose();
        replayManager.Dispose();
        replayWatcherController.Dispose();
        replayWatcher.Dispose();
        gameClientPageViewModel.Dispose();
        replaysViewModel.Dispose();
    }

    public Task ShutDown()
    {
        return Task.WhenAll([
            gameClientPollingService.ShutDown(),
            settingsService.ShutDown(),
            replayManager.ShutDown(),
            replayWatcher.ShutDown(),
            parallelLiteDBRunner.ShutDown()
        ]);
    }

    public static AppContainer CreateAppContainer(string dbPath)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();

        var liteDB = new LiteDatabase(dbPath);
        var liteDBRunner = new ParallelLiteDBRunner(liteDB);

        var replayRepository = new LiteDBReplayStorage(liteDBRunner);
        var playerRepository = new LiteDBPlayerStorage(liteDBRunner);
        var buildOrderRepository = new LiteDBBuildOrderStorage(liteDBRunner);

        var httpClient = new HttpClient();
        var gameClientPollingService = new GameClientPollingService(new StarCraft2GameClient(httpClient))
        {
            IsStarted = true
        };

        var replayManager = new ReplayManager(
            new StarCraft2ReplayParser(),
            replayRepository,
            buildOrderRepository,
            playerRepository);

        var buildOrderManager = new BuildOrderManager(replayManager, buildOrderRepository);

        var settingsService = new SettingsService(new ApplicationDataSettingsStorage());

        var playerManager = new PlayerManager(settingsService);

        var gameClientViewModel = new GameClientPageViewModel(
            new PlayerDetailsViewModel(dispatcher, buildOrderManager, playerManager, playerRepository, settingsService),
            dispatcher,
            playerRepository,
            settingsService,
            gameClientPollingService);


        var replayWatcher = new ReplayWatcher("SC2Replay", replayManager);

        var replayWatcherController =
            new ReplayWatcherController(replayWatcher, settingsService.SettingsChangedObservable);

        return new AppContainer(
            gameClientPollingService,
            httpClient,
            liteDB,
            liteDBRunner,
            settingsService,
            replayManager,
            replayWatcherController,
            gameClientViewModel,
            replayWatcher,
            new ReplaysPageViewModel(
                dispatcher,
                replayManager,
                settingsService),
            new MainPageViewModel(
                new PlayerDBNavigationViewModel(
                    gameClientViewModel,
                    new ReplaysPageViewModel(
                        dispatcher,
                        replayManager,
                        settingsService),
                    new SettingsPageViewModel(
                        buildOrderRepository,
                        playerRepository,
                        replayRepository,
                        settingsService))));
    }
}