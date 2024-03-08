using System.Threading.Tasks;
using PlayerDB.Core.Settings;
using PlayerDB.DataStorage;

namespace PlayerDB.App.Settings;

public class SettingsPageViewModel(
    IBuildOrderRepository buildOrderRepository,
    IPlayerRepository playerRepository,
    IReplayRepository replayRepository,
    ISettingsService settingsService) : ISettingsPageViewModel
{
    public Task ClearPlayersAndBuildOrders()
    {
        return Task.WhenAll(
            settingsService.ClearSettingsKey(nameof(DataModel.Settings.PlayerToons)),
            buildOrderRepository.DeleteAllBuildOrders(),
            playerRepository.DeleteAllPlayers(),
            replayRepository.DeleteAllReplays()
        );
    }

    public async Task ResetSettings()
    {
        await settingsService.ClearSettingsKey(nameof(DataModel.Settings.ScanReplayFoldersOnAppStart));
        await settingsService.ClearSettingsKey(nameof(DataModel.Settings.ReplayFolderPaths));
        await settingsService.ClearSettingsKey(nameof(DataModel.Settings.WatchReplayFolders));
    }
}