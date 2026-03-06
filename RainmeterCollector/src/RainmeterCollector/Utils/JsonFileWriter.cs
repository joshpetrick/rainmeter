using System.Text.Json;

namespace RainmeterCollector.Utils;

/// <summary>
/// Writes JSON payloads atomically so readers do not observe partial files.
/// </summary>
public sealed class JsonFileWriter
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task WriteAtomicAsync<T>(string outputPath, T payload, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFile = Path.Combine(directory ?? AppContext.BaseDirectory, $".{Path.GetFileName(outputPath)}.tmp");
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        await File.WriteAllTextAsync(tempFile, json, cancellationToken);

        if (File.Exists(outputPath))
        {
            File.Replace(tempFile, outputPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempFile, outputPath);
        }
    }
}
