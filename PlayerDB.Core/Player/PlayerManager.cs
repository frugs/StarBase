using PlayerDB.Core.Settings;
using PlayerDB.DataModel;
using PlayerDB.Utilities;

namespace PlayerDB.Core.Player;

public class PlayerManager(ISettingsService settingsService) : IPlayerManager
{
    private readonly SerialTaskQueue _queue = new(nameof(PlayerManager));

    public Task SetPlayerIsMe(string toon, bool value, CancellationToken cancellation = default)
    {
        return _queue.Enqueue(async () =>
        {
            var settingsSnapshot = await settingsService.GetCurrentSettings();
            var playerToons = (settingsSnapshot.PlayerToons ?? DefaultSettings.PlayerToons).ToList();

            switch (value)
            {
                case true when playerToons.Contains(toon):
                    return;
                case false when !playerToons.Contains(toon):
                    return;

                case true:
                    playerToons.Add(toon);
                    break;
                case false:
                    playerToons.RemoveAll(x => x == toon);
                    break;
            }

            await settingsService.ApplySettingsChange(
                nameof(DataModel.Settings.PlayerToons),
                new DataModel.Settings { PlayerToons = playerToons });
        }, cancellation);
    }
}