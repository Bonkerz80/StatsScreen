namespace StatsScreen.Models;

public sealed class CpuCoreTemperatureSnapshot
{
    private CpuCoreTemperatureSnapshot(IReadOnlyList<double> cores, DateTimeOffset timestamp, string status, uint? version)
    {
        CoreTemperatures = cores;
        Timestamp = timestamp;
        Status = status;
        PmTableVersion = version;
        IsAvailable = cores.Count == 8;
        if (IsAvailable)
        {
            MaxTemperature = cores.Max();
            MaxCoreIndex = Enumerable.Range(0, 8).First(i => cores[i] == MaxTemperature);
            AverageTemperature = cores.Average();
        }
    }

    public IReadOnlyList<double> CoreTemperatures { get; }
    public double? MaxTemperature { get; }
    public int? MaxCoreIndex { get; }
    public double? AverageTemperature { get; }
    public DateTimeOffset Timestamp { get; }
    public bool IsAvailable { get; }
    public string ProviderName => "Experimental Granite Ridge SMU";
    public string Status { get; }
    public uint? PmTableVersion { get; }

    public static CpuCoreTemperatureSnapshot Unavailable(string reason, uint? version = null) =>
        new(Array.Empty<double>(), DateTimeOffset.UtcNow, reason, version);

    public static CpuCoreTemperatureSnapshot Parse(uint version, byte[] table, DateTimeOffset timestamp)
    {
        if (version != 0x620105) return Unavailable("Unsupported PM version", version);
        if (table.Length != 1828) return Unavailable("Unexpected PM table size", version);
        var cores = new double[8];
        for (int i = 0; i < cores.Length; i++)
        {
            float value = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(table.AsSpan(0x4F4 + i * 4, 4));
            if (!float.IsFinite(value) || value <= 0 || value >= 125)
                return Unavailable(FormattableString.Invariant($"Invalid Core {i} temperature at 0x{0x4F4+i*4:X3}: {value}"), version);
            cores[i] = value;
        }
        return new(Array.AsReadOnly(cores), timestamp, "Available", version);
    }
}
