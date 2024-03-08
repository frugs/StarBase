using System;
using System.Threading.Tasks;
using Windows.Storage;
using PlayerDB.DataModel;

namespace PlayerDB.SettingsStorage.ApplicationData;

public class ApplicationDataSettingsStorage : ISettingsStorage
{
    private static ApplicationDataContainer LocalSettings => Windows.Storage.ApplicationData.Current.LocalSettings;

    public Task<Settings> LoadSettings()
    {
        Settings settings = new();
        {
            if (LocalSettings.Values.TryGetValue(MapPropertyToKey(nameof(Settings.ReplayFolderPaths)),
                    out var value) &&
                value is string stringValue and not "")
                settings.ReplayFolderPaths = [.. Split(stringValue)];
        }
        {
            if (LocalSettings.Values.TryGetValue(
                    MapPropertyToKey(nameof(Settings.ScanReplayFoldersOnAppStart)),
                    out var value) && value is bool boolValue)
                settings.ScanReplayFoldersOnAppStart = boolValue;
        }
        {
            if (LocalSettings.Values.TryGetValue(
                    MapPropertyToKey(nameof(Settings.WatchReplayFolders)),
                    out var value) && value is bool boolValue)
                settings.WatchReplayFolders = boolValue;
        }
        {
            if (LocalSettings.Values.TryGetValue(MapPropertyToKey(nameof(Settings.PlayerToons)),
                    out var value) &&
                value is string stringValue and not "")
                settings.PlayerToons = [.. Split(stringValue)];
        }

        return Task.FromResult(settings);

        static string[] Split(string str)
        {
            return str.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public Task StoreSettings(Settings settings)
    {
        if (settings.ReplayFolderPaths != null)
            LocalSettings.Values[MapPropertyToKey(nameof(Settings.ReplayFolderPaths))] =
                string.Join(',', settings.ReplayFolderPaths);

        if (settings.ScanReplayFoldersOnAppStart != null)
            LocalSettings.Values[MapPropertyToKey(nameof(Settings.ScanReplayFoldersOnAppStart))] =
                settings.ScanReplayFoldersOnAppStart;

        if (settings.WatchReplayFolders != null)
            LocalSettings.Values[MapPropertyToKey(nameof(Settings.WatchReplayFolders))] =
                settings.WatchReplayFolders;

        if (settings.PlayerToons != null)
            LocalSettings.Values[MapPropertyToKey(nameof(Settings.PlayerToons))] =
                string.Join(',', settings.PlayerToons);

        return Task.CompletedTask;
    }

    private static string MapPropertyToKey(string propertyName)
    {
        return propertyName switch
        {
            nameof(Settings.ReplayFolderPaths) =>
                nameof(Settings.ReplayFolderPaths),
            nameof(Settings.ScanReplayFoldersOnAppStart) =>
                nameof(Settings.ScanReplayFoldersOnAppStart),
            nameof(Settings.WatchReplayFolders) =>
                nameof(Settings.WatchReplayFolders),
            nameof(Settings.PlayerToons) =>
                nameof(Settings.PlayerToons),
            _ => ""
        };
    }
}