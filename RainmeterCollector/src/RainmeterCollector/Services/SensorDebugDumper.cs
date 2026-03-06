using System.Text.Json;
using RainmeterCollector.Models;
using RainmeterCollector.Utils;

namespace RainmeterCollector.Services;

/// <summary>
/// Writes discovered sensors to a dedicated file when debug mode is enabled.
/// </summary>
public sealed class SensorDebugDumper
{
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        options.Converters.Add(new NullableFloatNonFiniteToNullConverter());
        options.Converters.Add(new NullableDoubleNonFiniteToNullConverter());
        options.Converters.Add(new FloatNonFiniteToNullConverter());
        options.Converters.Add(new DoubleNonFiniteToNullConverter());

        return options;
    }

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
