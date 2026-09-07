using System.Globalization;
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

        bool hasAnyReading = cpuTemperature is not null ||
                              gpuTemperature is not null ||
                              cpuPower is not null ||
                              gpuPower is not null;

        return new DashboardSnapshot(
            ToMetric(cpuTemperature, "°C"),
            ToMetric(gpuTemperature, "°C"),
            ToMetric(cpuPower, "W"),
            ToMetric(gpuPower, "W"),
            capturedAt,
            hasAnyReading ? "LIVE" : "NO TARGET SENSORS",
            hasAnyReading,
            HardwareName(cpuTemperature, cpuPower),
            HardwareName(gpuTemperature, gpuPower));
    }

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
