; ==========================================================
; Pro Command Center - Shared Variables
; Edit this file to control theme, spacing, placement, and data labels.
; Scope: all ProCommandCenter module skins.
; ==========================================================

[Variables]
; ---------- Theme (dark / minimal / professional) ----------
; Keep contrasts subtle for a premium desktop look.
ThemeCanvas=18,22,28,0
ThemePanel=27,32,40,220
ThemeBorder=255,255,255,22
ThemeHeaderLine=255,255,255,18
ThemeAccent=138,168,232,255
ThemeText=226,232,240,255
ThemeMuted=150,160,174,255

; ---------- Typography ----------
; Segoe UI Variable is cleaner on modern Windows.
; Rainmeter will fall back to system defaults if unavailable.
FontPrimary=Segoe UI Variable Display
FontNumeric=Segoe UI Semibold
FontSizeTitle=11
FontSizeLabel=9
FontSizeValue=10
FontWeightTitle=600

; ---------- Geometry ----------
PanelWidth=356
PanelRadius=3
PanelPadding=16
HeaderHeight=34
RowHeight=24
LabelColumnWidth=156
BorderThickness=1

; ---------- Placement (dual-monitor defaults) ----------
; Recommended: place stack at right edge of monitor 2 for always-visible glance.
PosStatsX=2940
PosStatsY=78

PosSpecsX=2940
PosSpecsY=384

PosWeatherX=2940
PosWeatherY=626

PosStocksX=2940
PosStocksY=832

; Optional launcher placement (usually monitor 1 corner)
PosDashboardX=72
PosDashboardY=72

; ---------- Static specs panel values ----------
SpecCPU=AMD Ryzen 7 7800X3D
SpecGPU=NVIDIA GeForce RTX 4080 SUPER
SpecRAM=32 GB DDR5
SpecMonitor=2560 x 1440 (x2)
SpecStorage=NVMe 2 TB + SSD 2 TB

; ---------- Dynamic source configuration ----------
; HWiNFO plugin required for CPU/GPU temperatures and GPU usage.
; Plugin download: https://www.hwinfo.com/add-ons/
; Replace IDs below with your own HWiNFO shared-memory sensor IDs.
HWiNFOCpuTempSensorId=0xf0000100
HWiNFOCpuTempEntryId=1
HWiNFOGpuUsageSensorId=0xf0000200
HWiNFOGpuUsageEntryId=2
HWiNFOGpuTempSensorId=0xf0000200
HWiNFOGpuTempEntryId=3

PrimaryDrive=C:

; ---------- Weather ----------
; Keep URL-safe values (space => %20)
WeatherLocation=Seattle
WeatherUrl=https://wttr.in/#WeatherLocation#?format=%t|%C|%m

; ---------- Stocks ----------
; Stooq CSV endpoint: simple delayed close price watchlist.
Stock1Symbol=AAPL
Stock2Symbol=MSFT
Stock3Symbol=NVDA
Stock4Symbol=VOO

Stock1Url=https://stooq.com/q/l/?s=#Stock1Symbol#.us&i=d
Stock2Url=https://stooq.com/q/l/?s=#Stock2Symbol#.us&i=d
Stock3Url=https://stooq.com/q/l/?s=#Stock3Symbol#.us&i=d
Stock4Url=https://stooq.com/q/l/?s=#Stock4Symbol#.us&i=d
