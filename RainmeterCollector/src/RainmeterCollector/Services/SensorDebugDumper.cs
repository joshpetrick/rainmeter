using System.Text.Json;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

/// <summary>
/// Writes discovered sensors to a dedicated file when debug mode is enabled.
/// </summary>
public sealed class SensorDebugDumper
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task WriteAsync(string outputPath, IReadOnlyList<SensorReading> sensors, CancellationToken cancellationToken)
    {
        var payload = new
        {
            timestamp = DateTimeOffset.UtcNow,
            sensorCount = sensors.Count,
            sensors
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(directory ?? AppContext.BaseDirectory, $".{Path.GetFileName(outputPath)}.tmp");
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);

        if (File.Exists(outputPath))
        {
            File.Replace(tempPath, outputPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, outputPath);
        }
    }
}
