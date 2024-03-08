using LiteDB;
using PlayerDB.DataModel;

namespace PlayerDB.DataStorage.LiteDB;

public class LiteDBBuildOrderStorage(ILiteDBRunner runner) : IBuildOrderRepository
{
    static LiteDBBuildOrderStorage()
    {
        BsonMapper.Global.Entity<BuildOrder>()
            .DbRef(x => x.Replay, nameof(DataModel.Replay));
    }

    public Task<BuildOrder?> FindBuildOrderById(int id, CancellationToken cancellation = default)
    {
        return runner.Perform(
            db =>
            {
                return (BuildOrder?)db.GetCollection<BuildOrder>()
                    .Include(x => x.Replay)
                    .FindById(id);
            }, cancellation);
    }

    public Task<BuildOrder?> FindBuildOrderByKey(string key, CancellationToken cancellation = default)
    {
        return runner.Perform(
            db =>
            {
                return (BuildOrder?)db.GetCollection<BuildOrder>()
                    .Include(x => x.Replay)
                    .FindOne(Query.EQ(nameof(BuildOrder.Key), key));
            }, cancellation);
    }

    public Task SaveBuildOrder(BuildOrder buildOrder, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<BuildOrder>();
            col.EnsureIndex(x => x.Replay);
            col.EnsureIndex(x => x.Key);

            col.Upsert(buildOrder);
        }, cancellation);
    }

    public Task DeleteBuildOrder(BuildOrder buildOrder, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<BuildOrder>();
            col.Delete(buildOrder.Id);
            col.DeleteMany(Query.EQ(nameof(BuildOrder.Key), buildOrder.Key ?? ""));
        }, cancellation);
    }

    public Task DeleteBuildOrders(IReadOnlyCollection<BuildOrder> buildOrders,
        CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<BuildOrder>();
            col.DeleteMany(Query.In(nameof(BuildOrder.Key),
                new BsonArray(buildOrders.Select(x => new BsonValue(x.Key)).ToList())));
        }, cancellation);
    }

    public Task DeleteBuildOrdersForReplay(DataModel.Replay existingReplay, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<BuildOrder>();
            col.DeleteMany(Query.EQ("Replay", BsonMapper.Global.ToDocument(existingReplay)));
        }, cancellation);
    }

    public Task DeleteAllBuildOrders()
    {
        return runner.Perform(db => db.GetCollection<BuildOrder>().DeleteAll());
    }
}