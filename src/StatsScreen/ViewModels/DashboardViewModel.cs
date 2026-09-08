using System.ComponentModel;
using System.Runtime.CompilerServices;
using StatsScreen.Models;
using StatsScreen.Services.Presentation;
using StatsScreen.Services.Settings;
using WpfBrush = System.Windows.Media.Brush;

namespace StatsScreen.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private string _statusText = "WAITING FOR SENSORS";
    private string _cpuHardwareText = "CPU";
    private string _gpuHardwareText = "GPU";

    public DashboardViewModel()
    {
        CpuTemperature = new MetricViewModel(
            isTemperature: true,
            showTemperatureSource: true,
            defaultAccentKey: AccentPalette.DefaultCpuTemperatureKey);
        GpuTemperature = new MetricViewModel(
            isTemperature: true,
            defaultAccentKey: AccentPalette.DefaultGpuTemperatureKey);
        CpuPower = new MetricViewModel(defaultAccentKey: AccentPalette.DefaultCpuPowerKey);
        GpuPower = new MetricViewModel(defaultAccentKey: AccentPalette.DefaultGpuPowerKey);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MetricViewModel CpuTemperature { get; }

    public MetricViewModel GpuTemperature { get; }

    public MetricViewModel CpuPower { get; }

    public MetricViewModel GpuPower { get; }

    public WpfBrush CpuHardwareBrush => CpuTemperature.AccentBrush;

    public WpfBrush GpuHardwareBrush => GpuTemperature.AccentBrush;

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string CpuHardwareText
    {
        get => _cpuHardwareText;
        private set => SetField(ref _cpuHardwareText, value);
    }

    public string GpuHardwareText
    {
        get => _gpuHardwareText;
        private set => SetField(ref _gpuHardwareText, value);
    }

    public void ApplyAccentSettings(AppSettings settings)
    {
        CpuTemperature.SetAccent(settings.CpuTemperatureAccent);
        GpuTemperature.SetAccent(settings.GpuTemperatureAccent);
        CpuPower.SetAccent(settings.CpuPowerAccent);
        GpuPower.SetAccent(settings.GpuPowerAccent);
        RaisePropertyChanged(nameof(CpuHardwareBrush));
        RaisePropertyChanged(nameof(GpuHardwareBrush));
    }

    public void SetAccent(DashboardAccentTarget target, string accentKey)
    {
        switch (target)
        {
            case DashboardAccentTarget.CpuTemperature:
                CpuTemperature.SetAccent(accentKey);
                RaisePropertyChanged(nameof(CpuHardwareBrush));
                break;
            case DashboardAccentTarget.GpuTemperature:
                GpuTemperature.SetAccent(accentKey);
                RaisePropertyChanged(nameof(GpuHardwareBrush));
                break;
            case DashboardAccentTarget.CpuPower:
                CpuPower.SetAccent(accentKey);
                break;
            case DashboardAccentTarget.GpuPower:
                GpuPower.SetAccent(accentKey);
                break;
        }
    }

    public void Apply(DashboardSnapshot snapshot)
    {
        CpuTemperature.Apply(snapshot.CpuTemperature);
        GpuTemperature.Apply(snapshot.GpuTemperature);
        CpuPower.Apply(snapshot.CpuPower);
        GpuPower.Apply(snapshot.GpuPower);
        CpuHardwareText = HardwareNameFormatter.FormatCpu(snapshot.CpuHardwareName);
        GpuHardwareText = HardwareNameFormatter.FormatGpu(snapshot.GpuHardwareName);
        StatusText = string.Equals(snapshot.Status, "LIVE", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : snapshot.Status;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
