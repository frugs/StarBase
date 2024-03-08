using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PlayerDB.App;

public sealed partial class SplashScreenPage : Page
{
    public SplashScreenPage()
    {
        InitializeComponent();
    }

    public Task HideLogo()
    {
        SplashScreenLogo.Visibility = Visibility.Collapsed;
        return Task.Delay(500);
    }

    private void SplashScreenPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SplashScreenLogo.Visibility = Visibility.Visible;
    }
}