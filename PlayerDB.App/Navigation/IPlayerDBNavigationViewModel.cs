using System.ComponentModel;
using PlayerDB.App.GameClient;
using PlayerDB.App.Replays;
using PlayerDB.App.Settings;

namespace PlayerDB.App.Navigation;

public interface IPlayerDBNavigationViewModel : INotifyPropertyChanged
{
    IGameClientPageViewModel GameClientPageViewModel { get; }
    
    IReplaysPageViewModel ReplaysPageViewModel { get; }

    ISettingsPageViewModel SettingsPageViewModel { get; }
}