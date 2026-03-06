using Microsoft.Extensions.Logging;
using RainmeterCollector.Configuration;
using RainmeterCollector.Models;
using RainmeterCollector.Utils;

namespace RainmeterCollector.Services;

/// <summary>
/// Coordinates polling and snapshot writing on a stable interval.
/// </summary>
public sealed class MetricsCollectorService
{
    private readonly CollectorOptions _options;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly SystemInfoProvider _systemInfoProvider;
    private readonly HardwareSensorService _hardwareSensorService;
    private readonly MemoryMetricsProvider _memoryMetricsProvider;
    private readonly DiskMetricsProvider _diskMetricsProvider;
    private readonly NetworkMetricsProvider _networkMetricsProvider;
    private readonly JsonFileWriter _jsonFileWriter;
    private readonly SensorDebugDumper _sensorDebugDumper;

    public MetricsCollectorService(
        CollectorOptions options,
        ILogger<MetricsCollectorService> logger,
        SystemInfoProvider systemInfoProvider,
        HardwareSensorService hardwareSensorService,
        MemoryMetricsProvider memoryMetricsProvider,
        DiskMetricsProvider diskMetricsProvider,
        NetworkMetricsProvider networkMetricsProvider,
        JsonFileWriter jsonFileWriter,
        SensorDebugDumper sensorDebugDumper)
    {
        _options = options;
        _logger = logger;
        _systemInfoProvider = systemInfoProvider;
        _hardwareSensorService = hardwareSensorService;
        _memoryMetricsProvider = memoryMetricsProvider;
        _diskMetricsProvider = diskMetricsProvider;
        _networkMetricsProvider = networkMetricsProvider;
        _jsonFileWriter = jsonFileWriter;
        _sensorDebugDumper = sensorDebugDumper;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Collector started. Interval: {Interval}ms, Output: {Path}", _options.PollingIntervalMs, _options.OutputPath);

        if (_options.EnableDebugSensorDump)
        {
            _logger.LogInformation("Debug sensor dump is enabled. Path: {Path}", _options.DebugSensorDumpPath);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(250, _options.PollingIntervalMs)));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var (metrics, allSensors) = BuildSnapshot();

                await _jsonFileWriter.WriteAtomicAsync(_options.OutputPath, metrics, cancellationToken);

                if (_options.EnableDebugSensorDump)
                {
                    await _sensorDebugDumper.WriteAsync(_options.DebugSensorDumpPath, allSensors, cancellationToken);
                }

                _logger.LogInformation("Metrics updated at {Timestamp} ({SensorCount} sensors)", metrics.Timestamp, allSensors.Count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect or write metrics this cycle.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private (MetricsSnapshot snapshot, List<SensorReading> allSensors) BuildSnapshot()
    {
        var sensorSnapshot = _hardwareSensorService.CollectSensors(_options.EnableRawSensorDump);

        var snapshot = new MetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            System = _systemInfoProvider.GetSystemMetrics(),
            Cpu = sensorSnapshot.Cpu,
            Gpu = sensorSnapshot.Gpus,
            Memory = _memoryMetricsProvider.GetMemoryMetrics(),
            Disks = _diskMetricsProvider.GetDiskMetrics(sensorSnapshot.AllSensors),
            Network = _networkMetricsProvider.GetNetworkMetrics(),
            Motherboard = sensorSnapshot.Motherboard,
            Fans = sensorSnapshot.Fans,
            AllSensors = _options.EnableRawSensorDump ? sensorSnapshot.AllSensors : []
        };

        return (snapshot, sensorSnapshot.AllSensors);
    }
}
