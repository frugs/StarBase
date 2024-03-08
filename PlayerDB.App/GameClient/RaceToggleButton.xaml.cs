using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PlayerDB.App.GameClient;

public sealed partial class RaceToggleButton : UserControl
{
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool?),
        typeof(RaceToggleButton),
        new PropertyMetadata(null)
    );

    public RaceToggleButton()
    {
        InitializeComponent();
    }

    public event EventHandler<RoutedEventArgs>? Click;

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool IsTerran { get; set; } = false;

    public bool IsProtoss { get; set; } = false;

    public bool IsZerg { get; set; } = false;

    private void ToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }
}