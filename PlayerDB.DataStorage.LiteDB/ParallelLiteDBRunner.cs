using System.Collections.Concurrent;
using LiteDB;
using PlayerDB.LifeCycle;

namespace PlayerDB.DataStorage.LiteDB;

public class ParallelLiteDBRunner(ILiteDatabase liteDB) : ILiteDBRunner, IShutDown
{
    private readonly bool _isShuttingDown = false;
    private readonly ConcurrentQueue<WeakReference<Task>> _inFlightTasks = new();

    public Task<T> Perform<T>(Func<ILiteDatabase, T> dbOperation, CancellationToken cancellation = default)
    {
        if (_isShuttingDown) throw new InvalidOperationException($"{nameof(ParallelLiteDBRunner)} is shutting down");

        var result = Task.Run(() => dbOperation(liteDB), cancellation);
        _inFlightTasks.Enqueue(new WeakReference<Task>(result));
        return result;
    }

    public Task Perform(Action<ILiteDatabase> dbOperation, CancellationToken cancellation = default)
    {
        if (_isShuttingDown) throw new InvalidOperationException($"{nameof(ParallelLiteDBRunner)} is shutting down");

        var result = Task.Run(() => dbOperation(liteDB), cancellation);
        _inFlightTasks.Enqueue(new WeakReference<Task>(result));
        return result;
    }

    public Task ShutDown()
    {
        return Task.WhenAll(_inFlightTasks.SelectMany(x =>
            x.TryGetTarget(out var target) ? new[] { target } : []));
    }
}