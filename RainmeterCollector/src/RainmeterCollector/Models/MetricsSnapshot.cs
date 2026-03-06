using System.Text.Json.Serialization;

namespace RainmeterCollector.Models;

public sealed class MetricsSnapshot
{
    public DateTimeOffset Timestamp { get; set; }
    public SystemMetrics System { get; set; } = new();
    public CpuMetrics Cpu { get; set; } = new();
    public List<GpuMetrics> Gpu { get; set; } = [];
    public MemoryMetrics Memory { get; set; } = new();
    public List<DiskMetrics> Disks { get; set; } = [];
    public List<NetworkMetrics> Network { get; set; } = [];
    public MotherboardMetrics Motherboard { get; set; } = new();
    public List<FanMetrics> Fans { get; set; } = [];
    public List<SensorReading> AllSensors { get; set; } = [];
}

public sealed class SystemMetrics
{
    public string MachineName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public double UptimeSeconds { get; set; }
}

public sealed class CpuMetrics
{
    public string? Name { get; set; }
    public float? UsageTotalPercent { get; set; }
    public List<NamedValue<float?>> PerCoreUsagePercent { get; set; } = [];
    public float? TemperatureC { get; set; }
    public float? PackageTemperatureC { get; set; }
    public List<NamedValue<float?>> CoreTemperaturesC { get; set; } = [];
    public List<NamedValue<float?>> ClocksMHz { get; set; } = [];
    public float? PowerWatts { get; set; }
    public List<SensorReading> RawSensors { get; set; } = [];
}

public sealed class GpuMetrics
{
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = "Unknown";
    public float? UsagePercent { get; set; }
    public float? CoreUsagePercent { get; set; }
    public float? MemoryUsagePercent { get; set; }
    public float? TemperatureC { get; set; }
    public float? MemoryUsedMB { get; set; }
    public float? MemoryTotalMB { get; set; }
    public float? CoreClockMHz { get; set; }
    public float? MemoryClockMHz { get; set; }
    public float? PowerWatts { get; set; }
    public float? FanRpm { get; set; }
    public List<SensorReading> RawSensors { get; set; } = [];
}

public sealed class MemoryMetrics
{
    public double TotalMB { get; set; }
    public double UsedMB { get; set; }
    public double AvailableMB { get; set; }
    public double UsagePercent { get; set; }
}

public sealed class DiskMetrics
{
    public string Name { get; set; } = string.Empty;
    public string MountPoint { get; set; } = string.Empty;
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double FreeGB { get; set; }
    public double UsagePercent { get; set; }
    public float? ReadBytesPerSec { get; set; }
    public float? WriteBytesPerSec { get; set; }
    public float? TemperatureC { get; set; }
    public List<NamedValue<float?>> TemperaturesC { get; set; } = [];
    public List<SensorReading> RawSensors { get; set; } = [];
}

public sealed class NetworkMetrics
{
    public string Name { get; set; } = string.Empty;
    public double BytesSentPerSec { get; set; }
    public double BytesReceivedPerSec { get; set; }
    public List<string> IpAddresses { get; set; } = [];
}

public sealed class MotherboardMetrics
{
    public List<NamedValue<float?>> Temperatures { get; set; } = [];
    public List<NamedValue<float?>> Voltages { get; set; } = [];
    public List<SensorReading> RawSensors { get; set; } = [];
}

public sealed class FanMetrics
{
    public string Name { get; set; } = string.Empty;
    public float? Rpm { get; set; }
}

public sealed class NamedValue<T>
{
    public string Name { get; set; } = string.Empty;
    public T? Value { get; set; }
}

public sealed class SensorReading
{
    public string HardwareName { get; set; } = string.Empty;
    public string HardwareType { get; set; } = string.Empty;
    public string? ParentHardwareName { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public int Index { get; set; }
    public float? Value { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier { get; set; }
}
