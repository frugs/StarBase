using CommunityToolkit.Mvvm.ComponentModel;
using PlayerDB.App.GameClient;
using PlayerDB.App.Replays;
using PlayerDB.App.Settings;

namespace PlayerDB.App.Navigation;

public class PlayerDBNavigationViewModel(
    IGameClientPageViewModel gameClientPageViewModel,
    IReplaysPageViewModel replaysPageViewModel,
    ISettingsPageViewModel settingsPageViewModel) : ObservableObject, IPlayerDBNavigationViewModel
{
    public IGameClientPageViewModel GameClientPageViewModel => gameClientPageViewModel;

    public IReplaysPageViewModel ReplaysPageViewModel => replaysPageViewModel;

    public ISettingsPageViewModel SettingsPageViewModel => settingsPageViewModel;
}