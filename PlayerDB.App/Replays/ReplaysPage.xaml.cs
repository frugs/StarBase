using System;
using System.Linq;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace PlayerDB.App.Replays;

public sealed partial class ReplaysPage : Page
{
    private IReplaysPageViewModel? _viewModel;

    public ReplaysPage()
    {
        InitializeComponent();
    }

    public IReplaysPageViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;
            if (_viewModel != null) _viewModel.IsActive = false;
            if (value != null) value.IsActive = true;

            _viewModel = value;

            Bindings.Update();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IReplaysPageViewModel viewModel) ViewModel = viewModel;
    }

    public static string RenderProgressDescription(int? loadedReplays, int? totalReplays)
    {
        return (loadedReplays, totalReplays) switch
        {
            (null, null) => "Scanning for replays...",
            (null, var total) => $"Discovered {total} replays",
            var (loaded, total) => $"Loaded {loaded} of {total} replays"
        };
    }

    private async void AddSingleReplay_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var filePicker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            FileTypeFilter = { ".Sc2Replay" }
        };

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        if (hwnd == IntPtr.Zero) return;

        InitializeWithWindow.Initialize(filePicker, hwnd);

        var file = await filePicker.PickSingleFileAsync();
        if (file == null) return;

        await ViewModel.LoadReplay(file.Path);
    }

    private async void AddReplayFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var folderPicker = new FolderPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { "*" }
        };

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        if (hwnd == IntPtr.Zero) return;

        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder == null) return;

        if (ViewModel.ReplayFolders is { } replayFolders
            && !replayFolders.Select(x => x.ReplayFolderFilePath).Contains(folder.Path))
            await ViewModel.AddReplayFolder(folder.Path);
    }

    private void RemoveReplayFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.ReplayFolders is { } replayFolders
            && sender is Button { Tag: string tag })
        {
            var toRemove = replayFolders.Where(x => x.ReplayFolderFilePath == tag).ToList();
            foreach (var itemToRemove in toRemove) replayFolders.Remove(itemToRemove);
        }
    }

    private async void ScanAllFolders_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        await ViewModel.SearchForAndLoadReplays();
    }
}