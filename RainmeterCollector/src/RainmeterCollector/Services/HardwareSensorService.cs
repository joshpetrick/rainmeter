using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

/// <summary>
/// Wraps LibreHardwareMonitor and exposes both raw sensor discovery and normalized metrics.
/// </summary>
public sealed class HardwareSensorService : IDisposable
{
    private static readonly Regex CpuCoreRegex = new("core\\s*#?\\s*(\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly Computer _computer;
    private readonly object _sync = new();

    public HardwareSensorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsControllerEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true
        };

        _computer.Open();
    }

    public SensorSnapshot CollectSensors(bool includeRawSensors)
    {
        lock (_sync)
        {
            var sensorReadings = new List<SensorReading>();
            var cpu = new CpuMetrics();
            var motherboard = new MotherboardMetrics();
            var fans = new List<FanMetrics>();
            var gpuMap = new Dictionary<string, GpuMetrics>(StringComparer.OrdinalIgnoreCase);

            foreach (var hardware in _computer.Hardware)
            {
                UpdateHardwareTree(hardware);
                FlattenAndMap(hardware, null, sensorReadings, cpu, gpuMap, motherboard, fans, includeRawSensors);
            }

            SortCpuArrays(cpu);

            return new SensorSnapshot
            {
                Cpu = cpu,
                Gpus = gpuMap.Values.OrderBy(g => g.Name).ToList(),
                Motherboard = motherboard,
                Fans = MergeFans(fans),
                AllSensors = sensorReadings
            };
        }
    }

    private static void UpdateHardwareTree(IHardware hardware)
    {
        try
        {
            hardware.Update();
        }
        catch
        {
            return;
        }

        foreach (var sub in hardware.SubHardware)
        {
            UpdateHardwareTree(sub);
        }
    }

    private static void FlattenAndMap(
        IHardware hardware,
        string? parentName,
        List<SensorReading> target,
        CpuMetrics cpu,
        Dictionary<string, GpuMetrics> gpuMap,
        MotherboardMetrics motherboard,
        List<FanMetrics> fans,
        bool includeRawSensors)
    {
        foreach (var sensor in hardware.Sensors)
        {
            SensorReading reading;
            try
            {
                reading = new SensorReading
                {
                    HardwareName = hardware.Name,
                    HardwareType = hardware.HardwareType.ToString(),
                    ParentHardwareName = parentName,
                    SensorName = sensor.Name,
                    SensorType = sensor.SensorType.ToString(),
                    Index = sensor.Index,
                    Value = sensor.Value,
                    Min = sensor.Min,
                    Max = sensor.Max,
                    Identifier = sensor.Identifier?.ToString()
                };
            }
            catch
            {
                continue;
            }

            target.Add(reading);

            try
            {
                MapSensor(reading, cpu, gpuMap, motherboard, fans, includeRawSensors);
            }
            catch
            {
                // Mapping issues should never break polling.
            }
        }

        foreach (var sub in hardware.SubHardware)
        {
            FlattenAndMap(sub, hardware.Name, target, cpu, gpuMap, motherboard, fans, includeRawSensors);
        }
    }

    private static void MapSensor(
        SensorReading sensor,
        CpuMetrics cpu,
        Dictionary<string, GpuMetrics> gpuMap,
        MotherboardMetrics motherboard,
        List<FanMetrics> fans,
        bool includeRawSensors)
    {
        if (sensor.HardwareType.Contains("Cpu", StringComparison.OrdinalIgnoreCase))
        {
            cpu.Name ??= sensor.HardwareName;
            MapCpuSensor(cpu, sensor);
            if (includeRawSensors)
            {
                cpu.RawSensors.Add(sensor);
            }
        }

        if (sensor.HardwareType.Contains("Gpu", StringComparison.OrdinalIgnoreCase))
        {
            var gpu = GetOrCreateGpu(gpuMap, sensor.HardwareName);
            MapGpuSensor(gpu, sensor);
            if (includeRawSensors)
            {
                gpu.RawSensors.Add(sensor);
            }
        }

        if (sensor.HardwareType.Contains("Motherboard", StringComparison.OrdinalIgnoreCase)
            || sensor.HardwareType.Contains("SuperIO", StringComparison.OrdinalIgnoreCase))
        {
            MapMotherboardSensor(motherboard, sensor);
            if (includeRawSensors)
            {
                motherboard.RawSensors.Add(sensor);
            }
        }

        if (sensor.SensorType.Equals("Fan", StringComparison.OrdinalIgnoreCase))
        {
            fans.Add(new FanMetrics
            {
                Name = BuildCompositeName(sensor.HardwareName, sensor.SensorName),
                Rpm = sensor.Value
            });
        }
    }

    private static void MapCpuSensor(CpuMetrics cpu, SensorReading sensor)
    {
        var name = sensor.SensorName.ToLowerInvariant();
        var type = sensor.SensorType.ToLowerInvariant();

        if (type == "load")
        {
            if (name.Contains("total"))
            {
                cpu.UsageTotalPercent = sensor.Value;
            }
            else if (TryNormalizeCpuCoreName(sensor.SensorName, out var normalizedName))
            {
                UpsertNamedValue(cpu.PerCoreUsagePercent, normalizedName, sensor.Value);
            }
        }

        if (type == "temperature")
        {
            if (name.Contains("package") || name.Contains("cpu package"))
            {
                cpu.PackageTemperatureC = sensor.Value;
            }

            if (TryNormalizeCpuCoreName(sensor.SensorName, out var normalizedCore))
            {
                UpsertNamedValue(cpu.CoreTemperaturesC, normalizedCore, sensor.Value);
            }

            if (cpu.TemperatureC is null || name.Contains("package") || name.Contains("core max"))
            {
                cpu.TemperatureC = sensor.Value;
            }
        }

        if (type == "clock")
        {
            var clockName = TryNormalizeCpuCoreName(sensor.SensorName, out var normalizedCoreClock)
                ? normalizedCoreClock
                : sensor.SensorName;
            UpsertNamedValue(cpu.ClocksMHz, clockName, sensor.Value);
        }

        if (type == "power" && (name.Contains("package") || cpu.PowerWatts is null))
        {
            cpu.PowerWatts = sensor.Value;
        }
    }

    private static bool TryNormalizeCpuCoreName(string sourceName, out string normalized)
    {
        var match = CpuCoreRegex.Match(sourceName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var core))
        {
            normalized = $"core{core}";
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    private static GpuMetrics GetOrCreateGpu(Dictionary<string, GpuMetrics> map, string hardwareName)
    {
        if (!map.TryGetValue(hardwareName, out var gpu))
        {
            gpu = new GpuMetrics
            {
                Name = hardwareName,
                Vendor = DetectVendor(hardwareName)
            };
            map[hardwareName] = gpu;
        }

        return gpu;
    }

    private static void MapGpuSensor(GpuMetrics gpu, SensorReading sensor)
    {
        var name = sensor.SensorName.ToLowerInvariant();
        var type = sensor.SensorType.ToLowerInvariant();

        if (type == "load")
        {
            if (name.Contains("core") || name == "gpu core" || name == "gpu")
            {
                gpu.CoreUsagePercent = sensor.Value;
                gpu.UsagePercent ??= sensor.Value;
            }
            else if (name.Contains("memory"))
            {
                gpu.MemoryUsagePercent = sensor.Value;
            }
        }

        if (type == "temperature" && (name.Contains("gpu core") || name.Contains("core") || gpu.TemperatureC is null))
        {
            gpu.TemperatureC = sensor.Value;
        }

        if (type is "smalldata" or "data")
        {
            if (name.Contains("dedicated memory used") || name.Contains("memory used"))
            {
                gpu.MemoryUsedMB = sensor.Value;
            }
            else if (name.Contains("dedicated memory total") || name.Contains("memory total"))
            {
                gpu.MemoryTotalMB = sensor.Value;
            }
        }

        if (type == "clock")
        {
            if (name.Contains("gpu core") || (name.Contains("core") && !name.Contains("memory")))
            {
                gpu.CoreClockMHz = sensor.Value;
            }
            else if (name.Contains("memory"))
            {
                gpu.MemoryClockMHz = sensor.Value;
            }
        }

        if (type == "power")
        {
            gpu.PowerWatts = sensor.Value;
        }

        if (type == "fan")
        {
            gpu.FanRpm = sensor.Value;
        }

        gpu.UsagePercent ??= gpu.CoreUsagePercent;
    }

    private static void MapMotherboardSensor(MotherboardMetrics motherboard, SensorReading sensor)
    {
        if (sensor.SensorType.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
        {
            UpsertNamedValue(motherboard.Temperatures, BuildCompositeName(sensor.HardwareName, sensor.SensorName), sensor.Value);
        }

        if (sensor.SensorType.Equals("Voltage", StringComparison.OrdinalIgnoreCase))
        {
            UpsertNamedValue(motherboard.Voltages, BuildCompositeName(sensor.HardwareName, sensor.SensorName), sensor.Value);
        }
    }

    private static void UpsertNamedValue(List<NamedValue<float?>> list, string name, float? value)
    {
        var existing = list.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            list.Add(new NamedValue<float?> { Name = name, Value = value });
            return;
        }

        existing.Value = value;
    }

    private static void SortCpuArrays(CpuMetrics cpu)
    {
        cpu.PerCoreUsagePercent = cpu.PerCoreUsagePercent.OrderBy(x => x.Name).ToList();
        cpu.CoreTemperaturesC = cpu.CoreTemperaturesC.OrderBy(x => x.Name).ToList();
        cpu.ClocksMHz = cpu.ClocksMHz.OrderBy(x => x.Name).ToList();
    }

    private static string DetectVendor(string hardwareName)
    {
        if (hardwareName.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
        {
            return "NVIDIA";
        }

        if (hardwareName.Contains("amd", StringComparison.OrdinalIgnoreCase) || hardwareName.Contains("radeon", StringComparison.OrdinalIgnoreCase))
        {
            return "AMD";
        }

        if (hardwareName.Contains("intel", StringComparison.OrdinalIgnoreCase))
        {
            return "Intel";
        }

        return "Unknown";
    }

    private static string BuildCompositeName(string hardware, string sensor)
    {
        if (string.IsNullOrWhiteSpace(hardware))
        {
            return sensor;
        }

        return $"{hardware} - {sensor}";
    }

    private static List<FanMetrics> MergeFans(IEnumerable<FanMetrics> fans)
    {
        return fans
            .GroupBy(f => f.Name)
            .Select(g => g.OrderByDescending(x => x.Rpm ?? 0).First())
            .OrderBy(f => f.Name)
            .ToList();
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _computer.Close();
        }
    }
}

public sealed class SensorSnapshot
{
    public CpuMetrics Cpu { get; set; } = new();
    public List<GpuMetrics> Gpus { get; set; } = [];
    public MotherboardMetrics Motherboard { get; set; } = new();
    public List<FanMetrics> Fans { get; set; } = [];
    public List<SensorReading> AllSensors { get; set; } = [];
}
