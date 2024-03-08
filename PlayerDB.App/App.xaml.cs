using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;
using PlayerDB.DataModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PlayerDB.App;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    public static MainWindow? MainWindow { get; private set; }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainWindow = CreateAndActivateMainWindow();

        var dbPath = ApplicationData.Current.LocalCacheFolder.Path + "\\PlayerDB.db";

        var appContainer = AppContainer.CreateAppContainer(dbPath);

        await Task.WhenAll(InitialiseApp(appContainer), Task.Delay(1500));
        
        await SetUpMainWindow(mainWindow, appContainer);

        await SetUpAppLifeCycle(mainWindow, appContainer);
    }

    private async Task InitialiseApp(AppContainer appContainer)
    {
        var settings = await appContainer.SettingsService.GetCurrentSettings();

        try
        {
            if (settings.ScanReplayFoldersOnAppStart ?? DefaultSettings.ScanReplayFoldersOnAppStart)
                await appContainer.ReplayManager.SearchForAndLoadReplays(
                    settings.ReplayFolderPaths ?? DefaultSettings.ReplayFolderPaths);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Unexpected error scanning replay folders on app start");
            Debug.WriteLine(ex.ToString());
        }

        try
        {
            appContainer.ReplayWatcherController.Start(settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Unexpected error starting replay watcher");
            Debug.WriteLine(ex.ToString());
        }
    }

    private static async Task SetUpAppLifeCycle(MainWindow mainWindow, AppContainer appContainer)
    {
        var monitor = new object();
        Monitor.Enter(monitor);
        var taskCompletionSource = new TaskCompletionSource();
        try
        {
            mainWindow.Closed += (_, _) =>
            {
                // Create foreground thread to keep app alive after the main window is closed until shutdown has completed
                new Thread(() =>
                    {
                        while (!Monitor.TryEnter(monitor)) Thread.Sleep(500);
                        Debug.WriteLine("exiting app");
                    })
                    { IsBackground = false }.Start();

                taskCompletionSource.SetResult();
            };

            await taskCompletionSource.Task;
        }
        finally
        {
            try
            {
                await appContainer.ShutDown();
            }
            catch (TaskCanceledException)
            {
                // ignore, we expect things to get cancelled during shutdown
            }
            catch (Exception ex)
            {
                // TODO: This will cause aggregate exceptions to get swallowed
                Debug.WriteLine(ex.ToString());
            }

            appContainer.Dispose();

            Monitor.Exit(monitor);
        }
    }

    private static Task SetUpMainWindow(MainWindow mainWindow, AppContainer appContainer)
    {
        return mainWindow.NavigateToMainPage(appContainer.MainPageViewModel);
    }

    private static MainWindow CreateAndActivateMainWindow()
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
        return MainWindow;
    }
}