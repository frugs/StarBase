namespace PlayerDB.Core.FileSystem;

public interface IReplayWatcher
{
    void Start();
    void Enable();
    void Disable();
    void SetWatchedPaths(IReadOnlyCollection<string> paths);
}