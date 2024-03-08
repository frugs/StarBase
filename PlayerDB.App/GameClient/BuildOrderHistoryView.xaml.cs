using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlayerDB.App.Util;
using PlayerDB.DataModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PlayerDB.App.GameClient;

public sealed partial class BuildOrderHistoryView : UserControl
{
    private IReadOnlyList<BuildOrder> _viewModel = [];

    public BuildOrderHistoryView()
    {
        InitializeComponent();
    }

    public IReadOnlyList<BuildOrder> ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            Bindings.Update();
        }
    }

#pragma warning disable CA1822 // Mark members as static#pragma warning disable CA1822 // Mark members as static
    private Visibility ReplayDetailsVisibility(IReadOnlyList<BuildOrder>? vm, int index)
    {
        return vm is null or [] || index >= vm.Count ? Visibility.Collapsed : Visibility.Visible;
    }

    private Visibility BuildOrderVisibility(IReadOnlyList<BuildOrder>? vm, int index)
    {
        if (vm is null or []) return Visibility.Collapsed;
        if (index >= vm.Count) return Visibility.Collapsed;
        return vm[index].BuildOrderActions is null or [] ? Visibility.Collapsed : Visibility.Visible;
    }

    private Visibility ProgressRingVisibility(IReadOnlyList<BuildOrder>? vm, int index)
    {
        if (vm is null or []) return Visibility.Collapsed;
        if (index >= vm.Count) return Visibility.Collapsed;
        return BuildOrderVisibility(vm, index) == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private string RenderBuildOrder(IReadOnlyList<BuildOrder>? vm, int index)
    {
        if (vm is { Count: var count } buildOrders && count > index)
        {
            var sb = new StringBuilder();
            foreach (var line in buildOrders[index].BuildOrderActions ?? [])
                sb.AppendLine(line);

            return sb.ToString();
        }

        return "";
    }

    public DateTime? RenderReplayStartTime(IReadOnlyList<BuildOrder>? vm, int index)
    {
        return vm is { Count: var count } buildOrders &&
               index < count &&
               buildOrders[index].ReplayStartTimeUtc != default
            ? buildOrders[index].ReplayStartTimeUtc
            : null;
    }

    public string RenderMatchUp(IReadOnlyList<BuildOrder>? vm, int index)
    {
        return vm is { Count: var count } buildOrders &&
               index < count &&
               buildOrders[index].PlayerRace.IsKnownAndNotRandom() &&
               buildOrders[index].OpponentRace.IsKnownAndNotRandom()
            ? (buildOrders[index].PlayerRace, buildOrders[index].OpponentRace).ToMatchUpString()
            : "";
    }

    public string RenderPlayerMmr(IReadOnlyList<BuildOrder>? vm, int index)
    {
        return vm is { Count: var count } buildOrders &&
               index < count &&
               buildOrders[index].PlayerMmr is { } playerMmr
            ? $"MMR - {playerMmr}"
            : "";
    }
#pragma warning restore CA1822 // Mark members as static
}