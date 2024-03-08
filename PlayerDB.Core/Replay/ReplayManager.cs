using System.Collections.Concurrent;
using System.Reactive.Subjects;
using PlayerDB.DataModel;
using PlayerDB.DataStorage;
using PlayerDB.LifeCycle;
using PlayerDB.Replay;
using PlayerDB.Utilities;

namespace PlayerDB.Core.Replay;

public sealed class ReplayManager(
    IReplayParser replayParser,
    IReplayRepository replayRepository,
    IBuildOrderRepository buildOrderRepository,
    IPlayerRepository playerRepository) : IReplayManager, IShutDown, IDisposable
{
    private static readonly ILoadReplaysRequest CompletedLoadReplaysRequest =
        new LoadReplaysRequest(_ => Task.CompletedTask) { IsLoadingComplete = true };

    private readonly BehaviorSubject<ILoadReplaysRequest> _loadReplaysRequestsSubject =
        new(CompletedLoadReplaysRequest);

    private readonly SerialTaskQueue _taskQueue = new(nameof(ReplayManager));

    public void Dispose()
    {
        _taskQueue.Dispose();
    }

    public IObservable<ILoadReplaysRequest> LoadReplaysRequestsObservable => _loadReplaysRequestsSubject;

    public Task<ILoadReplaysRequest> LoadReplay(string replayFilePath, CancellationToken cancellation = default)
    {
        var tcs = new TaskCompletionSource<ILoadReplaysRequest>();
        cancellation.Register(() => tcs.TrySetCanceled(cancellation));
        _taskQueue.Enqueue(async () =>
        {
            var loadReplaysRequest =
                new LoadReplaysRequest(request => LoadReplayInner(request, replayFilePath, cancellation));
            _loadReplaysRequestsSubject.OnNext(loadReplaysRequest);
            tcs.SetResult(loadReplaysRequest);

            await loadReplaysRequest.Task;
            _loadReplaysRequestsSubject.OnNext(CompletedLoadReplaysRequest);
        }, cancellation);
        return tcs.Task;
    }

    public Task<ILoadReplaysRequest> SearchForAndLoadReplays(
        IReadOnlyCollection<string> rootFilePaths,
        CancellationToken cancellation = default)
    {
        var tcs = new TaskCompletionSource<ILoadReplaysRequest>();
        cancellation.Register(() => tcs.TrySetCanceled(cancellation));
        _taskQueue.Enqueue(async () =>
        {
            var loadReplaysRequest =
                new LoadReplaysRequest(
                    request => LoadSearchForAndLoadReplaysInner(request, rootFilePaths, cancellation));
            _loadReplaysRequestsSubject.OnNext(loadReplaysRequest);
            tcs.SetResult(loadReplaysRequest);

            await loadReplaysRequest.Task;
            _loadReplaysRequestsSubject.OnNext(CompletedLoadReplaysRequest);
        }, cancellation);
        return tcs.Task;
    }

    public Task ShutDown()
    {
        return _taskQueue.ShutDown();
    }

    private async Task LoadSearchForAndLoadReplaysInner(
        LoadReplaysRequest request,
        IReadOnlyCollection<string> rootFilePaths,
        CancellationToken cancellation)
    {
        request.RequestedPaths = rootFilePaths;

        var replayFilePaths = new ConcurrentQueue<string>();

        foreach (var rootFilePath in rootFilePaths)
            await Parallel.ForEachAsync(
                Directory.EnumerateFiles(rootFilePath, "*.SC2Replay", SearchOption.AllDirectories),
                cancellation,
                async (replayFilePath, _) =>
                {
                    // TODO: Re-parse if parser version for data in db is outdated
                    if (await replayRepository.FindReplayByPath(
                            replayFilePath,
                            cancellation) is not null) return;

                    replayFilePaths.Enqueue(replayFilePath);

                    request.TotalReplays = replayFilePaths.Count;
                });

        request.LoadedReplays = 0;
        request.TotalReplays = replayFilePaths.Count;
        request.ReplayFilePath = replayFilePaths.FirstOrDefault() ?? "";

        const int bufferSize = 293;
        var buffer = new List<ParseResult>(bufferSize);

        await foreach (var parseResult in
                       replayParser.ParseReplaysParallel(replayFilePaths.ToList(),
                               IReplayParser.Mode.Fast)
                           .WithCancellation(cancellation))
        {
            if (buffer.Count < bufferSize)
            {
                buffer.Add(parseResult);
                continue;
            }

            request.ReplayFilePath = buffer.FirstOrDefault()?.Replay.FilePath ?? "";

            await SaveParseResults(buffer, cancellation);

            request.LoadedReplays += buffer.Count;

            buffer.Clear();
        }

        await SaveParseResults(buffer, cancellation);

        request.LoadedReplays = replayFilePaths.Count;
        request.TotalReplays = replayFilePaths.Count;
        request.IsLoadingComplete = true;
    }

    private async Task LoadReplayInner(LoadReplaysRequest request, string replayFilePath,
        CancellationToken cancellation = default)
    {
        request.LoadedReplays = 1;
        request.TotalReplays = 1;
        request.ReplayFilePath = replayFilePath;

        // Always upsert with freshly parsed data if loading a single replay
        // This includes removing the build order data from the previous parsing
        var parseResult = await replayParser.ParseReplay(replayFilePath);
        if (parseResult == null) return;

        var existingReplay = await replayRepository.FindReplayByPath(replayFilePath, cancellation);
        if (existingReplay is not null) parseResult.Replay.Id = existingReplay.Id;

        await SaveParseResults([parseResult], cancellation);

        request.IsLoadingComplete = true;
    }

    private async Task SaveParseResults(
        IReadOnlyCollection<ParseResult> parseResults,
        CancellationToken cancellation = default)
    {
        await Parallel.ForEachAsync(
            parseResults,
            cancellation,
            async (parseResult, _) =>
            {
                await replayRepository.SaveReplay(parseResult.Replay, cancellation);

                foreach (var player in parseResult.Players)
                foreach (var buildOrder in player.BuildOrders ?? [])
                    buildOrder.Replay = parseResult.Replay;
            });

        var buildOrders = parseResults
            .SelectMany(x => x.Players)
            .SelectMany(x => x.BuildOrders ?? [])
            .ToList();

        await Parallel.ForEachAsync(
            buildOrders,
            cancellation,
            async (buildOrder, _) =>
            {
                var dbBuildOrder = await buildOrderRepository.FindBuildOrderByKey(buildOrder.Key ?? "", cancellation);
                if (dbBuildOrder != null)
                {
                    buildOrder.BuildOrderActions ??= dbBuildOrder.BuildOrderActions;
                    buildOrder.Id = dbBuildOrder.Id;
                }

                await buildOrderRepository.SaveBuildOrder(buildOrder, cancellation);
            });

        var players = parseResults
            .SelectMany(x => x.Players)
            .Aggregate(
                new Dictionary<string, DataModel.Player>(),
                (acc, next) =>
                {
                    if (next.Toon is null) return acc;

                    if (acc.TryGetValue(next.Toon, out var player))
                    {
                        player.BuildOrders ??= [];
                        player.BuildOrders.AddRange(next.BuildOrders ?? []);
                    }
                    else
                    {
                        acc[next.Toon] = next;
                    }

                    return acc;
                })
            .Values
            .ToList();

        await Parallel.ForEachAsync(
            players,
            cancellation,
            async (player, _) =>
            {
                var dbPlayer = await playerRepository.FindPlayerByToon(player.Toon ?? "", cancellation);
                if (dbPlayer != null)
                {
                    var playerBuildOrders = player.BuildOrders ?? [];

                    dbPlayer.BuildOrders ??= [];
                    dbPlayer.BuildOrders.AddRange(playerBuildOrders
                        .Where(x => !dbPlayer
                            .BuildOrders
                            .Select(y => y.Id)
                            .ToHashSet()
                            .Contains(x.Id)));

                    await playerRepository.SavePlayer(dbPlayer, cancellation);
                }
                else
                {
                    await playerRepository.SavePlayer(player, cancellation);
                }
            });
    }

    private class LoadReplaysRequest : ILoadReplaysRequest
    {
        public LoadReplaysRequest(Func<LoadReplaysRequest, Task> loadReplaysTaskFactory)
        {
            Task = loadReplaysTaskFactory(this);
        }

        public int? LoadedReplays { get; set; }

        public int? TotalReplays { get; set; }

        public bool IsLoadingComplete { get; set; }

        public string ReplayFilePath { get; set; } = "";

        public Task Task { get; }

        public ReplayLoadingState CurrentLoadingState =>
            new(LoadedReplays, TotalReplays, IsLoadingComplete, ReplayFilePath);

        public IReadOnlyCollection<string> RequestedPaths { get; set; } = [];
    }
}