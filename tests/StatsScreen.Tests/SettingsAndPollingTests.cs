using StatsScreen.Models;
using StatsScreen.Services.Hardware;
using StatsScreen.Services.Logging;
using StatsScreen.Services.Polling;
using StatsScreen.Services.Presentation;
using StatsScreen.Services.Settings;
using Xunit;

namespace StatsScreen.Tests;

public sealed class SettingsAndPollingTests
{
    [Fact]
    public void SettingsRoundTripPersistsValuesUsedByContextMenuCommands()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"StatsScreen-tests-{Guid.NewGuid():N}");
        var logger = new TestLogger();

        try
        {
            var writer = new SettingsService(logger, directory);
            writer.Save(new AppSettings
            {
                MonitorDeviceName = "\\\\.\\DISPLAY2",
                StartInDisplayMode = true,
                PollIntervalMilliseconds = 2000,
                CpuTemperatureAccent = AccentPalette.PinkKey,
                GpuTemperatureAccent = AccentPalette.GreenKey,
                CpuPowerAccent = AccentPalette.OrangeKey,
                GpuPowerAccent = AccentPalette.WhiteKey
            });

            AppSettings loaded = new SettingsService(logger, directory).Load();

            Assert.Equal("\\\\.\\DISPLAY2", loaded.MonitorDeviceName);
            Assert.True(loaded.StartInDisplayMode);
            Assert.Equal(2000, loaded.PollIntervalMilliseconds);
            Assert.Equal(AccentPalette.PinkKey, loaded.CpuTemperatureAccent);
            Assert.Equal(AccentPalette.GreenKey, loaded.GpuTemperatureAccent);
            Assert.Equal(AccentPalette.OrangeKey, loaded.CpuPowerAccent);
            Assert.Equal(AccentPalette.WhiteKey, loaded.GpuPowerAccent);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void OlderSettingsFilesLoadWithTheNewDefaultColours()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"StatsScreen-tests-{Guid.NewGuid():N}");
        var logger = new TestLogger();

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "settings.json"),
                "{\"MonitorDeviceName\":\"\\\\.\\DISPLAY1\",\"PollIntervalMilliseconds\":1000}");

            AppSettings loaded = new SettingsService(logger, directory).Load();

            Assert.Equal(AccentPalette.DefaultCpuTemperatureKey, loaded.CpuTemperatureAccent);
            Assert.Equal(AccentPalette.DefaultGpuTemperatureKey, loaded.GpuTemperatureAccent);
            Assert.Equal(AccentPalette.DefaultCpuPowerKey, loaded.CpuPowerAccent);
            Assert.Equal(AccentPalette.DefaultGpuPowerKey, loaded.GpuPowerAccent);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void InvalidColourSettingsFallBackToTheirSectionDefaults()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"StatsScreen-tests-{Guid.NewGuid():N}");
        var logger = new TestLogger();

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "settings.json"),
                "{\"CpuTemperatureAccent\":\"NotAColour\",\"GpuTemperatureAccent\":\"Purple\",\"CpuPowerAccent\":null,\"GpuPowerAccent\":\"Green\"}");

            AppSettings loaded = new SettingsService(logger, directory).Load();

            Assert.Equal(AccentPalette.DefaultCpuTemperatureKey, loaded.CpuTemperatureAccent);
            Assert.Equal(AccentPalette.PurpleKey, loaded.GpuTemperatureAccent);
            Assert.Equal(AccentPalette.DefaultCpuPowerKey, loaded.CpuPowerAccent);
            Assert.Equal(AccentPalette.GreenKey, loaded.GpuPowerAccent);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ResetColoursRestoresTheFourDashboardDefaults()
    {
        var settings = new AppSettings
        {
            CpuTemperatureAccent = AccentPalette.RedKey,
            GpuTemperatureAccent = AccentPalette.LimeKey,
            CpuPowerAccent = AccentPalette.BlueKey,
            GpuPowerAccent = AccentPalette.GreyKey
        };

        settings.ResetAccentColors();
        settings.Normalize();

        Assert.Equal(AccentPalette.DefaultCpuTemperatureKey, settings.CpuTemperatureAccent);
        Assert.Equal(AccentPalette.DefaultGpuTemperatureKey, settings.GpuTemperatureAccent);
        Assert.Equal(AccentPalette.DefaultCpuPowerKey, settings.CpuPowerAccent);
        Assert.Equal(AccentPalette.DefaultGpuPowerKey, settings.GpuPowerAccent);
    }

    [Fact]
    public void DefaultPaletteUsesTheRequestedDashboardAccents()
    {
        Assert.Equal("#4DC6C7", AccentPalette.Get(AccentPalette.DefaultCpuTemperatureKey).Hex);
        Assert.Equal("#5F9FEA", AccentPalette.Get(AccentPalette.DefaultGpuTemperatureKey).Hex);
        Assert.Equal("#D5A85B", AccentPalette.Get(AccentPalette.DefaultCpuPowerKey).Hex);
        Assert.Equal("#B98BE7", AccentPalette.Get(AccentPalette.DefaultGpuPowerKey).Hex);
    }

    [Fact]
    public async Task RestartStopsThePreviousPollingLoopBeforeStartingTheNextOne()
    {
        var source = new SerializedSnapshotSource();
        using var service = new SensorPollingService(source, new TestLogger(), 250);

        service.Start();
        await Task.Delay(360);
        service.Restart(500);
        await Task.Delay(360);
        service.Stop();

        Assert.Equal(500, service.IntervalMilliseconds);
        Assert.Equal(1, source.MaximumConcurrentReads);
        Assert.True(source.TotalReads >= 2);
    }

    private sealed class SerializedSnapshotSource : IHardwareSnapshotSource
    {
        private int _activeReads;
        private int _maximumConcurrentReads;
        private int _totalReads;

        public int MaximumConcurrentReads => Volatile.Read(ref _maximumConcurrentReads);

        public int TotalReads => Volatile.Read(ref _totalReads);

        public DashboardSnapshot ReadSnapshot()
        {
            int active = Interlocked.Increment(ref _activeReads);
            Interlocked.Increment(ref _totalReads);
            UpdateMaximum(active);

            Thread.Sleep(80);
            Interlocked.Decrement(ref _activeReads);
            return DashboardSnapshot.Unavailable("TEST");
        }

        private void UpdateMaximum(int active)
        {
            while (true)
            {
                int current = Volatile.Read(ref _maximumConcurrentReads);
                if (current >= active || Interlocked.CompareExchange(ref _maximumConcurrentReads, active, current) == current)
                    return;
            }
        }
    }

    private sealed class TestLogger : IAppLogger
    {
        public string LogFilePath { get; } = Path.Combine(Path.GetTempPath(), "StatsScreen-tests.log");

        public void Info(string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
