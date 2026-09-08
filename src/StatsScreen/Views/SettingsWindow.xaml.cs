using System.Windows;
using System.Windows.Controls;
using StatsScreen.Services.Display;
using StatsScreen.Services.Settings;

namespace StatsScreen.Views;

public partial class SettingsWindow : Window
{
    private readonly IReadOnlyList<DisplayInfo> _displays;

    public SettingsWindow(IReadOnlyList<DisplayInfo> displays, AppSettings currentSettings)
    {
        InitializeComponent();

        _displays = displays;
        Settings = currentSettings.Clone();
        MonitorComboBox.ItemsSource = displays;
        StartInDisplayModeCheckBox.IsChecked = currentSettings.StartInDisplayMode;
        PollingIntervalTextBox.Text = currentSettings.PollIntervalMilliseconds.ToString();

        int selectedIndex = displays
            .Select((display, index) => new { display, index })
            .FirstOrDefault(item => string.Equals(
                item.display.DeviceName,
                currentSettings.MonitorDeviceName,
                StringComparison.OrdinalIgnoreCase))?.index
            ?? displays.ToList().FindIndex(display => display.IsPrimary);

        if (selectedIndex >= 0 && selectedIndex < MonitorComboBox.Items.Count)
            MonitorComboBox.SelectedIndex = selectedIndex;
        else if (MonitorComboBox.Items.Count > 0)
            MonitorComboBox.SelectedIndex = 0;
    }

    public AppSettings Settings { get; private set; } = new();

    private void MonitorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MonitorComboBox.SelectedItem is DisplayInfo display)
        {
            MonitorHintText.Text = display.IsTargetSize
                ? "This is the target 800 × 600 display."
                : $"Current bounds: {display.ResolutionText}. The dashboard will fill the selected display.";
        }
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PollingIntervalTextBox.Text, out int interval))
        {
            System.Windows.MessageBox.Show(
                this,
                "Polling interval must be a whole number of milliseconds.",
                "Invalid settings",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        Settings.MonitorDeviceName = (MonitorComboBox.SelectedItem as DisplayInfo)?.DeviceName;
        Settings.StartInDisplayMode = StartInDisplayModeCheckBox.IsChecked == true;
        Settings.PollIntervalMilliseconds = interval;
        Settings.Normalize();
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
