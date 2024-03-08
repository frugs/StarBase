using PlayerDB.DataModel;

namespace PlayerDB.Core.FileSystem;

public sealed class ReplayWatcherController(
    IReplayWatcher replayWatcher,
    IObservable<(string SettingsKey, DataModel.Settings Change)> settingsChangedObservable) : IDisposable, IReplayWatcherController
{
    private IDisposable? _sub;

    public void Dispose()
    {
        _sub?.Dispose();
    }

    public void Start(DataModel.Settings initialSettings)
    {
        replayWatcher.Start();

        ApplyWatchedPathsFromSettings(initialSettings);
        EnableOrDisableFromSettings(initialSettings);

        _sub = settingsChangedObservable.Subscribe(args =>
        {
            switch (args.SettingsKey)
            {
                case nameof(DataModel.Settings.WatchReplayFolders):
                    EnableOrDisableFromSettings(args.Change);
                    break;

                case nameof(DataModel.Settings.ReplayFolderPaths):
                    ApplyWatchedPathsFromSettings(args.Change);
                    break;
            }
        });
    }

    private void ApplyWatchedPathsFromSettings(DataModel.Settings settings)
    {
        replayWatcher.SetWatchedPaths(settings.ReplayFolderPaths ?? DefaultSettings.ReplayFolderPaths);
    }

    private void EnableOrDisableFromSettings(DataModel.Settings settings)
    {
        if (settings.WatchReplayFolders ?? DefaultSettings.WatchReplayFolders)
            replayWatcher.Enable();
        else
            replayWatcher.Disable();
    }
}