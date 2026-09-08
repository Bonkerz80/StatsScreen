using System.Text.Json.Serialization;
using StatsScreen.Models;

namespace StatsScreen.Services.Hardware;

[JsonConverter(typeof(JsonStringEnumConverter<CpuTemperatureSource>))]
public enum CpuTemperatureSource { Auto, MaxCore, AverageCore, Core0, Core1, Core2, Core3, Core4, Core5, Core6, Core7, TctlTdie }

public static class CpuTemperatureSelection
{
    public static string Label(CpuTemperatureSource source) => source switch
    {
        CpuTemperatureSource.Auto => "Auto",
        CpuTemperatureSource.MaxCore => "Max Core",
        CpuTemperatureSource.AverageCore => "Average Core",
        CpuTemperatureSource.TctlTdie => "Tctl/Tdie",
        _ => $"Core {(int)source - (int)CpuTemperatureSource.Core0}"
    };

    public static bool IsAvailable(DashboardSnapshot snapshot, CpuTemperatureSource source) => source switch
    {
        CpuTemperatureSource.Auto => true,
        CpuTemperatureSource.TctlTdie => snapshot.TctlTdie?.Value is { } v && double.IsFinite(v) && v > 0 && v < 125,
        _ => Enum.IsDefined(source) && snapshot.CoreTemperatures is { IsAvailable: true } cores &&
            DateTimeOffset.UtcNow - cores.Timestamp < TimeSpan.FromSeconds(15)
    };

    public static DashboardSnapshot Apply(DashboardSnapshot snapshot, CpuTemperatureSource source)
    {
        SensorMetric metric = snapshot.CpuTemperature;
        var cores = snapshot.CoreTemperatures;
        bool available = IsAvailable(snapshot, CpuTemperatureSource.MaxCore);
        if (source == CpuTemperatureSource.TctlTdie && IsAvailable(snapshot, source)) metric = snapshot.TctlTdie!;
        else if (available && cores is not null)
        {
            if (source is CpuTemperatureSource.Auto or CpuTemperatureSource.MaxCore)
                metric = new(cores.MaxTemperature, "°C", $"MAX CORE · CORE {cores.MaxCoreIndex}");
            else if (source == CpuTemperatureSource.AverageCore)
                metric = new(cores.AverageTemperature, "°C", "AVERAGE CORE");
            else if (source >= CpuTemperatureSource.Core0 && source <= CpuTemperatureSource.Core7)
            {
                int index = (int)source - (int)CpuTemperatureSource.Core0;
                metric = new(cores.CoreTemperatures[index], "°C", $"CORE {index}");
            }
        }
        bool any = new[] { metric, snapshot.GpuTemperature, snapshot.CpuPower, snapshot.GpuPower }.Any(m => m.Value.HasValue);
        bool all = new[] { metric, snapshot.GpuTemperature, snapshot.CpuPower, snapshot.GpuPower }.All(m => m.Value.HasValue);
        return snapshot with { CpuTemperature = metric, Status = all ? "" : any ? "SENSOR ERROR" : snapshot.Status, IsHardwareAvailable = any };
    }
}
