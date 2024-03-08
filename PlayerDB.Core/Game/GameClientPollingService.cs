using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PlayerDB.Game;
using PlayerDB.LifeCycle;

namespace PlayerDB.Core.Game;

public sealed class GameClientPollingService(IGameClient gameClient) : IGameClientPollingService, IShutDown, IDisposable
{
    private readonly Subject<GameData> _gameClientSubject = new();
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly List<Task> _tasks = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isShuttingDown;

    private bool _isStarted;

    public bool IsStarted
    {
        get => _isStarted;
        set
        {
            lock (_lock)
            {
                if (value && _isShuttingDown) return;
                if (_isStarted == value) return;

                _isStarted = value;
                _tasks.RemoveAll(x => x.IsCompleted);

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                if (_isStarted)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _tasks.Add(StartPolling(_cancellationTokenSource.Token));
                }
                else
                {
                    _cancellationTokenSource = null;
                }
            }
        }
    }

    public IObservable<GameData> GameClientObservable =>
        _gameClientSubject.DistinctUntilChanged();

    public Task ShutDown()
    {
        List<Task>? tasks;
        lock (_lock)
        {
            _isShuttingDown = true;
            IsStarted = false;
            tasks = _tasks;
        }

        return Task.WhenAll(tasks);
    }

    private async Task StartPolling(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
            try
            {
                await _semaphore.WaitAsync(cancellation);

                var gameData = await gameClient.ReadGameData(cancellation);

                if (gameData != null) _gameClientSubject.OnNext(gameData);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _semaphore.Release();
                try
                {
                    await Task.Delay(2000, cancellation);
                }
                catch
                {
                    // ignored, finally block
                }
            }
    }

    public void Dispose()
    {
        _gameClientSubject.Dispose();
        _semaphore.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}