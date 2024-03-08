namespace PlayerDB.Core.BuildOrder;

public interface IBuildOrderManager
{
    IReadOnlyCollection<DataModel.BuildOrder> RecentValidBuildOrdersForPlayer(DataModel.Player player, int maxBuildOrdersPerMatchUp = 3);

    Task EnsureBuildOrderActions(DataModel.BuildOrder buildOrder, CancellationToken cancellation = default);
}