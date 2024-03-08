using PlayerDB.Game;

namespace PlayerDB.Core.Game;

public interface IGameClientPollingService
{
    bool IsStarted { get; set; }

    IObservable<GameData> GameClientObservable { get; }
}