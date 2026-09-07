namespace StatsScreen.Models;

public sealed record SensorMetric(
    double? Value,
    string Unit,
    string Source,
    string Format = "0.0")
{
    public static SensorMetric Missing(string unit) => new(null, unit, string.Empty);
}
