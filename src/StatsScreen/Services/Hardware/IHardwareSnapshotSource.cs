using StatsScreen.Models;

namespace StatsScreen.Services.Hardware;

public interface IHardwareSnapshotSource
{
    DashboardSnapshot ReadSnapshot();
}
