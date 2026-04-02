# SkyMaster ATC

SkyMaster ATC is a standalone ATC client for controlling AI traffic in Lockheed Martin Prepar3D (P3D) and Microsoft Flight Simulator X (FSX) using SimConnect.

> **Phases 1–5 complete:** SimConnect message-pump layer, SimObject scanner, AI spawn manager, WPF radar display, ATC controllers (ground/tower/approach/QRA), and TTS engine.  See [docs/architecture.md](docs/architecture.md) for the full design and [docs/simconnect-notes.md](docs/simconnect-notes.md) for SDK setup.

---

## Prerequisites

- **Windows 10 / 11** (WPF requires Windows; build also works on Linux CI with `EnableWindowsTargeting`)
- **.NET 8 SDK** – [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (optional, but recommended for WPF designer support)
- SimConnect SDK (FSX SP2 or P3D) installed/redistributed on the target machine (see [docs/simconnect-notes.md](docs/simconnect-notes.md))

---

## Building

### With the .NET CLI (recommended for CI)

```powershell
# From the repository root
dotnet build SkyMasterATC.sln -c Release
```

> The solution builds **without** the SimConnect assembly (stub mode) by default.  
> In stub mode the application runs fully: aircraft are spawned in memory, positions are simulated at 1 Hz, and all UI panels are functional.  
> To enable real SimConnect, follow the steps in [docs/simconnect-notes.md](docs/simconnect-notes.md).

### With Visual Studio 2022

Open `SkyMasterATC.sln`, select **Release | Any CPU**, and press **Build → Build Solution**.

---

## Running

After building:

```powershell
.\src\SkyMasterATC.App\bin\Release\net8.0-windows\SkyMasterATC.exe
```

The application will open and immediately attempt a SimConnect connection.  In stub mode this succeeds instantly and the status indicator turns green.  Click **Start Position Updates** to begin simulated aircraft movement, then use the **Spawn** tab to add AI aircraft to the radar.

---

## Quick-Start (Stub Mode)

1. Launch the application – status shows **Connected**.
2. Click **Start Position Updates**.
3. Switch to the **Spawn** tab → enter a position (or leave defaults for Heathrow) → click **Spawn Aircraft**.
4. The aircraft appears on the radar.  Click its data tag to select it.
5. Switch to **ATC** tab → issue pushback / taxi / takeoff, assign altitude / heading / speed, or press **SCRAMBLE** for a QRA intercept.
6. Switch to the **TTS** tab to hear ATC phrases spoken aloud.

---

## Publishing a Self-Contained Windows EXE

```powershell
dotnet publish src/SkyMasterATC.App/SkyMasterATC.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

The output `publish\SkyMasterATC.exe` is a fully self-contained Windows desktop executable.

---

## Project Structure

```
SkyMasterATC.sln
src/
  SkyMasterATC.App/        WPF desktop application (.exe) + RadarControl
  SkyMasterATC.Core/       Business logic, models, TTS service (System.Speech)
  SkyMasterATC.SimConnect/ SimConnect abstraction layer (stub + real SDK path)
  SkyMasterATC.Traffic/    SimObject scanner, AI spawn manager, ATC controllers
docs/
  architecture.md
  simconnect-notes.md
```

See [docs/architecture.md](docs/architecture.md) for detailed responsibilities of each layer.

---

## Status

| Phase | Feature | Status |
|---|---|---|
| 1 | SimConnect skeleton + WPF UI | ✅ Complete |
| 2 | SimObject library scanner + AI spawn manager | ✅ Complete |
| 3 | Radar renderer (WPF Canvas, range rings, data tags, zoom/pan) | ✅ Complete |
| 4 | ATC controllers (ground/tower, approach/center, QRA scramble) | ✅ Complete |
| 5 | TTS engine (Windows SAPI, ATC phrase builder, voice selection) | ✅ Complete |
