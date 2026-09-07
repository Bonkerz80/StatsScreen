using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using StatsScreen.Models;
using StatsScreen.Services.Display;
using StatsScreen.Services.Logging;
using StatsScreen.Services.Polling;
using StatsScreen.Services.Settings;
using StatsScreen.ViewModels;

namespace StatsScreen.Views;

public partial class MainWindow : Window
{
    private static readonly int[] PollingIntervalPresets = [500, 1000, 2000, 5000];

    private readonly SettingsService _settingsService;
    private readonly DisplayService _displayService;
    private readonly SensorPollingService _pollingService;
    private readonly IAppLogger _logger;
    private readonly ContextMenu _contextMenu;
    private AppSettings _settings;
    private Rect? _normalWindowBounds;
    private bool _isDisplayMode;

    public MainWindow(
        AppSettings settings,
        SettingsService settingsService,
        DisplayService displayService,
        SensorPollingService pollingService,
        IAppLogger logger)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;
        _displayService = displayService;
        _pollingService = pollingService;
        _logger = logger;
        _contextMenu = (ContextMenu)Resources["DashboardContextMenu"];

        ViewModel = new DashboardViewModel();
        DataContext = ViewModel;
        _pollingService.SnapshotAvailable += PollingService_OnSnapshotAvailable;
    }

    public DashboardViewModel ViewModel { get; }

    public void ApplyStartupDisplayMode()
    {
        if (!_settings.StartInDisplayMode)
            return;

        Dispatcher.BeginInvoke(new Action(EnterDisplayMode));
    }

    private void PollingService_OnSnapshotAvailable(object? sender, DashboardSnapshot snapshot)
    {
        if (Dispatcher.HasShutdownStarted)
            return;

        _ = Dispatcher.BeginInvoke(new Action(() => { if (IsLoaded) ViewModel.Apply(snapshot); }));
    }

    private void Window_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleDisplayMode();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isDisplayMode)
        {
            ExitDisplayMode();
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            OpenSettings();
            e.Handled = true;
        }
    }

    private void Window_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right)
            return;

        OpenContextMenu();
        e.Handled = true;
    }

    private void OpenContextMenu()
    {
        if (_contextMenu.IsOpen)
            return;

        BuildContextMenu();
        _contextMenu.PlacementTarget = this;
        _contextMenu.Placement = PlacementMode.MousePoint;
        _contextMenu.IsOpen = true;
    }

    private void BuildContextMenu()
    {
        _contextMenu.Items.Clear();

        _contextMenu.Items.Add(CreateMenuItem(
            _isDisplayMode ? "Exit Fullscreen" : "Enter Fullscreen",
            (_, _) => ToggleDisplayMode()));

        var monitorMenu = CreateMenuItem("Monitor");
        monitorMenu.ItemContainerStyle = FindResource("StatsMenuItemStyle") as Style;
        IReadOnlyList<DisplayInfo> displays = _displayService.GetDisplays();
        DisplayInfo selectedDisplay = _displayService.SelectDisplay(_settings.MonitorDeviceName);
        foreach (DisplayInfo display in displays)
        {
            MenuItem monitorItem = CreateMenuItem(
                display.MenuLabel,
                (_, _) => SelectMonitor(display));
            monitorItem.IsCheckable = true;
            monitorItem.IsChecked = string.Equals(
                selectedDisplay.DeviceName,
                display.DeviceName,
                StringComparison.OrdinalIgnoreCase);
            monitorMenu.Items.Add(monitorItem);
        }

        if (displays.Count == 0)
        {
            MenuItem unavailableItem = CreateMenuItem("No displays detected");
            unavailableItem.IsEnabled = false;
            monitorMenu.Items.Add(unavailableItem);
        }

        _contextMenu.Items.Add(monitorMenu);

        var startupItem = CreateMenuItem("Start in dedicated display mode");
        startupItem.IsCheckable = true;
        startupItem.IsChecked = _settings.StartInDisplayMode;
        startupItem.Click += (_, _) => SetStartInDisplayMode(startupItem.IsChecked);
        _contextMenu.Items.Add(startupItem);

        var pollingMenu = CreateMenuItem("Polling Interval");
        pollingMenu.ItemContainerStyle = FindResource("StatsMenuItemStyle") as Style;
        foreach (int interval in GetPollingIntervals())
        {
            MenuItem intervalItem = CreateMenuItem(
                $"{interval} ms",
                (_, _) => SetPollingInterval(interval));
            intervalItem.IsCheckable = true;
            intervalItem.IsChecked = interval == _settings.PollIntervalMilliseconds;
            pollingMenu.Items.Add(intervalItem);
        }

        _contextMenu.Items.Add(pollingMenu);
        _contextMenu.Items.Add(CreateMenuItem("Settings...", (_, _) => OpenSettings()));
        _contextMenu.Items.Add(CreateMenuItem("Open Diagnostic Log Folder", (_, _) => OpenDiagnosticLogFolder()));
        _contextMenu.Items.Add(new Separator
        {
            Style = FindResource("StatsMenuSeparatorStyle") as Style
        });
        _contextMenu.Items.Add(CreateMenuItem("Exit StatsScreen", (_, _) => Close()));
    }

    private MenuItem CreateMenuItem(string header, RoutedEventHandler? handler = null)
    {
        var item = new MenuItem
        {
            Header = header,
            Style = FindResource("StatsMenuItemStyle") as Style
        };

        if (handler is not null)
            item.Click += handler;

        return item;
    }

    private IReadOnlyList<int> GetPollingIntervals()
    {
        return PollingIntervalPresets
            .Append(_settings.PollIntervalMilliseconds)
            .Distinct()
            .OrderBy(interval => interval)
            .ToArray();
    }

    private void SelectMonitor(DisplayInfo display)
    {
        _settings.MonitorDeviceName = display.DeviceName;
        _settingsService.Save(_settings);

        if (_isDisplayMode)
            _displayService.MoveDisplayMode(this, display);

        _logger.Info($"Selected display from context menu: {display.DisplayLabel}.");
    }

    private void SetStartInDisplayMode(bool enabled)
    {
        _settings.StartInDisplayMode = enabled;
        _settingsService.Save(_settings);
        _logger.Info($"Start in dedicated display mode set to {enabled}.");
    }

    private void SetPollingInterval(int intervalMilliseconds)
    {
        _settings.PollIntervalMilliseconds = intervalMilliseconds;
        _settings.Normalize();
        _settingsService.Save(_settings);
        _pollingService.Restart(_settings.PollIntervalMilliseconds);
        _logger.Info($"Polling interval set to {_settings.PollIntervalMilliseconds} ms.");
    }

    private void OpenDiagnosticLogFolder()
    {
        string? directory = Path.GetDirectoryName(_logger.LogFilePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _logger.Error($"Unable to open diagnostic log folder '{directory}'.", exception);
        }
    }

    private void ToggleDisplayMode()
    {
        if (_isDisplayMode)
            ExitDisplayMode();
        else
            EnterDisplayMode();
    }

    private void EnterDisplayMode()
    {
        if (_isDisplayMode)
            return;

        WindowState = WindowState.Normal;
        _normalWindowBounds = new Rect(Left, Top, Width, Height);

        DisplayInfo display = _displayService.SelectDisplay(_settings.MonitorDeviceName);
        _settings.MonitorDeviceName = display.DeviceName;
        _settingsService.Save(_settings);
        _displayService.EnterDisplayMode(this, display);
        _isDisplayMode = true;
        _logger.Info($"Entered dedicated display mode on {display.DisplayLabel}.");
    }

    private void ExitDisplayMode()
    {
        if (!_isDisplayMode)
            return;

        _displayService.ExitDisplayMode(this);
        _isDisplayMode = false;

        if (_normalWindowBounds is { } bounds)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
        }

        _logger.Info("Exited dedicated display mode.");
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_displayService.GetDisplays(), _settings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
            return;

        bool wasDisplayMode = _isDisplayMode;
        if (wasDisplayMode)
            ExitDisplayMode();

        _settings = dialog.Settings;
        _settingsService.Save(_settings);
        _pollingService.Restart(_settings.PollIntervalMilliseconds);

        if (wasDisplayMode)
            EnterDisplayMode();
    }
}
