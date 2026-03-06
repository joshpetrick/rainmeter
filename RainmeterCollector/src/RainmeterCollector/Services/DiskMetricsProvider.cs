using System.Diagnostics;
using System.IO;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

public sealed class DiskMetricsProvider : IDisposable
{
    private readonly Dictionary<string, PerformanceCounter> _readCounters = [];
    private readonly Dictionary<string, PerformanceCounter> _writeCounters = [];

    public List<DiskMetrics> GetDiskMetrics(IEnumerable<SensorReading> allSensors)
    {
        var diskMetrics = new List<DiskMetrics>();
        var storageSensors = allSensors
            .Where(s => s.HardwareType.Contains("Storage", StringComparison.OrdinalIgnoreCase)
                     || s.HardwareType.Contains("Hdd", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var totalGb = drive.TotalSize / 1024d / 1024d / 1024d;
            var freeGb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
            var usedGb = Math.Max(0, totalGb - freeGb);
            var key = GetDiskCounterInstanceName(drive.Name);
            var relatedSensors = FilterDiskSensors(storageSensors, drive);
            var tempSensors = relatedSensors
                .Where(s => s.SensorType.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
                .Select(s => new NamedValue<float?> { Name = s.SensorName, Value = s.Value })
                .OrderBy(s => s.Name)
                .ToList();

            diskMetrics.Add(new DiskMetrics
            {
                Name = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name.TrimEnd('\\') : drive.VolumeLabel,
                MountPoint = drive.Name,
                TotalGB = Math.Round(totalGb, 2),
                UsedGB = Math.Round(usedGb, 2),
                FreeGB = Math.Round(freeGb, 2),
                UsagePercent = totalGb > 0 ? Math.Round(usedGb / totalGb * 100d, 2) : 0,
                ReadBytesPerSec = ReadCounterValue(_readCounters, key, "Disk Read Bytes/sec"),
                WriteBytesPerSec = ReadCounterValue(_writeCounters, key, "Disk Write Bytes/sec"),
                TemperatureC = PickPrimaryTemperature(tempSensors),
                TemperaturesC = tempSensors,
                RawSensors = relatedSensors
            });
        }

        return diskMetrics;
    }

    private static float? PickPrimaryTemperature(IReadOnlyList<NamedValue<float?>> temps)
    {
        return temps
            .OrderByDescending(t => t.Name.Contains("composite", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => t.Name.Contains("temperature", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Value)
            .FirstOrDefault();
    }

    private static string GetDiskCounterInstanceName(string driveName)
    {
        var letter = driveName.TrimEnd('\\').TrimEnd(':');
        return $"{letter}:";
    }

    private static List<SensorReading> FilterDiskSensors(IEnumerable<SensorReading> sensors, DriveInfo drive)
    {
        var mountToken = drive.Name[..1];
        var label = drive.VolumeLabel;

        var matches = sensors
            .Where(s => s.HardwareName.Contains(mountToken, StringComparison.OrdinalIgnoreCase)
                     || (!string.IsNullOrWhiteSpace(label) && s.HardwareName.Contains(label, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matches.Count > 0)
        {
            return matches;
        }

        // Fall back to all storage sensors when direct mapping is unavailable.
        return sensors.ToList();
    }

    private static float? ReadCounterValue(Dictionary<string, PerformanceCounter> cache, string instance, string counterName)
    {
        try
        {
            if (!cache.TryGetValue(instance, out var counter))
            {
                counter = new PerformanceCounter("LogicalDisk", counterName, instance, readOnly: true);
                _ = counter.NextValue();
                cache[instance] = counter;
                return 0;
            }

            return counter.NextValue();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var counter in _readCounters.Values)
        {
            counter.Dispose();
        }

        foreach (var counter in _writeCounters.Values)
        {
            counter.Dispose();
        }
    }
}
