using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace PlayerDB.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
        AppWindow.SetIcon("Assets/logo3_padded.ico");
    }

    public async Task NavigateToMainPage(IMainPageViewModel mainPageViewModel)
    {
        await SplashScreenPage.HideLogo();
        MainWindowFrame.Navigate(typeof(MainPage), mainPageViewModel);
    }
}