using System.Globalization;
using StatsScreen.Models;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Hardware.GraniteRidge;

public sealed class GraniteRidgeTemperatureProvider : IDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private readonly Func<CpuIdentity> _detect;
    private readonly Func<IRyzenSmuReader> _createReader;
    private readonly IAppLogger _logger;
    private readonly bool _diagnostic;
    private readonly Func<DateTimeOffset> _utcNow;
    private IRyzenSmuReader? _reader;
    private bool _started, _permanentlyDisabled, _disposed, _loggedValid, _warned;
    private DateTimeOffset? _retryAfterUtc;
    private uint? _version;
    private string _reason = "Not initialized";

    public GraniteRidgeTemperatureProvider(IAppLogger logger, bool diagnostic = false,
        Func<CpuIdentity>? detect = null, Func<IRyzenSmuReader>? createReader = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _logger = logger; _diagnostic = diagnostic;
        _detect = detect ?? CpuIdentity.Detect;
        _createReader = createReader ?? (() => new PawnIoSmuReader());
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public CpuCoreTemperatureSnapshot Read(double? aggregate)
    {
        if (_disposed) return CpuCoreTemperatureSnapshot.Unavailable("Provider disposed", _version);
        if (_permanentlyDisabled) return CpuCoreTemperatureSnapshot.Unavailable(_reason, _version);
        if (_retryAfterUtc is { } retryAfter && _utcNow() < retryAfter)
            return CpuCoreTemperatureSnapshot.Unavailable(_reason, _version);

        try
        {
            if (!_started)
            {
                CpuIdentity cpu = _detect();
                _started = true;
                _logger.Info($"Granite Ridge CPU: {cpu.Name}; vendor={cpu.Vendor}; family=0x{cpu.Family:X}; model=0x{cpu.Model:X}; cores={cpu.Cores}; packages={cpu.Packages}; supported={cpu.IsSupported}; expected PM=0x620105.");
                if (!cpu.IsSupported) return Disable("Unsupported CPU configuration");
            }

            if (_reader is null)
            {
                _reader = _createReader();
                _logger.Info("Granite Ridge PawnIO signed module access succeeded.");
            }

            // Recheck before every transfer, including after sleep/resume. Unknown layouts are never read.
            uint version = _reader!.ResolveVersion();
            if (_version != version) _logger.Info($"Granite Ridge PM table version 0x{version:X8} detected; expected 0x00620105.");
            _version = version;
            if (version != 0x620105) return Disable($"Granite Ridge PM table version 0x{version:X8} is not supported");
            var snapshot = CpuCoreTemperatureSnapshot.Parse(version, _reader.RefreshAndReadTable(), DateTimeOffset.UtcNow);
            if (!snapshot.IsAvailable) return Retry(snapshot.Status);
            bool recovered = _retryAfterUtc is not null;
            _retryAfterUtc = null;
            _reason = "Available";
            if (recovered) _logger.Info("Granite Ridge telemetry recovered automatically after a transient failure.");
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
        catch (Exception exception) { return Retry($"Granite Ridge initialization/telemetry unavailable: {exception.Message}"); }
    }

    private CpuCoreTemperatureSnapshot Disable(string reason)
    {
        _permanentlyDisabled = true; _reason = reason;
        _logger.Warning($"{reason}; using LibreHardwareMonitor fallback. Custom provider disabled until restart.");
        DisposeReader();
        return CpuCoreTemperatureSnapshot.Unavailable(reason, _version);
    }

    private CpuCoreTemperatureSnapshot Retry(string reason)
    {
        _reason = reason;
        _retryAfterUtc = _utcNow().Add(RetryInterval);
        _logger.Warning($"{reason}; using LibreHardwareMonitor fallback. The experimental provider will retry automatically in 30 seconds.");
        DisposeReader();
        return CpuCoreTemperatureSnapshot.Unavailable(reason, _version);
    }

    private void DisposeReader()
    {
        IRyzenSmuReader? reader = _reader;
        _reader = null;
        if (reader is null) return;
        try { reader.Dispose(); }
        catch (Exception exception) { _logger.Warning($"Granite Ridge reader cleanup failed: {exception.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeReader();
    }
}
