using PlayerDB.DataModel;

namespace PlayerDB.DataStorage;

public interface IPlayerRepository
{
    Task<Player?> FindPlayerByIdPartial(int id, CancellationToken cancellation = default);

    Task<Player?> FindPlayerByIdFull(int id, CancellationToken cancellation = default);

    Task<Player?> FindPlayerByToon(string toon, CancellationToken cancellation = default);

    Task<List<Player>> MatchPlayersByName(string name, long? recentSecs = null, CancellationToken cancellation = default);

    Task SavePlayer(Player player, CancellationToken cancellation = default);

    Task DeleteAllPlayers();

    IObservable<Player> PlayerAddedOrUpdatedObservable { get; }
}