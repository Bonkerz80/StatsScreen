namespace StatsScreen.Models;

public sealed record DashboardSnapshot(
    SensorMetric CpuTemperature,
    SensorMetric GpuTemperature,
    SensorMetric CpuPower,
    SensorMetric GpuPower,
    DateTimeOffset CapturedAt,
    string Status,
    bool IsHardwareAvailable,
    string CpuHardwareName = "",
    string GpuHardwareName = "")
{
    public CpuCoreTemperatureSnapshot? CoreTemperatures { get; init; }
    public SensorMetric? TctlTdie { get; init; }

    public static DashboardSnapshot Unavailable(string status) => new(
        SensorMetric.Missing("°C"),
        SensorMetric.Missing("°C"),
        SensorMetric.Missing("W"),
        SensorMetric.Missing("W"),
        DateTimeOffset.Now,
        status,
        false);
}
