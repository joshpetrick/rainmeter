# Rainmeter PC Stats Skin

This repository contains a Rainmeter skin that displays:

- CPU usage
- RAM usage (used/total and percentage)
- C: drive free/total capacity
- Network download and upload speed
- System uptime
- Predefined stock watchlist prices (3 symbols)
- Current weather summary

## Install

1. Copy the `PCStats` folder to your Rainmeter skins directory (typically `Documents\\Rainmeter\\Skins`).
2. In Rainmeter, click **Refresh all**.
3. Load `PCStats.ini` from the `PCStats` skin.

## Customize

Open `PCStats/PCStats.ini` and edit values in `[Variables]`:

- `FontName`
- `FontColor`
- `AccentColor`
- `Padding`
- `LineHeight`

### Stock list

The skin includes a predefined 3-item watchlist.

- Labels: `Stock1Label`, `Stock2Label`, `Stock3Label`
- Data symbols query: `StockSymbols`

Example:

```ini
Stock1Label=AAPL
Stock2Label=MSFT
Stock3Label=NVDA
StockSymbols=AAPL,MSFT,NVDA
```

### Weather

Set your location using URL-escaped text in `WeatherLocation`.

Examples:

- `WeatherLocation=New%20York`
- `WeatherLocation=London`
- `WeatherLocation=Tokyo`

The weather line is pulled from `wttr.in` and refreshes automatically.

## Notes

- You can change the monitored disk by editing `Drive=C:` in disk measures.
- Stock prices are fetched from Yahoo Finance's public quote endpoint.
