using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RainmeterCollector.Configuration;
using RainmeterCollector.Services;
using RainmeterCollector.Utils;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "RAINMETER_COLLECTOR_")
    .Build();

var options = configuration.GetSection("Collector").Get<CollectorOptions>() ?? new CollectorOptions();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    if (options.EnableConsoleLogging)
    {
        builder
            .SetMinimumLevel(LogLevel.Information)
            .AddSimpleConsole(c =>
            {
                c.SingleLine = true;
                c.TimestampFormat = "HH:mm:ss ";
            });
    }
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Rainmeter hardware collector starting...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var hardwareService = new HardwareSensorService();
using var diskService = new DiskMetricsProvider();

var collector = new MetricsCollectorService(
    options,
    loggerFactory.CreateLogger<MetricsCollectorService>(),
    new SystemInfoProvider(),
    hardwareService,
    new MemoryMetricsProvider(),
    diskService,
    new NetworkMetricsProvider(),
    new JsonFileWriter(),
    new SensorDebugDumper());

try
{
    await collector.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Collector stopped.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Collector crashed unexpectedly.");
    Environment.ExitCode = 1;
}
