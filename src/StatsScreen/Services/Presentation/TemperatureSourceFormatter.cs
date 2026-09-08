namespace StatsScreen.Services.Presentation;

public static class TemperatureSourceFormatter
{
    public static string Format(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        int separator = source.LastIndexOf(" / ", StringComparison.Ordinal);
        string sensorName = separator >= 0
            ? source[(separator + 3)..].Trim()
            : source.Trim();

        if (sensorName.StartsWith("Core (", StringComparison.OrdinalIgnoreCase) &&
            sensorName.EndsWith(')'))
        {
            sensorName = sensorName[6..^1].Trim();
        }

        return sensorName.Equals("Package", StringComparison.OrdinalIgnoreCase)
            ? "CPU Package"
            : sensorName;
    }
}
