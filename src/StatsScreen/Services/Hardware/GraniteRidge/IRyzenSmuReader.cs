namespace StatsScreen.Services.Hardware.GraniteRidge;

// Only telemetry operations are exposed. No general register or command API.
public interface IRyzenSmuReader : IDisposable
{
    uint ResolveVersion();
    byte[] RefreshAndReadTable();
}
