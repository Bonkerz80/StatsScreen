using StatsScreen.Models;
using StatsScreen.Services.Hardware;
using Xunit;

namespace StatsScreen.Tests;

public sealed class SensorSelectionTests
{
    [Fact]
    public void ActualAmdIntegratedAndDiscreteInventorySelects9070Xt()
    {
        HardwareSensorDescriptor[] sensors =
        [
            Gpu("integrated", "AMD Radeon(TM) Graphics", DetectedHardwareType.GpuAmd, 0, "GPU VR SoC", DetectedSensorType.Temperature, 54),
            Gpu("integrated", "AMD Radeon(TM) Graphics", DetectedHardwareType.GpuAmd, 0, "GPU SoC", DetectedSensorType.Power, 14),
            Gpu("discrete", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Core", DetectedSensorType.Temperature, 52),
            Gpu("discrete", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Package", DetectedSensorType.Power, 50)
        ];
        Assert.Equal("discrete", SensorSelection.SelectGpuTemperature(sensors)?.HardwareId);
        Assert.Equal("discrete", SensorSelection.SelectGpuPower(sensors)?.HardwareId);
        sensors[2] = sensors[2] with { Value = null };
        Assert.Null(SensorSelection.SelectGpuTemperature(sensors));
        Assert.Equal("discrete", SensorSelection.SelectGpuPower(sensors)?.HardwareId);
        Assert.Null(SensorSelection.SelectGpuTemperature(sensors.Take(2)));
        Assert.Null(SensorSelection.SelectGpuPower(sensors.Take(2)));
    }

    [Fact]
    public void MissingCoreReadingsDoNotSilentlyBecomePackageReadings()
    {
        Assert.Null(SensorSelection.SelectCpuTemperature([CpuTemperature("Core #1", null), CpuTemperature("Package", 70)]));
        Assert.Null(SensorSelection.SelectCpuTemperature([CpuTemperature("Core (Tctl/Tdie)", 0)]));
        Assert.Equal(60f, SensorSelection.SelectCpuTemperature([CpuTemperature("Core #1", float.NaN), CpuTemperature("Core #2", 60)])?.Value);
        Assert.Null(SensorSelection.SelectCpuPower([CpuPower("SoC", 20)]));
    }

    [Fact]
    public void CpuTemperatureSourceIsVisibleAndClearsWhenUnavailable()
    {
        var metric = new StatsScreen.ViewModels.MetricViewModel(
            isTemperature: true,
            showTemperatureSource: true);
        metric.Apply(new SensorMetric(65, "°C", "Ryzen / Core (Tctl/Tdie)"));
        Assert.Equal("Tctl/Tdie", metric.DetailText);
        Assert.DoesNotContain("CPU FALLBACK", metric.DetailText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIVE SENSOR", metric.DetailText, StringComparison.OrdinalIgnoreCase);
        metric.Apply(SensorMetric.Missing("°C"));
        Assert.Equal("N/A", metric.ValueText);
        Assert.Equal("SENSOR UNAVAILABLE", metric.DetailText);
    }

    [Fact]
    public void CpuTemperatureUsesHighestIndividualCoreInsteadOfPackageTemperature()
    {
        HardwareSensorDescriptor[] sensors =
        [
            CpuTemperature("Package", 72),
            CpuTemperature("Core #0", 61),
            CpuTemperature("Core #1", 67),
            CpuTemperature("Core #2", 64)
        ];

        HardwareSensorDescriptor? selected = SensorSelection.SelectCpuTemperature(sensors);

        Assert.NotNull(selected);
        Assert.Equal("Core #1", selected.SensorName);
        Assert.Equal(67, selected.Value);
    }

    [Fact]
    public void CpuTemperatureFallsBackToTctlTdieWhenNumberedCoresAreNotExposed()
    {
        HardwareSensorDescriptor[] sensors =
        [
            CpuTemperature("CPU Package", 70),
            CpuTemperature("Core (Tctl/Tdie)", 63)
        ];

        HardwareSensorDescriptor? selected = SensorSelection.SelectCpuTemperature(sensors);

        Assert.NotNull(selected);
        Assert.Equal("Core (Tctl/Tdie)", selected.SensorName);
    }

    [Fact]
    public void CpuPowerPrefersPackagePowerAndIgnoresPerCorePower()
    {
        HardwareSensorDescriptor[] sensors =
        [
            CpuPower("IA Cores", 34),
            CpuPower("Package Power", 88),
            CpuPower("CPU Power Limit", 125)
        ];

        HardwareSensorDescriptor? selected = SensorSelection.SelectCpuPower(sensors);

        Assert.NotNull(selected);
        Assert.Equal("Package Power", selected.SensorName);
        Assert.Equal(88, selected.Value);
    }

    [Fact]
    public void GpuSelectionPrefersDiscreteGpuAndTotalBoardPower()
    {
        HardwareSensorDescriptor[] sensors =
        [
            Gpu("igpu", "Intel Graphics", DetectedHardwareType.GpuIntel, 0, "GPU Temperature", DetectedSensorType.Temperature, 48),
            Gpu("igpu", "Intel Graphics", DetectedHardwareType.GpuIntel, 0, "GPU Power", DetectedSensorType.Power, 12),
            Gpu("dgpu", "NVIDIA GeForce", DetectedHardwareType.GpuNvidia, 1, "GPU Core", DetectedSensorType.Temperature, 55),
            Gpu("dgpu", "NVIDIA GeForce", DetectedHardwareType.GpuNvidia, 1, "GPU Board Power", DetectedSensorType.Power, 142),
            Gpu("dgpu", "NVIDIA GeForce", DetectedHardwareType.GpuNvidia, 1, "GPU Core Power", DetectedSensorType.Power, 95),
            Gpu("dgpu", "NVIDIA GeForce", DetectedHardwareType.GpuNvidia, 1, "Hot Spot", DetectedSensorType.Temperature, 72)
        ];

        HardwareSensorDescriptor? temperature = SensorSelection.SelectGpuTemperature(sensors);
        HardwareSensorDescriptor? power = SensorSelection.SelectGpuPower(sensors);

        Assert.NotNull(temperature);
        Assert.NotNull(power);
        Assert.Equal("dgpu", temperature.HardwareId);
        Assert.Equal("GPU Core", temperature.SensorName);
        Assert.Equal("GPU Board Power", power.SensorName);
        Assert.Equal(142, power.Value);
    }

    [Fact]
    public void MissingValuesAreNotSelected()
    {
        HardwareSensorDescriptor[] sensors =
        [CpuTemperature("Core #0", null)];

        Assert.Null(SensorSelection.SelectCpuTemperature(sensors));
    }

    private static HardwareSensorDescriptor CpuTemperature(string name, float? value) =>
        new("cpu", "Test CPU", DetectedHardwareType.Cpu, 0, name, DetectedSensorType.Temperature, value);

    private static HardwareSensorDescriptor CpuPower(string name, float? value) =>
        new("cpu", "Test CPU", DetectedHardwareType.Cpu, 0, name, DetectedSensorType.Power, value);

    private static HardwareSensorDescriptor Gpu(
        string id,
        string hardwareName,
        DetectedHardwareType hardwareType,
        int index,
        string sensorName,
        DetectedSensorType sensorType,
        float? value) =>
        new(id, hardwareName, hardwareType, index, sensorName, sensorType, value);
}
