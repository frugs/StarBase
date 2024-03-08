using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PlayerDB.App.Util;

namespace PlayerDB.App.Replays;

public interface IReplaysPageViewModel : IActivatableViewModel
{
    ObservableCollection<IReplayFolderItemViewModel> ReplayFolders { get; }

    bool IsLoadingReplays { get; }

    int? LoadedReplays { get; }

    int? TotalReplays { get; }

    string? CurrentReplay { get; }

    bool IsProgressIndeterminate { get; }

    double Progress { get; }

    bool ScanFoldersOnAppStart { get; set; }

    bool WatchReplayFolders { get; set; }

    Task AddReplayFolder(string replayFolder);

    Task LoadReplay(string replayFilePath);

    Task SearchForAndLoadReplays();
}

public interface IReplayFolderItemViewModel
{
    string? ReplayFolderFilePath { get; }

    bool IsLoadingReplays { get; }
}