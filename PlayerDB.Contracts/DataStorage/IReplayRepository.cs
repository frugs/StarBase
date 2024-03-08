namespace PlayerDB.DataStorage;

public interface IReplayRepository
{
    Task SaveReplay(DataModel.Replay replay, CancellationToken cancellation = default);

    Task<DataModel.Replay?> FindReplayByPath(string path, CancellationToken cancellation = default);

    Task DeleteAllReplays();
}