namespace PlayerDB.DataModel;

public enum StarCraftRace
{
    Unknown = 0,
    Terran,
    Protoss,
    Zerg,
    Random
}

public static class StarCraftRaceExtensions
{
    public static StarCraftRace ToStarCraftRace(this string? str)
    {
        return str?.ToUpperInvariant() switch
        {
            "TERRAN" or "TERR" => StarCraftRace.Terran,
            "PROTOSS" or "PROT" => StarCraftRace.Protoss,
            "ZERG" => StarCraftRace.Zerg,
            "RANDOM" => StarCraftRace.Random,
            _ => StarCraftRace.Unknown
        };
    }

    public static bool IsKnownAndNotRandom(this StarCraftRace? race)
    {
        return race switch
        {
            null => false,
            _ => ((StarCraftRace)race).IsKnownAndNotRandom()
        };
    }

    public static bool IsKnownAndNotRandom(this StarCraftRace race)
    {
        return race switch
        {
            StarCraftRace.Unknown or StarCraftRace.Random => false,
            _ => true
        };
    }
}