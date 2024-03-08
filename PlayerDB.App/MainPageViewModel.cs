using PlayerDB.App.Navigation;

namespace PlayerDB.App;

public class MainPageViewModel(IPlayerDBNavigationViewModel navigationViewModel) : IMainPageViewModel
{
    public IPlayerDBNavigationViewModel NavigationViewModel { get; } = navigationViewModel;
}