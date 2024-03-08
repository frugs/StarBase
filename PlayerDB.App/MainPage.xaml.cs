using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PlayerDB.App;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private IMainPageViewModel? _viewModel;

    public MainPage()
    {
        InitializeComponent();

        App.MainWindow?.SetTitleBar(TitleBar);
    }

    public IMainPageViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;

            _viewModel = value;

            Bindings.Update();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IMainPageViewModel viewModel) ViewModel = viewModel;
    }
}