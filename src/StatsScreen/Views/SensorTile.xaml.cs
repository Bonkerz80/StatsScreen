using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StatsScreen.Views;

public partial class SensorTile : System.Windows.Controls.UserControl
{
    public SensorTile()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SensorTile), new PropertyMetadata(string.Empty));

    public System.Windows.Media.Brush Accent
    {
        get => (System.Windows.Media.Brush)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(
            nameof(Accent),
            typeof(System.Windows.Media.Brush),
            typeof(SensorTile),
            new PropertyMetadata(new SolidColorBrush(System.Windows.Media.Color.FromRgb(73, 196, 199))));
}
