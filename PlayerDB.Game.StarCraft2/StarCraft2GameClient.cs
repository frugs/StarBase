using System.Text.Json;
using PlayerDB.DataModel;

namespace PlayerDB.Game.StarCraft2;

public class StarCraft2GameClient(HttpClient httpClient) : IGameClient
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<GameData?> ReadGameData(CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();

            var httpResponse = await httpClient.GetAsync("http://localhost:6119/game", cancellation);

            httpResponse.EnsureSuccessStatusCode();

            var responseContentStream = await httpResponse.Content.ReadAsStreamAsync(cancellation);

            var gameResponse = await JsonSerializer.DeserializeAsync<GameResponse>(
                responseContentStream,
                _jsonSerializerOptions,
                cancellation);

            return gameResponse?.Players?
                .Where(x => x.Type == "user" && !string.IsNullOrEmpty(x.Name))
                .Select(x => new GamePlayerData(x.Name!, x.Race.ToStarCraftRace()))
                .Aggregate(
                    (IEnumerable<GamePlayerData>) [],
                    (acc, player) => acc.Append(player),
                    acc => new GameData(acc.ToList()));
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // Maybe log?
        }
        return null;
    }

    public class GameResponsePlayer
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Race { get; set; }
    }

    public class GameResponse
    {
        public IList<GameResponsePlayer>? Players { get; set; }
    }
}