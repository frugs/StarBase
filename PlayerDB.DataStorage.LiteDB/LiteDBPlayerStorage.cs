﻿using LiteDB;
using PlayerDB.DataModel;

namespace PlayerDB.DataStorage.LiteDB;

public class LiteDBPlayerStorage(ILiteDBRunner runner) : IPlayerRepository
{
    static LiteDBPlayerStorage()
    {
        BsonMapper.Global.Entity<Player>()
            .DbRef(x => x.BuildOrders, nameof(BuildOrder));
    }

    public Task<Player?> FindPlayerByIdPartial(int id, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
                (Player?)db.GetCollection<Player>().FindById(id),
            cancellation);
    }

    public Task<Player?> FindPlayerByIdFull(int id, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
                (Player?)GetFullCollection(db).FindById(id),
            cancellation);
    }

    public Task<List<Player>> MatchPlayersByName(string name, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
                db.GetCollection<Player>()
                    .Find(Query.EQ(nameof(Player.Name), name))
                    .ToList(),
            cancellation);
    }

    public Task<Player?> FindPlayerByToon(string toon, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
                (Player?)GetFullCollection(db).FindOne(
                    Query.EQ(nameof(Player.Toon), toon)),
            cancellation);
    }

    public Task SavePlayer(Player player, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<Player>();
            col.EnsureIndex(x => x.Name);
            col.EnsureIndex(x => x.Toon);

            return col.Upsert(player);
        }, cancellation);
    }

    public Task DeleteAllPlayers()
    {
        return runner.Perform(db => db.GetCollection<Player>().DeleteAll());
    }

    private static ILiteCollection<Player> GetFullCollection(ILiteDatabase db)
    {
        return db.GetCollection<Player>()
            .Include(BsonExpression.Create(nameof(Player.BuildOrders)))
            .Include(BsonExpression.Create(
                $"{nameof(Player.BuildOrders)}[*].{nameof(BuildOrder.Replay)}"));
    }
}