using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using PlayerDB.DataModel;

namespace PlayerDB.App.GameClient;

public interface IPlayerDetailsViewModel : INotifyPropertyChanged
{
    bool IsAvailable { get; }
    string ClanName { get; }
    string PlayerName { get; }
    string Toon { get; }
    bool PlayerIsMe { get; set; }
    IReadOnlyCollection<StarCraftRace> PlayerRaces { get; set; }
    IReadOnlyCollection<StarCraftRace> OpponentRaces { get; set; }
    IReadOnlyList<DataModel.BuildOrder> BuildOrders { get; }
    string PlayerNotes { get; set; }

    Task SetPlayer(Player player, StarCraftRace? playerRace = null, StarCraftRace? opponentRace = null);
    Task UpdateBuildOrders();
    Task SavePlayerIsMe();
}