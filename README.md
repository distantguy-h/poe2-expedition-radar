# PoE2 Expedition Radar

Rune Monolith Intelligence scanner for Path of Exile 2.

## Demo

<video src="https://github.com/distantguy-h/poe2-expedition-radar/releases/download/v1.0.0/POE2.Expedition.Radar.mp4" controls width="100%"></video>

## Features

- Real-time expedition/rune monolith scanning
- Reward value calculation (Exalted / Divine)
- Filtering by expedition, slot count, and search
- Auto-detect game process

## Requirements

- Windows 10/11 x64
- .NET 9.0 SDK (to build from source)
- Path of Exile 2 running

## Build from source

```bash
dotnet publish PoE2ExpeditionScanner.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:IncludeAllContentForSelfExtract=true -o publish
```

Or simply run `build.bat`.

## Download

Pre-built exe available in [Releases](../../releases).

## Usage

1. Run `PoE2ExpeditionScanner.exe` as Administrator
2. Open Path of Exile 2
3. Click **SCAN** to start scanning
