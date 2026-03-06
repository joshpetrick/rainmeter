using System.Runtime.InteropServices;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

public sealed class MemoryMetricsProvider
{
    public MemoryMetrics GetMemoryMetrics()
    {
        var memoryStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(ref memoryStatus))
        {
            return new MemoryMetrics();
        }

        var totalMb = memoryStatus.ullTotalPhys / 1024d / 1024d;
        var availableMb = memoryStatus.ullAvailPhys / 1024d / 1024d;
        var usedMb = Math.Max(0, totalMb - availableMb);

        return new MemoryMetrics
        {
            TotalMB = Math.Round(totalMb, 2),
            AvailableMB = Math.Round(availableMb, 2),
            UsedMB = Math.Round(usedMb, 2),
            UsagePercent = totalMb > 0 ? Math.Round(usedMb / totalMb * 100d, 2) : 0
        };
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MemoryStatusEx()
        {
            dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
        }
    }
}
