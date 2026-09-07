using System.Text.RegularExpressions;
using StatsScreen.Models;

namespace StatsScreen.Services.Hardware;

public static partial class SensorSelection
{
    public static HardwareSensorDescriptor? SelectCpuTemperature(
        IEnumerable<HardwareSensorDescriptor> sensors)
    {
        var candidates = sensors
            .Where(IsCpuTemperature)
            .ToArray();

        HardwareSensorDescriptor[] individualCoreSensors = candidates
            .Where(sensor => IsIndividualCoreTemperature(sensor.SensorName))
            .ToArray();

        if (individualCoreSensors.Length > 0)
        {
            return individualCoreSensors
                .Where(Valid)
                .OrderByDescending(sensor => sensor.Value!.Value)
                .ThenBy(sensor => sensor.SensorName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        // AMD systems commonly expose Tctl/Tdie or Tdie rather than numbered cores.
        // These are only considered after the individual-core pass above.
        return candidates
            .Where(Valid)
            .Where(sensor => IsCpuAggregateTemperature(sensor.SensorName))
            .OrderByDescending(sensor => CpuAggregateTemperatureScore(sensor.SensorName))
            .ThenByDescending(sensor => sensor.Value!.Value)
            .ThenBy(sensor => sensor.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static HardwareSensorDescriptor? SelectCpuPower(
        IEnumerable<HardwareSensorDescriptor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.HardwareType == DetectedHardwareType.Cpu)
            .Where(sensor => sensor.SensorType == DetectedSensorType.Power)
            .Where(Valid)
            .Where(sensor => !ContainsAny(
                sensor.SensorName,
                "limit",
                "peak",
                "rail",
                "per core",
                "per-core",
                "core",
                "cores",
                "uncore",
                "dram"))
            .ToArray();

        return candidates
            .Where(sensor => CpuPowerScore(sensor.SensorName) > 10)
            .OrderByDescending(sensor => CpuPowerScore(sensor.SensorName))
            .ThenByDescending(sensor => sensor.Value!.Value)
            .ThenBy(sensor => sensor.SensorName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static HardwareSensorDescriptor? SelectGpuTemperature(
        IEnumerable<HardwareSensorDescriptor> sensors)
    {
        GpuGroup? group = SelectGpuGroup(sensors);
        return group is null
            ? null
            : group.Sensors
                .Where(sensor => sensor.SensorType == DetectedSensorType.Temperature)
                .Where(Valid)
                .Where(sensor => GpuTemperatureScore(sensor.SensorName) >= 90)
                .Where(sensor => !ContainsAny(sensor.SensorName, "hot spot", "hotspot", "junction", "memory", "vrm"))
                .OrderByDescending(sensor => GpuTemperatureScore(sensor.SensorName))
                .ThenByDescending(sensor => sensor.Value!.Value)
                .ThenBy(sensor => sensor.SensorName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    public static HardwareSensorDescriptor? SelectGpuPower(
        IEnumerable<HardwareSensorDescriptor> sensors)
    {
        GpuGroup? group = SelectGpuGroup(sensors);
        return group is null
            ? null
            : group.Sensors
                .Where(sensor => sensor.SensorType == DetectedSensorType.Power)
                .Where(Valid)
                .Where(sensor => GpuPowerScore(sensor.SensorName) >= 100)
                .Where(sensor => !ContainsAny(
                    sensor.SensorName,
                    "limit",
                    "rail",
                    "pin",
                    "connector",
                    "pcie",
                    "memory",
                    "core",
                    "12v",
                    "input",
                    "output"))
                .OrderByDescending(sensor => GpuPowerScore(sensor.SensorName))
                .ThenByDescending(sensor => sensor.Value!.Value)
                .ThenBy(sensor => sensor.SensorName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    private static GpuGroup? SelectGpuGroup(IEnumerable<HardwareSensorDescriptor> sensors)
    {
        return sensors
            .Where(sensor => IsGpu(sensor.HardwareType))
            .GroupBy(sensor => sensor.HardwareId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GpuGroup(
                group.Key,
                group.ToArray(),
                group.First().HardwareType,
                group.First().HardwareName,
                group.Min(sensor => sensor.HardwareIndex)))
            .Where(group => group.Sensors.Any(sensor =>
                sensor.SensorType is DetectedSensorType.Temperature or DetectedSensorType.Power))
            .OrderByDescending(group => ContainsAny(group.HardwareName, "Radeon RX", "Radeon Pro", "GeForce", "Quadro", "Arc A", "Arc B"))
            .ThenBy(group => ContainsAny(group.HardwareName, "Radeon(TM) Graphics", "Radeon Graphics", "UHD", "Iris"))
            .ThenByDescending(group => DiscreteGpuPriority(group.HardwareType))
            .ThenBy(group => group.HardwareIndex)
            .ThenBy(group => group.HardwareName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool IsCpuTemperature(HardwareSensorDescriptor sensor) =>
        sensor.HardwareType == DetectedHardwareType.Cpu &&
        sensor.SensorType == DetectedSensorType.Temperature;

    private static bool IsIndividualCoreTemperature(string sensorName)
    {
        string normalized = sensorName.Trim().ToLowerInvariant();
        if (ContainsAny(normalized, "package", "tctl", "tdie", "junction", "hot spot", "distance", "tj max"))
            return false;

        return CoreNumberRegex().IsMatch(normalized);
    }

    private static bool IsCpuAggregateTemperature(string sensorName)
    {
        string normalized = sensorName.Trim().ToLowerInvariant();
        return ContainsAny(normalized, "tctl/tdie", "tdie", "tctl", "package", "cpu temperature", "cpu temp");
    }

    private static int CpuAggregateTemperatureScore(string sensorName)
    {
        string normalized = sensorName.ToLowerInvariant();
        if (normalized.Contains("tctl/tdie", StringComparison.Ordinal))
            return 100;
        if (normalized.Contains("tdie", StringComparison.Ordinal))
            return 95;
        if (normalized.Contains("tctl", StringComparison.Ordinal))
            return 90;
        if (normalized.Contains("cpu temperature", StringComparison.Ordinal) ||
            normalized.Contains("cpu temp", StringComparison.Ordinal))
            return 70;
        if (normalized.Contains("package", StringComparison.Ordinal))
            return 60;
        return 50;
    }

    private static int CpuPowerScore(string sensorName)
    {
        string normalized = sensorName.ToLowerInvariant();
        if (ContainsAny(normalized, "package power", "cpu package", "processor package"))
            return 100;
        if (ContainsAny(normalized, "total cpu", "total processor"))
            return 90;
        if (ContainsAny(normalized, "cpu power", "processor power"))
            return 80;
        if (normalized.Contains("package", StringComparison.Ordinal))
            return 75;
        return 10;
    }

    private static int GpuTemperatureScore(string sensorName)
    {
        string normalized = sensorName.ToLowerInvariant();
        if (ContainsAny(normalized, "gpu core", "core temperature", "core temp"))
            return 100;
        if (normalized.Equals("core", StringComparison.Ordinal) ||
            normalized.Contains("gpu temperature", StringComparison.Ordinal) ||
            normalized.Contains("gpu temp", StringComparison.Ordinal))
            return 90;
        if (normalized.Contains("temperature", StringComparison.Ordinal) ||
            normalized.Contains("temp", StringComparison.Ordinal))
            return 60;
        return 10;
    }

    private static int GpuPowerScore(string sensorName)
    {
        string normalized = sensorName.ToLowerInvariant();
        if (ContainsAny(normalized, "total board", "gpu board", "board power"))
            return 110;
        if (ContainsAny(normalized, "gpu package", "total gpu", "gpu power"))
            return 100;
        if (normalized.Equals("power", StringComparison.Ordinal))
            return 80;
        if (normalized.Contains("power", StringComparison.Ordinal))
            return 50;
        return 10;
    }

    private static bool IsGpu(DetectedHardwareType hardwareType) =>
        hardwareType is DetectedHardwareType.GpuNvidia or
            DetectedHardwareType.GpuAmd or
            DetectedHardwareType.GpuIntel;

    private static int DiscreteGpuPriority(DetectedHardwareType hardwareType) =>
        hardwareType is DetectedHardwareType.GpuNvidia or DetectedHardwareType.GpuAmd ? 2 : 1;

    private static bool HasUsableGpuTemperature(IEnumerable<HardwareSensorDescriptor> sensors) =>
        sensors.Any(sensor =>
            sensor.SensorType == DetectedSensorType.Temperature &&
            sensor.Value.HasValue &&
            !ContainsAny(sensor.SensorName, "hot spot", "hotspot", "junction", "memory", "vrm"));

    private static bool HasUsableGpuPower(IEnumerable<HardwareSensorDescriptor> sensors) =>
        sensors.Any(sensor =>
            sensor.SensorType == DetectedSensorType.Power &&
            sensor.Value.HasValue &&
            !ContainsAny(
                sensor.SensorName,
                "limit",
                "rail",
                "pin",
                "connector",
                "pcie",
                "memory",
                "core",
                "12v",
                "input",
                "output"));

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static bool Valid(HardwareSensorDescriptor sensor) => sensor.Value is { } v && float.IsFinite(v) &&
        (sensor.SensorType == DetectedSensorType.Temperature ? v > 0 : v >= 0);

    [GeneratedRegex(@"\bcore\s*(?:#|number\s*)?\d+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CoreNumberRegex();

    private sealed record GpuGroup(
        string HardwareId,
        HardwareSensorDescriptor[] Sensors,
        DetectedHardwareType HardwareType,
        string HardwareName,
        int HardwareIndex);
}
