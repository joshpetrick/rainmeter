namespace RainmeterCollector.Configuration;

public sealed class CollectorOptions
{
    public string OutputPath { get; set; } = @"C:\RainmeterCollector\metrics.json";
    public int PollingIntervalMs { get; set; } = 2000;
    public bool EnableRawSensorDump { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableDebugSensorDump { get; set; } = false;
    public string DebugSensorDumpPath { get; set; } = @"C:\RainmeterCollector\sensors-debug.json";
}
