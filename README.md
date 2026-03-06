# Rainmeter Skins

This repository includes a modular Rainmeter dashboard focused on a polished, minimal, dark desktop aesthetic.

## Included skins

- `PCStats` (legacy single-panel skin)
- `ProCommandCenter` (modular professional dashboard)

---

## ProCommandCenter

A clean, premium-style, dual-monitor-friendly dashboard with independent modules:

- `SystemStats.ini`
  - CPU usage
  - CPU temperature (HWiNFO)
  - GPU usage (HWiNFO)
  - GPU temperature (HWiNFO)
  - RAM usage
  - Primary drive usage
  - Network throughput
- `SystemSpecs.ini`
  - Static hardware summary (editable)
- `Weather.ini`
  - Temperature, condition, and high/low via `wttr.in`
- `Stocks.ini`
  - Minimal watchlist close prices via Stooq CSV
- `Dashboard.ini`
  - Launcher for loading / unloading modules independently

### Folder structure

```text
ProCommandCenter/
├─ Dashboard.ini
├─ SystemStats.ini
├─ SystemSpecs.ini
├─ Weather.ini
├─ Stocks.ini
└─ @Resources/
   └─ Variables.inc
```

### Install

1. Copy `ProCommandCenter` into `Documents\Rainmeter\Skins`.
2. Refresh Rainmeter.
3. Load `ProCommandCenter\Dashboard.ini`.
4. Use launcher controls (Load / Unload per module) to enable only what you want.

### Required plugin for temperatures and GPU usage

`SystemStats.ini` is prepared for the **HWiNFO Rainmeter Plugin**.

- Plugin: <https://www.hwinfo.com/add-ons/>
- Configure sensor IDs in:
  - `ProCommandCenter/@Resources/Variables.inc`

### Primary customization

Edit `ProCommandCenter/@Resources/Variables.inc` to tune:

- Theme colors and typography
- Spacing and panel dimensions
- Module positions (`PosStatsX`, `PosSpecsX`, etc.)
- Static spec labels (`SpecCPU`, `SpecGPU`, etc.)
- Weather location (`WeatherLocation`)
- Stock symbols (`Stock1Symbol` ... `Stock4Symbol`)

### Notes

- Defaults are tuned for right-side stacking on a dual 1440p layout.
- All modules are independent and can be loaded individually.
- Weather and stock endpoints are lightweight public sources and may be delayed.
