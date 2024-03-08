namespace PlayerDB.Core.FileSystem;

public interface IReplayWatcherController
{
    void Start(DataModel.Settings initialSettings);
}