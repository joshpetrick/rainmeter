using Microsoft.VisualBasic.Devices;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

public sealed class MemoryMetricsProvider
{
    public MemoryMetrics GetMemoryMetrics()
    {
        var computerInfo = new ComputerInfo();
        var totalMb = computerInfo.TotalPhysicalMemory / 1024d / 1024d;
        var availableMb = computerInfo.AvailablePhysicalMemory / 1024d / 1024d;
        var usedMb = Math.Max(0, totalMb - availableMb);

        return new MemoryMetrics
        {
            TotalMB = Math.Round(totalMb, 2),
            AvailableMB = Math.Round(availableMb, 2),
            UsedMB = Math.Round(usedMb, 2),
            UsagePercent = totalMb > 0 ? Math.Round(usedMb / totalMb * 100d, 2) : 0
        };
    }
}
