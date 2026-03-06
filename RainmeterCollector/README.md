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


## Run automatically at Windows startup

You have two good options:

### Option A (recommended for personal desktop): Task Scheduler

This is usually the easiest and most reliable for a user-session app.

1. Publish a self-contained executable (optional but convenient):

```powershell
cd RainmeterCollector

dotnet publish .\src\RainmeterCollector\RainmeterCollector.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

2. Open **Task Scheduler** → **Create Task...**
3. **General** tab:
   - Name: `RainmeterCollector`
   - Select **Run whether user is logged on or not** (or only when logged on if preferred)
   - Check **Run with highest privileges**
4. **Triggers** tab:
   - Add trigger **At log on** (or **At startup**)
5. **Actions** tab:
   - Action: **Start a program**
   - Program/script: `C:\Path\To\RainmeterCollector\publish\RainmeterCollector.exe`
   - Add arguments: `--hidden`
   - Start in: `C:\Path\To\RainmeterCollector\publish`
6. **Settings** tab:
   - Enable restart/retry behavior if desired.

This keeps the collector running automatically each boot/logon and is usually enough for Rainmeter dashboards. With `--hidden`, no visible console window is shown to the user.

### Option B (background service): Windows Service

If you want true service behavior, use NSSM (Non-Sucking Service Manager) to host the EXE as a service.

1. Install NSSM.
2. From elevated terminal:

```powershell
nssm install RainmeterCollector "C:\Path\To\RainmeterCollector\publish\RainmeterCollector.exe"
nssm set RainmeterCollector AppDirectory "C:\Path\To\RainmeterCollector\publish"
sc.exe config RainmeterCollector start= auto
sc.exe start RainmeterCollector
```

3. Verify output file updates at `C:\RainmeterCollector\metrics.json`.

> Note: Running as a service can reduce availability of some user-session/GPU sensors depending on driver and permissions. If sensor coverage is lower as a service, prefer Task Scheduler at user logon.

### Minimal no-tools startup option

You can also place a shortcut to the collector EXE in:

`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`

If using this option, create a shortcut that launches with argument `--hidden`.

This is simple but less robust than Task Scheduler.

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
    "DebugSensorDumpPath": "C:\\RainmeterCollector\\sensors-debug.json",
    "RunHidden": false
  }
}
```

## Debug mode

Set `EnableDebugSensorDump` to `true` to generate a sensor discovery file at `DebugSensorDumpPath` every cycle.
Set `RunHidden` to `true` if you always want the process to hide the console window (equivalent to passing `--hidden`).
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
- Non-finite sensor values (`NaN`, `Infinity`) are serialized as `null` to keep JSON valid.
- Mapping rules are centralized in `HardwareSensorService` to make extension easy.
