namespace PlayerDB.DataModel;

public class BuildOrder
{
    public static readonly List<string> InvalidBuildOrderActions = [""];

    public int Id { get; set; }
    public int? PlayerMmr { get; set; }
    public StarCraftRace PlayerRace { get; set; } = StarCraftRace.Unknown;
    public StarCraftRace OpponentRace { get; set; } = StarCraftRace.Unknown;
    public DateTime ReplayStartTimeUtc { get; set; }
    public List<string>? BuildOrderActions { get; set; }
    public Replay? Replay { get; set; }
    public string? Key { get; set; }

    public bool IsValid => BuildOrderActions is null or [] or not [""] and not [null];
}