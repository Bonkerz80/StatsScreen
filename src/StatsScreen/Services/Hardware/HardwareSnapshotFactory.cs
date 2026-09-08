using StatsScreen.Models;

namespace StatsScreen.Services.Hardware;

public static class HardwareSnapshotFactory
{
    public static DashboardSnapshot CreateSnapshot(
        IReadOnlyList<HardwareSensorDescriptor> sensors,
        DateTimeOffset capturedAt)
    {
        HardwareSensorDescriptor? cpuTemperature = SensorSelection.SelectCpuTemperature(sensors);
        HardwareSensorDescriptor? gpuTemperature = SensorSelection.SelectGpuTemperature(sensors);
        HardwareSensorDescriptor? cpuPower = SensorSelection.SelectCpuPower(sensors);
        HardwareSensorDescriptor? gpuPower = SensorSelection.SelectGpuPower(sensors);

        SensorMetric cpuTemperatureMetric = ToMetric(cpuTemperature, "°C");
        SensorMetric gpuTemperatureMetric = ToMetric(gpuTemperature, "°C");
        SensorMetric cpuPowerMetric = ToMetric(cpuPower, "W");
        SensorMetric gpuPowerMetric = ToMetric(gpuPower, "W");

        bool hasAnyReading = IsAvailable(cpuTemperatureMetric) ||
                              IsAvailable(gpuTemperatureMetric) ||
                              IsAvailable(cpuPowerMetric) ||
                              IsAvailable(gpuPowerMetric);
        bool hasAllExpectedReadings = IsAvailable(cpuTemperatureMetric) &&
                                       IsAvailable(gpuTemperatureMetric) &&
                                       IsAvailable(cpuPowerMetric) &&
                                       IsAvailable(gpuPowerMetric);
        string status = hasAllExpectedReadings
            ? string.Empty
            : hasAnyReading ? "SENSOR ERROR" : "NO TARGET SENSORS";

        return new DashboardSnapshot(
            cpuTemperatureMetric,
            gpuTemperatureMetric,
            cpuPowerMetric,
            gpuPowerMetric,
            capturedAt,
            status,
            hasAnyReading,
            HardwareName(cpuTemperature, cpuPower),
            HardwareName(gpuTemperature, gpuPower));
    }

    private static bool IsAvailable(SensorMetric metric) =>
        metric.Value is { } value && !double.IsNaN(value) && !double.IsInfinity(value);

    private static string HardwareName(
        HardwareSensorDescriptor? primary,
        HardwareSensorDescriptor? secondary) =>
        primary?.HardwareName ?? secondary?.HardwareName ?? string.Empty;

    private static SensorMetric ToMetric(HardwareSensorDescriptor? sensor, string unit)
    {
        if (sensor?.Value is not { } value || float.IsNaN(value) || float.IsInfinity(value))
            return SensorMetric.Missing(unit);

        return new SensorMetric(
            value,
            unit,
            $"{sensor.HardwareName} / {sensor.SensorName}",
            "0.0");
    }
}
