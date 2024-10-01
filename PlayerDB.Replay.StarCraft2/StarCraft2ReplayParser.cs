using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using PlayerDB.DataModel;
using s2protocol.NET;
using s2protocol.NET.Models;

namespace PlayerDB.Replay.StarCraft2;

public class StarCraft2ReplayParser : IReplayParser
{
    private const int LatestParserVersion = 4;

    private static readonly string? AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private static readonly string[] IgnoredUnitTypes =
    [
        "SCV",
        "SupplyDepot",
        "Drone",
        "Overlord",
        "Probe",
        "Pylon",
        "MULE",
        "Larva",
        "ChangelingMarine",
        "ChangelingZergling",
        "ChangelingZealot",
        "Changeling",
        "Broodling",
        "Locust",
        "CreepTumor",
        "CreepTumorQueen",
        "AdeptPhaseShift",
        "KD8Charge",
        "Interceptor"
    ];

    private readonly ReplayDecoder _decoder = new(AssemblyPath ?? "");

    private readonly ReplayDecoderOptions _fastDecoderOptions = new()
    {
        Details = true,
        TrackerEvents = false,
        Metadata = true,
        MessageEvents = false,
        GameEvents = false,
        AttributeEvents = false
    };

    private readonly ReplayDecoderOptions _fullDecoderOptions = new()
    {
        Details = true,
        TrackerEvents = true,
        Metadata = true,
        MessageEvents = false,
        GameEvents = false,
        AttributeEvents = false
    };

    public int ParserVersion => LatestParserVersion;

    public async Task<ParseResult?> ParseReplay(
        string replayPath,
        IReplayParser.Mode mode = IReplayParser.Mode.Full)
    {
        Sc2Replay? replay;
        try
        {
            replay = await _decoder.DecodeAsync(replayPath, SelectDecoderOptions(mode));
        }
        catch
        {
            Debug.WriteLine("Failed to parse replay at " + replayPath);
            replay = null;
        }

        if (replay == null) return null;

        return ExtractData(replay, mode);
    }

    public IAsyncEnumerable<ParseResult> ParseReplaysParallel(
        IList<string> replayPaths,
        IReplayParser.Mode mode = IReplayParser.Mode.Full)
    {
        return _decoder.DecodeParallel(replayPaths,
                Environment.ProcessorCount * 3,
                SelectDecoderOptions(mode))
            .Select(x => (Sc2Replay?)x)
            .Select(x => x is not null ? ExtractData(x, mode) : null)
            .Where(x => x is not null)
            .Select(x => x!);
    }

    private ReplayDecoderOptions SelectDecoderOptions(IReplayParser.Mode mode)
    {
        return mode switch
        {
            IReplayParser.Mode.Full => _fullDecoderOptions,
            IReplayParser.Mode.Fast => _fastDecoderOptions,
            _ => _fullDecoderOptions
        };
    }

    private ParseResult? ExtractData(Sc2Replay replay, IReplayParser.Mode mode)
    {
        var replayStartTimeUtc = replay.Details?.DateTimeUTC ?? default;
        if (replayStartTimeUtc == default) return null;

        var extractedReplayData = new DataModel.Replay
        {
            StartTimeUtc = replayStartTimeUtc,
            FilePath = replay.FileName,
            ParserVersion = ParserVersion
        };

        var players = replay.Metadata?.Players.ToList() ?? [];

        if (players.Count == 0) return new ParseResult(extractedReplayData, []);

        var playerDetails = replay.Details?.Players ?? [];

        if (players.Count != playerDetails.Count) return new ParseResult(extractedReplayData, []);

        var trackerEvents = new List<TrackerEvent>()
            .Concat(replay.TrackerEvents?.SUnitInitEvents ?? [])
            .Concat(replay.TrackerEvents?.SUnitBornEvents ?? [])
            .Concat(replay.TrackerEvents?.SPlayerStatsEvents ?? [])
            .OrderBy(x => x.Gameloop)
            .ToList();

        var buildOrders = players
            .Select(x => x.PlayerID)
            .Select(
                playerId => (playerId, trackerEvents
                    .Where(x => x.Gameloop > 0)
                    .Where(x => x switch
                    {
                        SUnitBornEvent ev => !IgnoredUnitTypes.Contains(ev.UnitTypeName),
                        SUnitInitEvent ev => !IgnoredUnitTypes.Contains(ev.UnitTypeName),
                        _ => true
                    })
                    .Where(x => x switch
                    {
                        SUnitBornEvent ev => ev.ControlPlayerId == playerId,
                        SUnitInitEvent ev => ev.ControlPlayerId == playerId,
                        SPlayerStatsEvent ev => ev.PlayerId == playerId,
                        _ => false
                    })
                    .Aggregate(
                        new List<(int used, int made, string unitTypeName, int unitCount)> { (0, 0, "", 0) },
                        (acc, trackerEvent) =>
                        {
                            var (used, made, unitTypeName, unitCount) = acc.Last();

                            return trackerEvent switch
                            {
                                SUnitBornEvent ev when !string.IsNullOrEmpty(unitTypeName) &&
                                                       ev.UnitTypeName == unitTypeName =>
                                    acc[..^1]
                                        .Append((used, made, ev.UnitTypeName, unitCount + 1))
                                        .ToList(),
                                SUnitInitEvent ev when !string.IsNullOrEmpty(unitTypeName) &&
                                                       ev.UnitTypeName == unitTypeName =>
                                    acc[..^1]
                                        .Append((used, made, ev.UnitTypeName, unitCount + 1))
                                        .ToList(),
                                SUnitBornEvent ev =>
                                    acc.Append((used, made, ev.UnitTypeName, 1)).ToList(),
                                SUnitInitEvent ev =>
                                    acc.Append((used, made, ev.UnitTypeName, 1)).ToList(),
                                SPlayerStatsEvent ev =>
                                    acc.Append((ev.FoodUsed, ev.FoodMade, "", 0)).ToList(),
                                _ => acc
                            };
                        },
                        acc => acc.Where(x => !string.IsNullOrEmpty(x.unitTypeName)).ToList())
                    .Select(x => $"{x.used >> 12}/{x.made >> 12} {x.unitTypeName}{x.unitCount switch
                    {
                        > 1 => $" x{x.unitCount}",
                        _ => ""
                    }}")
                    .ToList()))
            .ToDictionary();

        var extractedPlayerData = playerDetails.Select((Func<DetailsPlayer, int, Player>)((x, i) =>
        {
            var playerRace = players[i].AssignedRace.ToStarCraftRace();
            int? playerMmr = replay.Initdata?.UserInitialData is { } userInitialData &&
                    userInitialData.ElementAtOrDefault(i)?.ScaledRating is { } mmr and > 0 and < 10000
                        ? (int)mmr
                        : null;
            List<BuildOrder> playerBuildOrders =
            [
                new BuildOrder
                {
                    Key = GenerateBuildOrderKey(replayStartTimeUtc, replay.FileName, x.Toon),
                    ReplayStartTimeUtc = replayStartTimeUtc,
                    PlayerMmr = playerMmr,
                    PlayerRace = playerRace,
                    OpponentRace = (OpponentOf(players[i])?.AssignedRace).ToStarCraftRace(),
                    BuildOrderActions = mode switch
                    {
                        IReplayParser.Mode.Fast => null,
                        IReplayParser.Mode.Full =>
                            CollectionExtensions.GetValueOrDefault<int, List<string>>(buildOrders, players[i].PlayerID) is { } buildOrder and not []
                                ? buildOrder
                                : BuildOrder.InvalidBuildOrderActions,
                        _ => null
                    }
                }
            ];

            var result = new Player
            {
                Name = x.Name,
                ClanName = x.ClanName,
                Toon = AsString(x.Toon),
                BuildOrdersLastUpdatedUtc = replayStartTimeUtc,
                BuildOrders = playerBuildOrders
            };

            switch (playerRace)
            {
                case StarCraftRace.Terran:
                    result.MmrLastUpdatedUtcT = replayStartTimeUtc;
                    result.MostRecentMmrT = playerMmr;
                    break;
                case StarCraftRace.Protoss:
                    result.MmrLastUpdatedUtcP = replayStartTimeUtc;
                    result.MostRecentMmrP = playerMmr;
                    break;
                case StarCraftRace.Zerg:
                    result.MmrLastUpdatedUtcZ = replayStartTimeUtc;
                    result.MostRecentMmrZ = playerMmr;
                    break;
            }

            return result;
        })).ToList();

        return new ParseResult(extractedReplayData, extractedPlayerData);

        MetadataPlayer? OpponentOf(MetadataPlayer? player)
        {
            return players.FirstOrDefault(x => x != player);
        }
    }

    private static string AsString(Toon toon)
    {
        return $"{toon.Region}-S2-{toon.Realm}-{toon.Id}";
    }

    private static string GenerateBuildOrderKey(DateTime replayStartTimeUtc, string replayPath, Toon toon)
    {
        return
            $"{replayStartTimeUtc.ToUniversalTime().ToString(CultureInfo.InvariantCulture)}::{replayPath}::{AsString(toon)}";
    }
}