using PlayerDB.Core.Replay;
using PlayerDB.DataModel;
using PlayerDB.DataStorage;

namespace PlayerDB.Core.BuildOrder;

public class BuildOrderManager(
    IReplayManager replayManager,
    IBuildOrderRepository buildOrderRepository) : IBuildOrderManager
{
    public IReadOnlyCollection<DataModel.BuildOrder> RecentValidBuildOrdersForPlayer(DataModel.Player player,
        int maxBuildOrdersPerMatchUp = 3)
    {
        var playerBuildOrders = player.BuildOrders ?? [];

        var recentBuildOrders = new[]
        {
            (StarCraftRace.Terran, StarCraftRace.Terran),
            (StarCraftRace.Terran, StarCraftRace.Protoss),
            (StarCraftRace.Terran, StarCraftRace.Zerg),
            (StarCraftRace.Protoss, StarCraftRace.Terran),
            (StarCraftRace.Protoss, StarCraftRace.Protoss),
            (StarCraftRace.Protoss, StarCraftRace.Zerg),
            (StarCraftRace.Zerg, StarCraftRace.Terran),
            (StarCraftRace.Zerg, StarCraftRace.Protoss),
            (StarCraftRace.Zerg, StarCraftRace.Zerg)
        }.ToDictionary(x => x, _ => new List<DataModel.BuildOrder>(maxBuildOrdersPerMatchUp));

        foreach (var buildOrder in playerBuildOrders.OrderByDescending(x => x.ReplayStartTimeUtc))
        {
            if (recentBuildOrders.Values.All(x => x.Count >= maxBuildOrdersPerMatchUp)) break;
            if (!buildOrder.IsValid) continue;

            var matchUpBuildOrders =
                recentBuildOrders.GetValueOrDefault((buildOrder.PlayerRace, buildOrder.OpponentRace));
            if (matchUpBuildOrders is not null && matchUpBuildOrders.Count < maxBuildOrdersPerMatchUp)
                matchUpBuildOrders.Add(buildOrder);
        }

        return recentBuildOrders.Values.SelectMany(x => x).ToList();
    }

    public async Task EnsureBuildOrderActions(DataModel.BuildOrder buildOrder, CancellationToken cancellation = default)
    {
        if (buildOrder.BuildOrderActions is not null and not [] ||
            buildOrder.Replay?.FilePath is null or "" ||
            buildOrder.Key is null or "") return;

        var request = await replayManager.LoadReplay(buildOrder.Replay.FilePath, cancellation);
        await request.Task;
        var reloadedBuildOrder = await buildOrderRepository.FindBuildOrderByKey(buildOrder.Key, cancellation);

        buildOrder.BuildOrderActions = reloadedBuildOrder?.BuildOrderActions ?? [];
    }
}