namespace PlayerDB.DataModel;

public class Settings
{
    public IReadOnlyCollection<string>? ReplayFolderPaths { get; set; }

    public bool? ScanReplayFoldersOnAppStart { get; set; }

    public bool? WatchReplayFolders { get; set; }

    public IReadOnlyCollection<string>? PlayerToons { get; set; }

    public long? PlayerFilterRecentSecs { get; set; }
}

public static class DefaultSettings
{
    public static IReadOnlyCollection<string> ReplayFolderPaths { get; } = [];

    public static bool ScanReplayFoldersOnAppStart { get; } = true;

    public static bool WatchReplayFolders { get; } = true;

    public static IReadOnlyCollection<string> PlayerToons { get; } = [];

    public static long PlayerFilterRecentSecs { get; } = (int)TimeSpan.FromDays(180).TotalSeconds;
}