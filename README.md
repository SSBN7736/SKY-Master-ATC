# SkyMaster ATC

SkyMaster ATC is a standalone ATC client for controlling AI traffic in Lockheed Martin Prepar3D (P3D) and Microsoft Flight Simulator X (FSX) using SimConnect.

> **Phase 1 – Initial skeleton:** SimConnect message-pump connection layer + WPF desktop UI.  See [docs/architecture.md](docs/architecture.md) for the full design and [docs/simconnect-notes.md](docs/simconnect-notes.md) for SDK setup.

---

## Prerequisites

- **Windows 10 / 11** (WPF requires Windows)
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

> The solution currently builds **without** the SimConnect assembly (placeholder mode).  
> To enable real SimConnect, follow the steps in [docs/simconnect-notes.md](docs/simconnect-notes.md).

### With Visual Studio 2022

Open `SkyMasterATC.sln`, select **Release | Any CPU**, and press **Build → Build Solution**.

---

## Running

After building:

```powershell
.\src\SkyMasterATC.App\bin\Release\net8.0-windows\SkyMasterATC.exe
```

The application will open and immediately attempt a SimConnect connection.  If the simulator is not running you will see a "SimConnect Error" warning and the status indicator will remain red.  Click **Connect** to retry at any time.

---

## Publishing a Self-Contained Windows EXE

To produce a single portable `.exe` that does not require the .NET runtime to be pre-installed:

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
  SkyMasterATC.App/        WPF desktop application (.exe)
  SkyMasterATC.Core/       Business logic, models, utilities (no UI / no SimConnect)
  SkyMasterATC.SimConnect/ SimConnect abstraction layer
  SkyMasterATC.Traffic/    AI spawning + ATC controller stubs
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
| 2 | SimObject library scanner + AI spawn manager | 🔲 Planned |
| 3 | Radar renderer (GDI+ / Skia) | 🔲 Planned |
| 4 | ATC controllers (ground/tower/approach/QRA) | 🔲 Planned |
| 5 | TTS engine (ATC ↔ pilot audio) | 🔲 Planned |
