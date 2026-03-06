using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

public sealed class SystemInfoProvider
{
    public SystemMetrics GetSystemMetrics()
    {
        return new SystemMetrics
        {
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.VersionString,
            UptimeSeconds = TimeSpan.FromMilliseconds(Environment.TickCount64).TotalSeconds
        };
    }
}
