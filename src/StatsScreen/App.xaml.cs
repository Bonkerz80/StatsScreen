using System.Windows;
using StatsScreen.Services.Display;
using StatsScreen.Services.Hardware;
using StatsScreen.Services.Logging;
using StatsScreen.Services.Polling;
using StatsScreen.Services.Settings;
using StatsScreen.Views;

namespace StatsScreen;

public partial class App : System.Windows.Application
{
    private FileLogger? _logger;
    private HardwareMonitorService? _hardwareMonitor;
    private SensorPollingService? _pollingService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        bool diagnostic = e.Args.Length == 2 && e.Args[0] == "--diagnose";
        _logger = new FileLogger(diagnostic ? e.Args[1] : null);
        _logger.Info($"Elevated: {new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}; PawnIO: {LibreHardwareMonitor.PawnIo.PawnIo.Version}");
        var settingsService = new SettingsService(_logger);
        AppSettings settings = settingsService.Load();
        var displayService = new DisplayService(_logger);
        _hardwareMonitor = new HardwareMonitorService(_logger, diagnostic);
        _pollingService = new SensorPollingService(_hardwareMonitor, _logger, settings.PollIntervalMilliseconds);

        var window = new MainWindow(
            settings,
            settingsService,
            displayService,
            _pollingService,
            _logger);

        MainWindow = window;
        window.Show();
        if (!diagnostic) window.ApplyStartupDisplayMode();
        if (diagnostic)
        {
            int samples = 0;
            _pollingService.SnapshotAvailable += (_, snapshot) =>
            {
                _logger.Info(System.Text.Json.JsonSerializer.Serialize(snapshot));
                var selected = CpuTemperatureSelection.Apply(snapshot, settings.CpuTemperatureSource);
                _logger.Info($"Dashboard CPU preference={settings.CpuTemperatureSource}; actual={selected.CpuTemperature.Source}; value={selected.CpuTemperature.Value}; Tctl/Tdie={snapshot.TctlTdie?.Value}");
                if (++samples == 8)
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _logger.Info($"UI values: {window.ViewModel.CpuTemperature.ValueText}, {window.ViewModel.GpuTemperature.ValueText}, {window.ViewModel.CpuPower.ValueText}, {window.ViewModel.GpuPower.ValueText}");
                        window.UpdateLayout();
                        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        bitmap.Render(window);
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                        using (var stream = System.IO.File.Create(System.IO.Path.Combine(e.Args[1], "dashboard.png"))) encoder.Save(stream);
                        Shutdown();
                    }));
            };
        }
        _pollingService.Start();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _pollingService?.Dispose();
        _hardwareMonitor?.Dispose();
        _logger?.Dispose();
        base.OnExit(e);
    }
}
