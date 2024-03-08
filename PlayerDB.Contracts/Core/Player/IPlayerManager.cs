namespace PlayerDB.Core.Player;

public interface IPlayerManager
{
    Task SetPlayerIsMe(string toon, bool value, CancellationToken cancellation = default);
}