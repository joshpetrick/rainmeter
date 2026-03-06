# RainmeterCollector (Windows hardware metrics collector, .NET 10 target)

RainmeterCollector is a lightweight .NET 10 console app for Windows 10/11 that polls hardware and OS metrics and writes a JSON file Rainmeter can consume.

## What it collects

- System metadata (machine name, OS, uptime)
- CPU usage, per-core load, package/overall temperatures, per-core temperatures, clocks, power (when available)
- GPU usage (overall/core/memory), temperatures, clocks, VRAM usage, power, fan RPM (when available)
- RAM total/used/free
- Disk capacity per drive, read/write throughput, temperatures when available
- Network throughput per adapter + IP addresses
- Motherboard temperatures/voltages
- Fan RPM
- Optional full raw sensor dump (`allSensors`) from LibreHardwareMonitor

## Architecture

- `Program.cs`: startup, config loading, logging, graceful shutdown.
- `MetricsCollectorService`: resilient polling loop and snapshot orchestration.
- Providers per concern (`SystemInfoProvider`, `MemoryMetricsProvider`, `DiskMetricsProvider`, `NetworkMetricsProvider`).
- `HardwareSensorService`: recursively enumerates LibreHardwareMonitor hardware/subhardware, maps normalized fields, and preserves raw sensors.
- `JsonFileWriter`: atomic write (`.tmp` + replace) to avoid partial reads by Rainmeter.
- `SensorDebugDumper`: optional debug output of every discovered sensor.
- Strongly typed JSON models in `Models/`.


## Prerequisite

- .NET SDK 10.x (project targets `net10.0-windows`)

## Build

```powershell
cd RainmeterCollector
# Restore packages
dotnet restore .\src\RainmeterCollector\RainmeterCollector.csproj
# Build release (targets net10.0-windows)
dotnet build .\src\RainmeterCollector\RainmeterCollector.csproj -c Release
```

## Run

```powershell
cd RainmeterCollector\src\RainmeterCollector
dotnet run -c Release
```

Default output path:

`C:\RainmeterCollector\metrics.json`

## Configuration

Edit `src/RainmeterCollector/appsettings.json`:

```json
{
  "Collector": {
    "OutputPath": "C:\\RainmeterCollector\\metrics.json",
    "PollingIntervalMs": 2000,
    "EnableRawSensorDump": true,
    "EnableConsoleLogging": true,
    "EnableDebugSensorDump": false,
    "DebugSensorDumpPath": "C:\\RainmeterCollector\\sensors-debug.json"
  }
}
```

## Debug mode

Set `EnableDebugSensorDump` to `true` to generate a sensor discovery file at `DebugSensorDumpPath` every cycle.
This file includes each sensor's hardware name/type, sensor name/type, current value, and min/max values.

## Rainmeter integration idea

For a future Rainmeter skin/module:

- Use a plugin/script that can parse JSON file values.
- Point the measure at `C:\RainmeterCollector\metrics.json`.
- Prefer stable fields such as:
  - `cpu.usageTotalPercent`
  - `cpu.perCoreUsagePercent[0].value`
  - `gpu[0].coreUsagePercent`
  - `gpu[0].temperatureC`
  - `memory.usagePercent`
  - `network[0].bytesReceivedPerSec`

## Notes

- Sensor availability depends on motherboard, drivers, and privileges.
- Missing sensors are handled gracefully (`null`/omitted behavior).
- Mapping rules are centralized in `HardwareSensorService` to make extension easy.
