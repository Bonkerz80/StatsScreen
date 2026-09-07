using StatsScreen.Models;
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
    public void MissingTemperatureIsUnavailableAndHasNoDisplayWarning()
    {
        Assert.Equal(TemperatureStatus.Unavailable, TemperatureStatusRules.Classify(null));
        Assert.Equal(0, TemperatureStatusRules.ToDisplayPercent(null));

        var viewModel = new DashboardViewModel();
        viewModel.Apply(DashboardSnapshot.Unavailable("NO TARGET SENSORS"));

        Assert.Equal(TemperatureStatus.Unavailable, viewModel.CpuTemperature.TemperatureStatus);
        Assert.Equal(TemperatureStatus.Unavailable, viewModel.GpuTemperature.TemperatureStatus);
        Assert.Equal(0, viewModel.CpuTemperature.TemperaturePercent);
        Assert.Equal("N/A", viewModel.CpuTemperature.ValueText);
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
            "LIVE",
            true,
            "AMD Ryzen 7 9800X3D",
            "AMD Radeon RX 9070 XT"));

        Assert.Equal("Ryzen 7 9800X3D", viewModel.CpuHardwareText);
        Assert.Equal("Radeon RX 9070 XT", viewModel.GpuHardwareText);
        Assert.Equal("Tctl/Tdie · CPU FALLBACK", viewModel.CpuTemperature.DetailText);
    }

    [Theory]
    [InlineData("AMD Ryzen 7 9800X3D", "Ryzen 7 9800X3D")]
    [InlineData("AMD Radeon(TM) Graphics", "Radeon Graphics")]
    [InlineData("NVIDIA GeForce RTX 4080 @ 2.5 GHz", "GeForce RTX 4080")]
    public void HardwareNameFormatterRemovesVendorNoise(string input, string expected)
    {
        Assert.Equal(expected, HardwareNameFormatter.FormatGpu(input));
    }
}
