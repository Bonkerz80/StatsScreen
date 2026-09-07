using System.ComponentModel;
using System.Runtime.CompilerServices;
using StatsScreen.Models;

namespace StatsScreen.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private string _statusText = "STARTING";
    private string _lastUpdateText = "Waiting for hardware";

    public DashboardViewModel()
    {
        CpuTemperature = new MetricViewModel();
        GpuTemperature = new MetricViewModel();
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

    public string LastUpdateText
    {
        get => _lastUpdateText;
        private set => SetField(ref _lastUpdateText, value);
    }

    public void Apply(DashboardSnapshot snapshot)
    {
        CpuTemperature.Apply(snapshot.CpuTemperature);
        GpuTemperature.Apply(snapshot.GpuTemperature);
        CpuPower.Apply(snapshot.CpuPower);
        GpuPower.Apply(snapshot.GpuPower);
        StatusText = snapshot.Status;
        LastUpdateText = snapshot.IsHardwareAvailable
            ? $"Updated {snapshot.CapturedAt.LocalDateTime:HH:mm:ss}"
            : "Waiting for readable sensors";
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
