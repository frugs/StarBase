using PlayerDB.DataModel;

namespace PlayerDB.Game;

public record GamePlayerData(string PlayerName, StarCraftRace? Race);

public record GameData(List<GamePlayerData> Players)
{
    // Custom equals and hashcode implementations as we want value semantics even though our members contain reference types
    public virtual bool Equals(GameData? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Players.SequenceEqual(other.Players);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Players.Aggregate(19, (current, player) => current * 31 + player.GetHashCode());
        }
    }
}

public interface IGameClient
{
    public Task<GameData?> ReadGameData(CancellationToken cancellation);
}