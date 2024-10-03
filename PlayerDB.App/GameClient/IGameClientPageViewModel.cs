using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PlayerDB.App.Util;
using PlayerDB.DataModel;

namespace PlayerDB.App.GameClient;

public record PlayerMatchItem(
    int PlayerId,
    string ClanName,
    string Name,
    string Toon,
    int? Mmr,
    HashSet<StarCraftRace> BuildOrderRaces, 
    StarCraftRace? PlayerRace,
    StarCraftRace? OpponentRace)
{
    public bool IsPlayerTerran => PlayerRace == StarCraftRace.Terran;
    public bool IsPlayerProtoss => PlayerRace == StarCraftRace.Protoss;
    public bool IsPlayerZerg => PlayerRace == StarCraftRace.Zerg;
}

public interface IGameClientPageViewModel : IActivatableViewModel
{
    bool IsConnectedToGameClient { get; set; }

    ObservableCollection<PlayerMatchItem> Matches { get; }

    PlayerMatchItem? SelectedMatch { get; set; }

    int? PlayerFilterRecentSecs { get; set; }

    IPlayerDetailsViewModel PlayerDetailsViewModel { get; }

    bool IsWaitingForPlayer { get; }

    Task<Player?> LoadPlayerDetails(int playerId);

    Task OnPlayerFilterRecentSecsChanged();
}