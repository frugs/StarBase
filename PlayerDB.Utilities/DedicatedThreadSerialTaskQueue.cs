using System.Collections.Concurrent;
using PlayerDB.LifeCycle;

namespace PlayerDB.Utilities;

public sealed class DedicatedThreadSynchronousWorkQueue(string workQueueName = nameof(DedicatedThreadSynchronousWorkQueue)) : IShutDown, IDisposable
{
    private readonly object _lock = new();
    private readonly BlockingCollection<Action> _queue = [];
    private readonly CancellationTokenSource _queueCts = new();
    private bool _isStarted;

    private Thread? _worker;

    public void Dispose()
    {
        _queue.Dispose();
        _queueCts.Dispose();
    }

    public Task ShutDown()
    {
        lock (_lock)
        {
            if (!_isStarted) return Task.CompletedTask;
            if (_worker == null) return Task.CompletedTask;

            _isStarted = false;
            var worker = _worker!;
            _worker = null;

            _queue.CompleteAdding();

            try
            {
                _queueCts.Cancel();
            }
            catch
            {
                // ignore, shutting down
            }

            return Task.Run(worker.Join);
        }
    }


    public Task<T> Enqueue<T>(Func<CancellationToken, T> work, CancellationToken cancellation = default)
    {
        var tcs = new TaskCompletionSource<T>();
        var success = _queue.TryAdd(() =>
        {
            using var linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(_queueCts.Token, cancellation);

            try
            {
                var result = work(linkedCancellation.Token);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException ex)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        if (!success) tcs.TrySetCanceled(CancellationToken.None);

        return tcs.Task;
    }

    public Task<T> Enqueue<T>(Func<T> work, CancellationToken cancellation = default)
    {
        return Enqueue(_ => work(), cancellation);
    }

    public Task Enqueue(Action<CancellationToken> work, CancellationToken cancellation = default)
    {
        return Enqueue(_ =>
        {
            work(cancellation);
            return true;
        }, cancellation);
    }

    public Task Enqueue(Action work, CancellationToken cancellation = default)
    {
        return Enqueue(_ => work(), cancellation);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_queue.IsAddingCompleted) throw new InvalidOperationException("Queue has been shut down");
            if (_isStarted) return;

            _isStarted = true;
            _worker = new Thread(ThreadProc) {Name = workQueueName };
        }

        _worker.Start();
    }

    private void ThreadProc()
    {
        while (_queue.TryTake(out var item, -1)) item();
    }
}