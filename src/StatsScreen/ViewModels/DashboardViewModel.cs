using System.ComponentModel;
using System.Runtime.CompilerServices;
using StatsScreen.Models;
using StatsScreen.Services.Presentation;

namespace StatsScreen.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private string _statusText = "WAITING FOR SENSORS";
    private string _cpuHardwareText = "CPU";
    private string _gpuHardwareText = "GPU";

    public DashboardViewModel()
    {
        CpuTemperature = new MetricViewModel(isTemperature: true, showTemperatureSource: true);
        GpuTemperature = new MetricViewModel(isTemperature: true);
        CpuPower = new MetricViewModel();
        GpuPower = new MetricViewModel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MetricViewModel CpuTemperature { get; }

    public MetricViewModel GpuTemperature { get; }

    public MetricViewModel CpuPower { get; }

    public MetricViewModel GpuPower { get; }

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
}
