namespace StatsScreen.Services.Settings;

public sealed class AppSettings
{
    public string? MonitorDeviceName { get; set; }

    public bool StartInDisplayMode { get; set; }

    public int PollIntervalMilliseconds { get; set; } = 1000;

    public AppSettings Clone() => new()
    {
        MonitorDeviceName = MonitorDeviceName,
        StartInDisplayMode = StartInDisplayMode,
        PollIntervalMilliseconds = PollIntervalMilliseconds
    };

    public void Normalize()
    {
        PollIntervalMilliseconds = Math.Clamp(PollIntervalMilliseconds, 250, 10_000);
    }
}
