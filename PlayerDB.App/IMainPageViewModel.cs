using PlayerDB.App.Navigation;

namespace PlayerDB.App;

public interface IMainPageViewModel
{
    public IPlayerDBNavigationViewModel NavigationViewModel { get; }
}