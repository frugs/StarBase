namespace PlayerDB.Replay;

public record ParseResult(DataModel.Replay Replay, IList<DataModel.Player> Players);

public interface IReplayParser
{
    public enum Mode
    {
        Full = 0,
        Fast
    }

    int ParserVersion { get; }

    Task<ParseResult?> ParseReplay(string replayPath, Mode mode = Mode.Full);

    IAsyncEnumerable<ParseResult> ParseReplaysParallel(IList<string> replayPaths, Mode mode = Mode.Full);
}