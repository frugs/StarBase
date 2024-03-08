using PlayerDB.Game;

namespace PlayerDB.Core.Game;

public static class GamePlayerDataExtensions
{
    public static GamePlayerData? GetOpponent(this GamePlayerData? player, IEnumerable<GamePlayerData>? players)
    {
        if (player == null || players == null) return null;

        return players.FirstOrDefault(x => !x.Equals(player));
    }
}