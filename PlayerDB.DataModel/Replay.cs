namespace PlayerDB.DataModel;

public class Replay
{
    public int Id { get; set; }
    public string? Hash { get; set; }
    public string? FilePath { get; set; }
    public bool IsCompetitive1V1 { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public List<BuildOrder>? BuildOrders { get; set; }
    public int ParserVersion { get; set; }
}