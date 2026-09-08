using StatsScreen.Services.Presentation;

namespace StatsScreen.Services.Settings;

public sealed class AppSettings
{
    public string? MonitorDeviceName { get; set; }

    public bool StartInDisplayMode { get; set; }

    public int PollIntervalMilliseconds { get; set; } = 1000;

    public string CpuTemperatureAccent { get; set; } = AccentPalette.DefaultCpuTemperatureKey;

    public string GpuTemperatureAccent { get; set; } = AccentPalette.DefaultGpuTemperatureKey;

    public string CpuPowerAccent { get; set; } = AccentPalette.DefaultCpuPowerKey;

    public string GpuPowerAccent { get; set; } = AccentPalette.DefaultGpuPowerKey;

    public AppSettings Clone() => new()
    {
        MonitorDeviceName = MonitorDeviceName,
        StartInDisplayMode = StartInDisplayMode,
        PollIntervalMilliseconds = PollIntervalMilliseconds,
        CpuTemperatureAccent = CpuTemperatureAccent,
        GpuTemperatureAccent = GpuTemperatureAccent,
        CpuPowerAccent = CpuPowerAccent,
        GpuPowerAccent = GpuPowerAccent
    };

    public void Normalize()
    {
        PollIntervalMilliseconds = Math.Clamp(PollIntervalMilliseconds, 250, 10_000);
        CpuTemperatureAccent = AccentPalette.NormalizeKey(
            CpuTemperatureAccent,
            AccentPalette.DefaultCpuTemperatureKey);
        GpuTemperatureAccent = AccentPalette.NormalizeKey(
            GpuTemperatureAccent,
            AccentPalette.DefaultGpuTemperatureKey);
        CpuPowerAccent = AccentPalette.NormalizeKey(
            CpuPowerAccent,
            AccentPalette.DefaultCpuPowerKey);
        GpuPowerAccent = AccentPalette.NormalizeKey(
            GpuPowerAccent,
            AccentPalette.DefaultGpuPowerKey);
    }

    public void ResetAccentColors()
    {
        CpuTemperatureAccent = AccentPalette.DefaultCpuTemperatureKey;
        GpuTemperatureAccent = AccentPalette.DefaultGpuTemperatureKey;
        CpuPowerAccent = AccentPalette.DefaultCpuPowerKey;
        GpuPowerAccent = AccentPalette.DefaultGpuPowerKey;
    }
}
