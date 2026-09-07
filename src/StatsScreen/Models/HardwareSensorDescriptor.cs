namespace StatsScreen.Models;

public enum DetectedHardwareType
{
    Other,
    Cpu,
    GpuNvidia,
    GpuAmd,
    GpuIntel
}

public enum DetectedSensorType
{
    Other,
    Temperature,
    Power
}

/// <summary>
/// A small application-level representation of a Libre Hardware Monitor sensor.
/// Keeping this separate from the library types makes sensor selection easy to test.
/// </summary>
public sealed record HardwareSensorDescriptor(
    string HardwareId,
    string HardwareName,
    DetectedHardwareType HardwareType,
    int HardwareIndex,
    string SensorName,
    DetectedSensorType SensorType,
    float? Value);
