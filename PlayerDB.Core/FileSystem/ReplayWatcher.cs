using System.Diagnostics;
using PlayerDB.Core.Replay;
using PlayerDB.LifeCycle;
using PlayerDB.Utilities;

namespace PlayerDB.Core.FileSystem;

public sealed class ReplayWatcher(
    string replayFileExtension,
    IReplayManager replayManager)
    : IReplayWatcher, IDisposable, IShutDown
{
    private readonly object _lock = new();
    private readonly List<string> _pathsToWatch = [];
    private readonly DedicatedThreadSynchronousWorkQueue _queue = new($"{nameof(ReplayWatcher)}WorkQueue");
    private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

    private bool _enabled;
    private bool _shutDown;

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values) watcher.Dispose();

        _queue.Dispose();
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_shutDown) throw new InvalidOperationException("ReplayWatcher is shutting down");
        }

        _queue.Start();
    }

    public void Enable()
    {
        lock (_lock)
        {
            if (_enabled) return;
            if (_shutDown) throw new InvalidOperationException("ReplayWatcher is shutting down");

            _enabled = true;

            foreach (var path in _pathsToWatch)
            {
                if (_watchers.ContainsKey(path)) continue;

                var watcher = CreateWatcher(path);
                SetUpWatcher(watcher);
                _watchers[path] = watcher;
            }
        }
    }

    public void Disable()
    {
        lock (_lock)
        {
            if (!_enabled) return;
            if (_shutDown) throw new InvalidOperationException("ReplayWatcher is shutting down");

            _enabled = false;

            foreach (var watcher in _watchers.Values) TearDownWatcher(watcher);
            _watchers.Clear();
        }
    }

    public Task ShutDown()
    {
        lock (_lock)
        {
            _shutDown = true;
        }

        return _queue.ShutDown();
    }

    public void SetWatchedPaths(IReadOnlyCollection<string> newPaths)
    {
        lock (_lock)
        {
            if (_shutDown) throw new InvalidOperationException("ReplayWatcher is shutting down");

            foreach (var path in newPaths)
            {
                if (_pathsToWatch.Contains(path)) continue;
                _pathsToWatch.Add(path);

                if (!_enabled) continue;
                if (_watchers.ContainsKey(path)) continue;

                var watcher = CreateWatcher(path);
                SetUpWatcher(watcher);
                _watchers[path] = watcher;
            }

            foreach (var watchedPath in _pathsToWatch
                         .ToList()
                         .Where(watchedPath => !newPaths.Contains(watchedPath)))
            {
                _pathsToWatch.Remove(watchedPath);

                if (!_watchers.Remove(watchedPath, out var watcher)) continue;
                TearDownWatcher(watcher);
            }
        }
    }

    private void SetUpWatcher(FileSystemWatcher watcher)
    {
        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Renamed += OnRenamed;

        watcher.EnableRaisingEvents = true;
    }

    private async void OnRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            await _queue.Enqueue(cancellation =>
            {
                // TODO: Do something with old replay if it exists

                replayManager.LoadReplay(e.FullPath, cancellation);
            });
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error in replay watcher OnCreated callback");
            Debug.WriteLine($"file: {e.FullPath}, exception:");
            Debug.WriteLine(ex.ToString());
        }
    }

    private async void OnCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            await _queue.Enqueue(cancellation => { replayManager.LoadReplay(e.FullPath, cancellation); });
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error in replay watcher OnCreated callback");
            Debug.WriteLine($"file: {e.FullPath}, exception:");
            Debug.WriteLine(ex.ToString());
        }
    }

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            await _queue.Enqueue(cancellation =>
            {
                // TODO: Remove old replay and build orders, then load changed replay

                replayManager.LoadReplay(e.FullPath, cancellation);
            });
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error in replay watcher OnCreated callback");
            Debug.WriteLine($"file: {e.FullPath}, exception:");
            Debug.WriteLine(ex.ToString());
        }
    }

    private FileSystemWatcher CreateWatcher(string path)
    {
        return new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            Filter = $"*.{replayFileExtension}"
        };
    }

    private void TearDownWatcher(FileSystemWatcher watcher)
    {
        watcher.EnableRaisingEvents = false;

        watcher.Changed -= OnChanged;
        watcher.Created -= OnCreated;
        watcher.Renamed -= OnRenamed;

        watcher.Dispose();
    }
}