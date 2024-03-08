using System.Reactive.Subjects;
using PlayerDB.LifeCycle;
using PlayerDB.SettingsStorage;
using PlayerDB.Utilities;

namespace PlayerDB.Core.Settings;

public sealed class SettingsService(ISettingsStorage storage) : ISettingsService, IDisposable, IShutDown
{
    private readonly Subject<(string SettingsKey, DataModel.Settings Change)> _settingsChangedSubject = new();
    private readonly SerialTaskQueue _taskQueue = new(nameof(SettingsService));

    private DataModel.Settings? _currentSettings;

    public void Dispose()
    {
        _taskQueue.Dispose();
    }

    public IObservable<(string SettingsKey, DataModel.Settings Change)> SettingsChangedObservable =>
        _settingsChangedSubject;

    public async Task<DataModel.Settings> GetCurrentSettings()
    {
        if (_currentSettings != null) return _currentSettings;

        return await _taskQueue.Enqueue(async () =>
        {
            if (_currentSettings != null) return _currentSettings;

            return await storage.LoadSettings();
        });
    }

    public Task ClearSettingsKey(string settingsKey)
    {
        return ApplySettingsChange(settingsKey, new DataModel.Settings());
    }

    public Task ApplySettingsChange(string settingsKey, DataModel.Settings change)
    {
        return _taskQueue.Enqueue(async () =>
        {
            _currentSettings ??= await storage.LoadSettings();

            if (!_currentSettings.ApplyChange(settingsKey, change)) return;

            await storage.StoreSettings(_currentSettings);

            _settingsChangedSubject.OnNext((settingsKey, change));
        });
    }

    public Task ShutDown()
    {
        return _taskQueue.ShutDown();
    }
}

public static class SettingsExtensions
{
    public static bool ApplyChange(
        this DataModel.Settings originalSettings,
        string settingsKey,
        DataModel.Settings change)
    {
        return settingsKey switch
        {
            nameof(DataModel.Settings.ReplayFolderPaths) => ApplyListChange(
                originalSettings.ReplayFolderPaths,
                change.ReplayFolderPaths,
                value => originalSettings.ReplayFolderPaths = value),
            nameof(DataModel.Settings.ScanReplayFoldersOnAppStart) => ApplyBoolChange(
                originalSettings.ScanReplayFoldersOnAppStart,
                change.ScanReplayFoldersOnAppStart,
                value => originalSettings.ScanReplayFoldersOnAppStart = value),
            nameof(DataModel.Settings.WatchReplayFolders) => ApplyBoolChange(
                originalSettings.WatchReplayFolders,
                change.WatchReplayFolders,
                value => originalSettings.WatchReplayFolders = value),
            nameof(DataModel.Settings.PlayerToons) => ApplyListChange(
                originalSettings.PlayerToons,
                change.PlayerToons,
                value => originalSettings.PlayerToons = value),
            _ => false
        };

        static bool ApplyListChange<T>(IReadOnlyCollection<T>? originalList, IReadOnlyCollection<T>? changeList, Action<IReadOnlyCollection<T>?> setter)
        {
            switch (originalList, changeList)
            {
                case var (x, y) when x == y:
                    return false;
                case (_, null):
                    setter(null);
                    return true;
                case (not null, not null) when originalList.SequenceEqual(changeList):
                    return false;
                case var (_, _):
                    setter(changeList.ToList());
                    return true;
            }
        }

        static bool ApplyBoolChange(bool? originalBool, bool? changeBool, Action<bool?> setter)
        {
            if (originalBool == changeBool) return false;

            setter(changeBool);
            return true;
        }
    }
}