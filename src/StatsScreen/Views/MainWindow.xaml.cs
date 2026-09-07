using System.Windows;
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
    private readonly SettingsService _settingsService;
    private readonly DisplayService _displayService;
    private readonly SensorPollingService _pollingService;
    private readonly IAppLogger _logger;
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
