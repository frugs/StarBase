using PlayerDB.DataModel;

namespace PlayerDB.App.Util;

public static class StarCraftRaceExtensions
{
    public static string ToMatchUpString(this (StarCraftRace PlayerRace, StarCraftRace OpponentRace) races)
    {
        return $"{races.PlayerRace.ToInitialString()}v{races.OpponentRace.ToInitialString()}";
    }

    public static string ToInitialString(this StarCraftRace race)
    {
        return race switch
        {
            StarCraftRace.Terran => "T",
            StarCraftRace.Protoss => "P",
            StarCraftRace.Zerg => "Z",
            StarCraftRace.Random => "R",
            StarCraftRace.Unknown or _ => "?"
        };
    }
}