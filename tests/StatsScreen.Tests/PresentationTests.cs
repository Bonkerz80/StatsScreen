using StatsScreen.Models;
using StatsScreen.Services.Hardware;
using StatsScreen.Services.Presentation;
using StatsScreen.Services.Temperature;
using StatsScreen.ViewModels;
using Xunit;

namespace StatsScreen.Tests;

public sealed class PresentationTests
{
    [Theory]
    [InlineData(69.9, TemperatureStatus.Normal)]
    [InlineData(70, TemperatureStatus.Warm)]
    [InlineData(84.9, TemperatureStatus.Warm)]
    [InlineData(85, TemperatureStatus.Hot)]
    public void TemperatureStatusUsesTheDefinedBands(double value, TemperatureStatus expected)
    {
        Assert.Equal(expected, TemperatureStatusRules.Classify(value));
    }

    [Fact]
    public void MissingTemperatureIsUnavailableAndShowsConciseDisplayWarning()
    {
        Assert.Equal(TemperatureStatus.Unavailable, TemperatureStatusRules.Classify(null));
        Assert.Equal(0, TemperatureStatusRules.ToDisplayPercent(null));

        var viewModel = new DashboardViewModel();
        viewModel.Apply(DashboardSnapshot.Unavailable("NO TARGET SENSORS"));

        Assert.Equal(TemperatureStatus.Unavailable, viewModel.CpuTemperature.TemperatureStatus);
        Assert.Equal(TemperatureStatus.Unavailable, viewModel.GpuTemperature.TemperatureStatus);
        Assert.Equal(0, viewModel.CpuTemperature.TemperaturePercent);
        Assert.Equal("N/A", viewModel.CpuTemperature.ValueText);
        Assert.Equal("SENSOR UNAVAILABLE", viewModel.CpuTemperature.DetailText);
    }

    [Fact]
    public void DashboardShowsTrimmedDetectedHardwareNames()
    {
        var viewModel = new DashboardViewModel();
        viewModel.Apply(new DashboardSnapshot(
            new SensorMetric(61, "°C", "AMD Ryzen 7 9800X3D / Core (Tctl/Tdie)"),
            new SensorMetric(55, "°C", "AMD Radeon RX 9070 XT / GPU Core"),
            new SensorMetric(66, "W", "AMD Ryzen 7 9800X3D / Package Power"),
            new SensorMetric(182, "W", "AMD Radeon RX 9070 XT / GPU Package"),
            DateTimeOffset.Now,
            string.Empty,
            true,
            "AMD Ryzen 7 9800X3D",
            "AMD Radeon RX 9070 XT"));

        Assert.Equal("Ryzen 7 9800X3D", viewModel.CpuHardwareText);
        Assert.Equal("Radeon RX 9070 XT", viewModel.GpuHardwareText);
        Assert.Equal("Tctl/Tdie", viewModel.CpuTemperature.DetailText);
        Assert.Equal(string.Empty, viewModel.GpuTemperature.DetailText);
        Assert.Equal(string.Empty, viewModel.StatusText);
    }

    [Theory]
    [InlineData("AMD Ryzen 7 9800X3D", "Ryzen 7 9800X3D")]
    [InlineData("AMD Radeon(TM) Graphics", "Radeon Graphics")]
    [InlineData("NVIDIA GeForce RTX 4080 @ 2.5 GHz", "GeForce RTX 4080")]
    public void HardwareNameFormatterRemovesVendorNoise(string input, string expected)
    {
        Assert.Equal(expected, HardwareNameFormatter.FormatGpu(input));
    }

    [Theory]
    [InlineData("AMD Ryzen 7 9800X3D / Core (Tctl/Tdie)", "Tctl/Tdie")]
    [InlineData("AMD Ryzen 7 9800X3D / Core (Tdie)", "Tdie")]
    [InlineData("AMD Ryzen 7 9800X3D / Core #7", "Core #7")]
    [InlineData("AMD Ryzen 7 9800X3D / Package", "CPU Package")]
    public void TemperatureSourceFormatterShowsOnlyTheFriendlySensorName(string source, string expected)
    {
        Assert.Equal(expected, TemperatureSourceFormatter.Format(source));
    }

    [Theory]
    [InlineData("Core (Tctl/Tdie)", "Tctl/Tdie")]
    [InlineData("Core (Tdie)", "Tdie")]
    [InlineData("Core #3", "Core #3")]
    public void CpuTemperatureSourceDoesNotExposeFallbackWording(string source, string expected)
    {
        var metric = new MetricViewModel(isTemperature: true, showTemperatureSource: true);

        metric.Apply(new SensorMetric(65, "°C", $"Test CPU / {source}"));

        Assert.Equal(expected, metric.DetailText);
        Assert.DoesNotContain("CPU FALLBACK", metric.DetailText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIVE SENSOR", metric.DetailText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HealthyDashboardLeavesCentreStatusBlank()
    {
        var viewModel = new DashboardViewModel();
        viewModel.Apply(new DashboardSnapshot(
            new SensorMetric(65, "°C", "CPU / Core (Tctl/Tdie)"),
            new SensorMetric(55, "°C", "GPU / GPU Core"),
            new SensorMetric(42, "W", "CPU / Package Power"),
            new SensorMetric(53, "W", "GPU / GPU Package"),
            DateTimeOffset.Now,
            string.Empty,
            true));

        Assert.Equal(string.Empty, viewModel.StatusText);
    }

    [Fact]
    public void ImportantDashboardStatusRemainsVisible()
    {
        var viewModel = new DashboardViewModel();
        viewModel.Apply(DashboardSnapshot.Unavailable("HARDWARE ERROR"));

        Assert.Equal("HARDWARE ERROR", viewModel.StatusText);
    }

    [Fact]
    public void PartialHardwareSnapshotReportsSensorError()
    {
        HardwareSensorDescriptor[] sensors =
        [
            Gpu("gpu", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Core", DetectedSensorType.Temperature, 55),
            Gpu("gpu", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Package", DetectedSensorType.Power, 53)
        ];

        DashboardSnapshot snapshot = HardwareSnapshotFactory.CreateSnapshot(sensors, DateTimeOffset.Now);

        Assert.Equal("SENSOR ERROR", snapshot.Status);
    }

    [Fact]
    public void CpuPowerZeroDescriptorValueIsPreserved()
    {
        HardwareSensorDescriptor[] sensors =
        [
            new("cpu", "Test CPU", DetectedHardwareType.Cpu, 0, "Core (Tctl/Tdie)", DetectedSensorType.Temperature, 65),
            new("cpu", "Test CPU", DetectedHardwareType.Cpu, 0, "Package Power", DetectedSensorType.Power, 0),
            Gpu("gpu", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Core", DetectedSensorType.Temperature, 55),
            Gpu("gpu", "AMD Radeon RX 9070 XT", DetectedHardwareType.GpuAmd, 1, "GPU Package", DetectedSensorType.Power, 53)
        ];

        DashboardSnapshot snapshot = HardwareSnapshotFactory.CreateSnapshot(sensors, DateTimeOffset.Now);

        Assert.Equal(0, snapshot.CpuPower.Value);
        Assert.Equal(string.Empty, snapshot.Status);
    }

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
