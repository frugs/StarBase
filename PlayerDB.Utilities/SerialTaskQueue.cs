using PlayerDB.LifeCycle;

namespace PlayerDB.Utilities;

public sealed class SerialTaskQueue(string queueName = "") : ISerialTaskQueue, IDisposable, IShutDown
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly List<WeakReference<Task>> _tasks = [];

    private bool _isShuttingDown;

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public Task ShutDown()
    {
        lock (_tasks)
        {
            _isShuttingDown = true;
            var tasks = _tasks.SelectMany(x => x.TryGetTarget(out var target) ? new[] { target } : []);
            _tasks.Clear();
            return Task.WhenAll(tasks);
        }
    }

    public Task<T> Enqueue<T>(Func<Task<T>> taskProvider, CancellationToken cancellation = default)
    {
        lock (_tasks)
        {
            cancellation.ThrowIfCancellationRequested();

            if (_isShuttingDown)
                throw new InvalidOperationException(
                    $"{(string.IsNullOrEmpty(queueName) ? "task queue" : "task queue for " + queueName)} is shutting down");

            var task = PerformTaskOnQueue(taskProvider, cancellation);
            _tasks.Add(new WeakReference<Task>(task));
            return task;
        }
    }

    public Task Enqueue(Func<Task> taskProvider, CancellationToken cancellation = default)
    {
        return Enqueue<bool>(async () =>
        {
            await taskProvider();
            return default;
        }, cancellation);
    }

    private async Task<T> PerformTaskOnQueue<T>(Func<Task<T>> taskProvider, CancellationToken cancellation)
    {
        try
        {
            await _semaphore.WaitAsync(cancellation);

            return await taskProvider();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}