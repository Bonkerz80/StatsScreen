using StatsScreen.Models;

namespace StatsScreen.Services.Temperature;

public static class TemperatureStatusRules
{
    public const double WarmThresholdCelsius = 70;
    public const double HotThresholdCelsius = 85;
    public const double DisplayMaximumCelsius = 100;

    public static TemperatureStatus Classify(double? celsius)
    {
        if (celsius is not { } value || double.IsNaN(value) || double.IsInfinity(value))
            return TemperatureStatus.Unavailable;

        if (value >= HotThresholdCelsius)
            return TemperatureStatus.Hot;

        if (value >= WarmThresholdCelsius)
            return TemperatureStatus.Warm;

        return TemperatureStatus.Normal;
    }

    public static double ToDisplayPercent(double? celsius)
    {
        if (celsius is not { } value || double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        return Math.Clamp(value / DisplayMaximumCelsius * 100, 0, 100);
    }
}
