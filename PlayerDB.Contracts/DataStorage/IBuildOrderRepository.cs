namespace PlayerDB.DataStorage;

public interface IBuildOrderRepository
{
    Task<DataModel.BuildOrder?> FindBuildOrderById(int buildOrderId, CancellationToken cancellation = default);

    Task<DataModel.BuildOrder?> FindBuildOrderByKey(string key, CancellationToken cancellation = default);

    Task SaveBuildOrder(DataModel.BuildOrder buildOrder, CancellationToken cancellation = default);

    Task DeleteBuildOrder(DataModel.BuildOrder buildOrder, CancellationToken cancellation = default);

    Task DeleteBuildOrders(IReadOnlyCollection<DataModel.BuildOrder> buildOrders, CancellationToken cancellation = default);

    Task DeleteBuildOrdersForReplay(DataModel.Replay existingReplay, CancellationToken cancellation = default);
    
    Task DeleteAllBuildOrders();
}