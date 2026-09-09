using StatsScreen.Models;
using StatsScreen.Services.Hardware;
using StatsScreen.Services.Hardware.GraniteRidge;
using StatsScreen.Services.Logging;
using StatsScreen.Services.Settings;
using Xunit;

namespace StatsScreen.Tests;

public sealed class GraniteRidgeTests
{
    private static readonly CpuIdentity Supported = new("AMD Ryzen 7 9800X3D 8-Core Processor", "AuthenticAMD", 0x1A, 0x44, 8, 1);
    private static readonly float[] Values = [61.2f, 60.4f, 64.8f, 62, 68.3f, 63.2f, 59.8f, 61.9f];
    private static byte[] Table()
    {
        byte[] data = new byte[1828];
        for (int i = 0; i < 8; i++) BitConverter.GetBytes(Values[i]).CopyTo(data, 1268 + 4 * i);
        return data;
    }
    private static CpuCoreTemperatureSnapshot Valid() => CpuCoreTemperatureSnapshot.Parse(0x620105, Table(), DateTimeOffset.UtcNow);
    private static DashboardSnapshot Dashboard(CpuCoreTemperatureSnapshot? cores = null) => new(
        new(75, "°C", "CPU / Tctl/Tdie"), new(45, "°C", "GPU"), new(30, "W", "CPU"), new(60, "W", "GPU"),
        DateTimeOffset.UtcNow, "", true) { CoreTemperatures = cores, TctlTdie = new(75, "°C", "Tctl/Tdie") };

    [Fact]
    public void KnownLayoutExtractsAllEightValuesAndComputesStatistics()
    {
        var result = Valid();
        Assert.True(result.IsAvailable);
        Assert.Equal(Values.Select(v => (double)v), result.CoreTemperatures);
        Assert.Equal(4, result.MaxCoreIndex);
        Assert.Equal((double)Values[4], result.MaxTemperature);
        Assert.Equal(Values.Select(v => (double)v).Average(), result.AverageTemperature);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)result.CoreTemperatures)[0] = 1);
    }

    [Theory]
    [InlineData(0u)] [InlineData(0x620205u)] [InlineData(0x620106u)]
    public void UnknownVersionsRejected(uint version) => Assert.False(CpuCoreTemperatureSnapshot.Parse(version, Table(), DateTimeOffset.UtcNow).IsAvailable);

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(float.NegativeInfinity)]
    [InlineData(-273f)] [InlineData(65535f)] [InlineData(0f)] [InlineData(125f)]
    public void InvalidCoreRejectsEntireSnapshot(float value)
    {
        byte[] table = Table();
        BitConverter.GetBytes(value).CopyTo(table, 0x510);
        var result = CpuCoreTemperatureSnapshot.Parse(0x620105, table, DateTimeOffset.UtcNow);
        Assert.False(result.IsAvailable);
        Assert.Empty(result.CoreTemperatures);
        Assert.Null(result.MaxTemperature);
        Assert.Null(result.AverageTemperature);
    }

    [Theory]
    [InlineData(0)] [InlineData(1296)] [InlineData(1827)] [InlineData(1832)]
    public void UnexpectedSizesRejected(int length) => Assert.False(CpuCoreTemperatureSnapshot.Parse(0x620105, new byte[length], DateTimeOffset.UtcNow).IsAvailable);

    [Fact]
    public void AutoPrefersActualMax() => Assert.Equal((double)Values[4], CpuTemperatureSelection.Apply(Dashboard(Valid()), CpuTemperatureSource.Auto).CpuTemperature.Value);

    [Fact]
    public void AverageUsesAllEight() => Assert.Equal(Valid().AverageTemperature, CpuTemperatureSelection.Apply(Dashboard(Valid()), CpuTemperatureSource.AverageCore).CpuTemperature.Value);

    [Theory]
    [InlineData(CpuTemperatureSource.Auto)] [InlineData(CpuTemperatureSource.MaxCore)] [InlineData(CpuTemperatureSource.Core7)]
    [InlineData(CpuTemperatureSource.AverageCore)]
    public void UnavailableFallsBackWithHonestLabel(CpuTemperatureSource source)
    {
        var snapshot = Dashboard();
        Assert.Equal(snapshot.CpuTemperature, CpuTemperatureSelection.Apply(snapshot, source).CpuTemperature);
        if (source != CpuTemperatureSource.Auto) Assert.False(CpuTemperatureSelection.IsAvailable(snapshot, source));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void ExplicitCoreWorks(int core)
    {
        var result = CpuTemperatureSelection.Apply(Dashboard(Valid()), (CpuTemperatureSource)((int)CpuTemperatureSource.Core0 + core));
        Assert.Equal((double)Values[core], result.CpuTemperature.Value);
        Assert.Equal($"CORE {core}", result.CpuTemperature.Source);
    }

    [Fact]
    public void TctlStaysSeparateFromCores() => Assert.Equal(75, CpuTemperatureSelection.Apply(Dashboard(Valid()), CpuTemperatureSource.TctlTdie).CpuTemperature.Value);

    [Fact]
    public void ZeroTctlIsNotOfferedAsAvailable()
    {
        var snapshot = Dashboard() with { TctlTdie = new(0, "°C", "Tctl/Tdie") };
        Assert.False(CpuTemperatureSelection.IsAvailable(snapshot, CpuTemperatureSource.TctlTdie));
        Assert.Equal(snapshot.CpuTemperature, CpuTemperatureSelection.Apply(snapshot, CpuTemperatureSource.TctlTdie).CpuTemperature);
    }

    [Fact]
    public void StaleSnapshotFallsBack()
    {
        var cores = CpuCoreTemperatureSnapshot.Parse(0x620105, Table(), DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal(75, CpuTemperatureSelection.Apply(Dashboard(cores), CpuTemperatureSource.Auto).CpuTemperature.Value);
    }

    [Fact]
    public void UnknownVersionNeverReadsTable()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using var reader = new Reader { Version = 0x620205 };
        using var provider = new GraniteRidgeTemperatureProvider(new Logger(), detect: () => Supported,
            createReader: () => reader, utcNow: () => now);
        Assert.False(provider.Read(70).IsAvailable);
        Assert.Equal(0, reader.Reads);
        now = now.AddMinutes(1);
        Assert.False(provider.Read(70).IsAvailable);
        Assert.Equal(1, reader.Resolves);
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public void VersionChangeFailsClosedBeforeNextRead()
    {
        using var reader = new Reader();
        using var provider = new GraniteRidgeTemperatureProvider(new Logger(), detect: () => Supported, createReader: () => reader);
        Assert.True(provider.Read(70).IsAvailable);
        reader.Version = 0x620205;
        Assert.False(provider.Read(70).IsAvailable);
        Assert.Equal(1, reader.Reads);
    }

    [Fact]
    public void UnsupportedCpuDoesNotOpenDriver()
    {
        using var provider = new GraniteRidgeTemperatureProvider(new Logger(), detect: () => Supported with { Cores = 6 }, createReader: () => throw new Exception("Must not open"));
        Assert.Contains("Unsupported CPU", provider.Read(70).Status);
    }

    [Fact]
    public void IdentityRequiresExactSkuFamilyModelAndTopology()
    {
        Assert.True(Supported.IsSupported);
        Assert.False((Supported with { Name = "AMD Ryzen 9 9950X3D" }).IsSupported);
        Assert.False((Supported with { Family = 0x19 }).IsSupported);
        Assert.False((Supported with { Model = 0x45 }).IsSupported);
        Assert.False((Supported with { Vendor = "GenuineIntel" }).IsSupported);
        Assert.False((Supported with { Packages = 2 }).IsSupported);
    }

    [Fact]
    public void ReaderFailureRetriesAfterCooldownAndDisposes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var firstReader = new Reader { Fail = true };
        var secondReader = new Reader();
        int creations = 0;
        using var provider = new GraniteRidgeTemperatureProvider(new Logger(), detect: () => Supported,
            createReader: () => ++creations == 1 ? firstReader : secondReader, utcNow: () => now);
        Assert.False(provider.Read(70).IsAvailable);
        Assert.True(firstReader.Disposed);
        Assert.False(provider.Read(70).IsAvailable);
        Assert.Equal(1, creations);
        now = now.AddSeconds(29);
        Assert.False(provider.Read(70).IsAvailable);
        Assert.Equal(1, creations);
        now = now.AddSeconds(1);
        Assert.True(provider.Read(70).IsAvailable);
        Assert.Equal(2, creations);
        Assert.Equal(1, firstReader.Reads);
        Assert.Equal(1, secondReader.Reads);
    }

    [Fact]
    public void SelectionRoundTripsAndClonePreservesIt()
    {
        string directory = Path.Combine(Path.GetTempPath(), "StatsScreen-tests-" + Guid.NewGuid());
        try
        {
            var service = new SettingsService(new Logger(), directory);
            var settings = new AppSettings { CpuTemperatureSource = CpuTemperatureSource.Core4 };
            service.Save(settings.Clone());
            Assert.Equal(CpuTemperatureSource.Core4, service.Load().CpuTemperatureSource);
            settings.CpuTemperatureSource = (CpuTemperatureSource)999;
            settings.Normalize();
            Assert.Equal(CpuTemperatureSource.Auto, settings.CpuTemperatureSource);
            Assert.Equal(CpuTemperatureSource.Auto, new AppSettings().CpuTemperatureSource);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private sealed class Reader : IRyzenSmuReader
    {
        public uint Version = 0x620105;
        public int Reads, Resolves;
        public bool Fail, Disposed;
        public uint ResolveVersion() { Resolves++; return Version; }
        public byte[] RefreshAndReadTable() { Reads++; if (Fail) throw new IOException("Access lost"); return Table(); }
        public void Dispose() => Disposed = true;
    }
    private sealed class Logger : IAppLogger
    {
        public string LogFilePath => "";
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}
