using System.Globalization;
using StatsScreen.Models;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Hardware.GraniteRidge;

public sealed class GraniteRidgeTemperatureProvider : IDisposable
{
    private readonly Func<CpuIdentity> _detect;
    private readonly Func<IRyzenSmuReader> _createReader;
    private readonly IAppLogger _logger;
    private readonly bool _diagnostic;
    private IRyzenSmuReader? _reader;
    private bool _started, _disabled, _loggedValid, _warned;
    private uint? _version;
    private string _reason = "Not initialized";

    public GraniteRidgeTemperatureProvider(IAppLogger logger, bool diagnostic = false,
        Func<CpuIdentity>? detect = null, Func<IRyzenSmuReader>? createReader = null)
    {
        _logger = logger; _diagnostic = diagnostic;
        _detect = detect ?? CpuIdentity.Detect;
        _createReader = createReader ?? (() => new PawnIoSmuReader());
    }

    public CpuCoreTemperatureSnapshot Read(double? aggregate)
    {
        if (_disabled) return CpuCoreTemperatureSnapshot.Unavailable(_reason, _version);
        try
        {
            if (!_started)
            {
                _started = true;
                CpuIdentity cpu = _detect();
                _logger.Info($"Granite Ridge CPU: {cpu.Name}; vendor={cpu.Vendor}; family=0x{cpu.Family:X}; model=0x{cpu.Model:X}; cores={cpu.Cores}; packages={cpu.Packages}; supported={cpu.IsSupported}; expected PM=0x620105.");
                if (!cpu.IsSupported) return Disable("Unsupported CPU configuration");
                _reader = _createReader();
                _logger.Info("Granite Ridge PawnIO signed module access succeeded.");
            }
            // Recheck before every transfer, including after sleep/resume. Unknown layouts are never read.
            uint version = _reader!.ResolveVersion();
            if (_version != version) _logger.Info($"Granite Ridge PM table version 0x{version:X8} detected; expected 0x00620105.");
            _version = version;
            if (version != 0x620105) return Disable($"Granite Ridge PM table version 0x{version:X8} is not supported");
            var snapshot = CpuCoreTemperatureSnapshot.Parse(version, _reader.RefreshAndReadTable(), DateTimeOffset.UtcNow);
            if (!snapshot.IsAvailable) return Disable(snapshot.Status);
            if (!_loggedValid || _diagnostic)
            {
                _logger.Info("Granite Ridge PM table version 0x620105 recognised. Ryzen SMU core temps: " +
                    string.Join("; ", snapshot.CoreTemperatures.Select((v, i) => FormattableString.Invariant($"Core{i}={v:F1} [0x{0x4F4+i*4:X3}]"))) +
                    FormattableString.Invariant($"; Max={snapshot.MaxTemperature:F1} (Core{snapshot.MaxCoreIndex}); Average={snapshot.AverageTemperature:F1}; LHM comparison={aggregate?.ToString("F1", CultureInfo.InvariantCulture) ?? "N/A"}."));
                _loggedValid = true;
            }
            if (!_warned && aggregate is { } t && snapshot.CoreTemperatures.All(v => t - v > 40))
            {
                _logger.Warning("All SMU cores are over 40 °C below the LHM aggregate; investigate sampling/telemetry consistency.");
                _warned = true;
            }
            return snapshot;
        }
        catch (Exception exception) { return Disable($"Granite Ridge initialization/telemetry unavailable: {exception.Message}"); }
    }

    private CpuCoreTemperatureSnapshot Disable(string reason)
    {
        _disabled = true; _reason = reason;
        _logger.Warning($"{reason}; using LibreHardwareMonitor fallback. Custom provider disabled until restart.");
        Dispose();
        return CpuCoreTemperatureSnapshot.Unavailable(reason, _version);
    }
    public void Dispose() { _reader?.Dispose(); _reader = null; _disabled = true; }
}
