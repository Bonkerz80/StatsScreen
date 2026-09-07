using System.Text.RegularExpressions;

namespace StatsScreen.Services.Presentation;

public static partial class HardwareNameFormatter
{
    public static string FormatCpu(string? hardwareName) => Format(hardwareName, "CPU");

    public static string FormatGpu(string? hardwareName) => Format(hardwareName, "GPU");

    private static string Format(string? hardwareName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hardwareName))
            return fallback;

        string cleaned = hardwareName.Trim();
        cleaned = cleaned
            .Replace("(R)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("(TM)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("(C)", string.Empty, StringComparison.OrdinalIgnoreCase);
        cleaned = AtMarkerRegex().Replace(cleaned, string.Empty);
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();

        cleaned = RemoveVendorPrefix(cleaned, "AMD");
        cleaned = RemoveVendorPrefix(cleaned, "Intel");
        cleaned = RemoveVendorPrefix(cleaned, "NVIDIA");

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }

    private static string RemoveVendorPrefix(string value, string vendor)
    {
        return value.StartsWith(vendor + " ", StringComparison.OrdinalIgnoreCase)
            ? value[(vendor.Length + 1)..].TrimStart()
            : value;
    }

    [GeneratedRegex(@"\s+@\s+.*$", RegexOptions.CultureInvariant)]
    private static partial Regex AtMarkerRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
