using LibreHardwareMonitor.Hardware;
using StatsScreen.Models;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Hardware;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly object _sync = new();
    private readonly IAppLogger _logger;
    private Computer? _computer;
    private string? _lastInventorySignature;
    private string? _lastLoggedSelectionSignature;
    private bool _disposed;
    private readonly HashSet<string> _failedHardware = new();

    public HardwareMonitorService(IAppLogger logger)
    {
        _logger = logger;
    }

    public DashboardSnapshot ReadSnapshot()
    {
        lock (_sync)
        {
            if (_disposed)
                return DashboardSnapshot.Unavailable("STOPPED");

            try
            {
                EnsureComputerIsOpen();

                _failedHardware.Clear();
                foreach (IHardware hardware in _computer!.Hardware)
                    UpdateHardwareTree(hardware);

                IReadOnlyList<HardwareSensorDescriptor> descriptors = ReadDescriptors();
                LogInventoryIfChanged();

                DashboardSnapshot snapshot = HardwareSnapshotFactory.CreateSnapshot(
                    descriptors,
                    DateTimeOffset.Now);

                if (_lastInventorySignature is not null &&
                    !string.Equals(_lastLoggedSelectionSignature, _lastInventorySignature, StringComparison.Ordinal))
                {
                    LogSelection(snapshot);
                    _lastLoggedSelectionSignature = _lastInventorySignature;
                }

                return snapshot;
            }
            catch (Exception exception)
            {
                _logger.Error("Hardware polling failed; the dashboard will show N/A until the next retry.", exception);
                return DashboardSnapshot.Unavailable("HARDWARE ERROR");
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                _computer?.Close();
            }
            catch (Exception exception)
            {
                _logger.Error("Libre Hardware Monitor did not close cleanly.", exception);
            }

            _computer = null;
        }
    }

    private void EnsureComputerIsOpen()
    {
        if (_computer is not null)
            return;

        _logger.Info("Opening LibreHardwareMonitorLib with CPU and GPU monitoring enabled.");
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };

        try
        {
            computer.Open();
            _computer = computer;
            _logger.Info("LibreHardwareMonitorLib opened successfully.");
        }
        catch
        {
            try
            {
                computer.Close();
            }
            catch (Exception closeException)
            {
                _logger.Warning($"LibreHardwareMonitorLib cleanup after open failure also failed: {closeException.Message}");
            }

            _computer = null;
            throw;
        }
    }

    private void UpdateHardwareTree(IHardware hardware)
    {
        try
        {
            hardware.Update();
        }
        catch (Exception exception)
        {
            _failedHardware.Add(hardware.Identifier.ToString());
            _logger.Warning($"Unable to update hardware '{hardware.Name}': {exception.Message}");
        }

        foreach (IHardware child in hardware.SubHardware)
            UpdateHardwareTree(child);
    }

    private IReadOnlyList<HardwareSensorDescriptor> ReadDescriptors()
    {
        var descriptors = new List<HardwareSensorDescriptor>();
        int hardwareIndex = 0;

        foreach (IHardware hardware in _computer!.Hardware)
        {
            AddDescriptors(hardware, hardwareIndex, descriptors);
            hardwareIndex++;
        }

        return descriptors;
    }

    private void AddDescriptors(
        IHardware hardware,
        int hardwareIndex,
        ICollection<HardwareSensorDescriptor> descriptors)
    {
        DetectedHardwareType hardwareType = MapHardwareType(hardware.HardwareType);
        if (hardwareType is not DetectedHardwareType.Other)
        {
            string hardwareId = hardware.Identifier.ToString();
            foreach (ISensor sensor in hardware.Sensors)
            {
                DetectedSensorType sensorType = sensor.SensorType switch
                {
                    SensorType.Temperature => DetectedSensorType.Temperature,
                    SensorType.Power => DetectedSensorType.Power,
                    _ => DetectedSensorType.Other
                };

                if (sensorType is DetectedSensorType.Temperature or DetectedSensorType.Power)
                {
                    descriptors.Add(new HardwareSensorDescriptor(
                        hardwareId,
                        hardware.Name,
                        hardwareType,
                        hardwareIndex,
                        sensor.Name,
                        sensorType,
                        _failedHardware.Contains(hardwareId) ? null : sensor.Value));
                }
            }
        }

        foreach (IHardware child in hardware.SubHardware)
            AddDescriptors(child, hardwareIndex, descriptors);
    }

    private void LogInventoryIfChanged()
    {
        IReadOnlyList<string> signatureLines = BuildInventoryLines(includeValues: false);
        string signature = string.Join("|", signatureLines);

        if (string.Equals(signature, _lastInventorySignature, StringComparison.Ordinal))
            return;

        _lastInventorySignature = signature;
        _logger.Info("Detected hardware and available sensor names:");
        foreach (string line in BuildInventoryLines(includeValues: true))
            _logger.Info(line);
    }

    private void LogSelection(DashboardSnapshot snapshot)
    {
        _logger.Info(
            $"Selected sensors: CPU temp={DisplaySource(snapshot.CpuTemperature.Source)}; " +
            $"GPU temp={DisplaySource(snapshot.GpuTemperature.Source)}; " +
            $"CPU power={DisplaySource(snapshot.CpuPower.Source)}; " +
            $"GPU power={DisplaySource(snapshot.GpuPower.Source)}.");
    }

    private IReadOnlyList<string> BuildInventoryLines(bool includeValues)
    {
        var lines = new List<string>();
        foreach (IHardware hardware in _computer!.Hardware)
            AppendInventoryLines(hardware, lines, string.Empty, includeValues);

        return lines;
    }

    private static void AppendInventoryLines(
        IHardware hardware,
        ICollection<string> lines,
        string indent,
        bool includeValues)
    {
        lines.Add($"{indent}{hardware.HardwareType}: {hardware.Name} ({hardware.Identifier})");
        foreach (ISensor sensor in hardware.Sensors.OrderBy(sensor => sensor.SensorType).ThenBy(sensor => sensor.Name))
        {
            string value = includeValues ? $" = {sensor.Value?.ToString() ?? "N/A"}" : string.Empty;
            lines.Add($"{indent}  {sensor.SensorType}: {sensor.Name}{value}");
        }

        foreach (IHardware child in hardware.SubHardware)
            AppendInventoryLines(child, lines, indent + "  ", includeValues);
    }

    private static string DisplaySource(string source) =>
        string.IsNullOrWhiteSpace(source) ? "N/A" : source;

    private static DetectedHardwareType MapHardwareType(HardwareType hardwareType) => hardwareType switch
    {
        HardwareType.Cpu => DetectedHardwareType.Cpu,
        HardwareType.GpuNvidia => DetectedHardwareType.GpuNvidia,
        HardwareType.GpuAmd => DetectedHardwareType.GpuAmd,
        HardwareType.GpuIntel => DetectedHardwareType.GpuIntel,
        _ => DetectedHardwareType.Other
    };
}
