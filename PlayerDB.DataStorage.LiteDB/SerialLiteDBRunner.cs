using LiteDB;
using PlayerDB.LifeCycle;
using PlayerDB.Utilities;

namespace PlayerDB.DataStorage.LiteDB;

public sealed class SerialLiteDBRunner(ILiteDatabase liteDB) : ILiteDBRunner, IShutDown, IDisposable
{
    private readonly SerialTaskQueue _taskQueue = new(nameof(SerialLiteDBRunner));

    public Task ShutDown()
    {
        return _taskQueue.ShutDown();
    }

    public Task<T> Perform<T>(Func<ILiteDatabase, T> dbOperation, CancellationToken cancellation = default)
    {
        return _taskQueue.Enqueue(() => Task.Run(() => dbOperation(liteDB), cancellation), cancellation);
    }

    public Task Perform(Action<ILiteDatabase> dbOperation, CancellationToken cancellation = default)
    {
        return _taskQueue.Enqueue(() => Task.Run(() => dbOperation(liteDB), cancellation), cancellation);
    }

    public void Dispose()
    {
        _taskQueue.Dispose();
    }
}