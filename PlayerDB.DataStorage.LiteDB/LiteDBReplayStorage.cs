using LiteDB;

namespace PlayerDB.DataStorage.LiteDB;

public class LiteDBReplayStorage(ILiteDBRunner runner) : IReplayRepository
{
    public Task SaveReplay(DataModel.Replay replay, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
        {
            var col = db.GetCollection<DataModel.Replay>();
            col.EnsureIndex(x => x.FilePath);

            return col
                .Upsert(replay);
        }, cancellation);
    }

    public Task<DataModel.Replay?> FindReplayByPath(string path, CancellationToken cancellation = default)
    {
        return runner.Perform(db =>
            (DataModel.Replay?)db.GetCollection<DataModel.Replay>()
                .FindOne(Query.EQ(nameof(DataModel.Replay.FilePath), path)), cancellation);
    }

    public Task DeleteAllReplays()
    {
        return runner.Perform(db => db.GetCollection<DataModel.Replay>().DeleteAll());
    }
}