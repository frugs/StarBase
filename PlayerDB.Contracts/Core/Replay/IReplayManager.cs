namespace PlayerDB.Core.Replay;

public record ReplayLoadingState(
    int? LoadedReplays = null,
    int? TotalReplays = null,
    bool IsLoadingComplete = false,
    string? ReplayFilePath = null,
    IReadOnlyCollection<string>? RequestedPaths = null);

public interface ILoadReplaysRequest
{
    Task Task { get; }
    ReplayLoadingState CurrentLoadingState { get; }
    IReadOnlyCollection<string> RequestedPaths { get; }
}

public interface IReplayManager
{
    public IObservable<ILoadReplaysRequest> LoadReplaysRequestsObservable { get; }

    public Task<ILoadReplaysRequest> LoadReplay(string replayFilePath, CancellationToken cancellation = default);

    public Task<ILoadReplaysRequest> SearchForAndLoadReplays(
        IReadOnlyCollection<string> rootFilePaths,
        CancellationToken cancellation = default);
}