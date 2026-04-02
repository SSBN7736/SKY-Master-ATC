# SkyMaster ATC – Architecture

## Overview

SkyMaster ATC is a Windows desktop ATC client that connects to Microsoft FSX / Lockheed Martin P3D via the SimConnect SDK.  The application is a **.NET 8 WPF executable** (`SkyMasterATC.exe`) built from a multi-project solution.

---

## Folder Structure

```
SkyMasterATC/
  SkyMasterATC.sln                 ← Visual Studio solution

  src/
    SkyMasterATC.App/              ← WPF desktop application (net8.0-windows)
      App.xaml / App.xaml.cs       ← Composition root; creates all services, launches MainWindow
      MainWindow.xaml / .cs        ← Full ATC console UI (radar, spawn, ATC, TTS panels)
      Controls/
        RadarControl.xaml / .cs    ← Canvas-based radar display with zoom/pan, data tags

    SkyMasterATC.Core/             ← Pure business logic – no UI, no SimConnect
      Models/
        SimAircraft.cs             ← Live AI aircraft record (position, callsign, squawk, …)
        RadarTrack.cs              ← Radar return snapshot with screen coordinates
        AtcCommand.cs              ← ATC instruction model (altitude, heading, clearances, …)
      Services/
        RadarRendererService.cs    ← Track store, LatLonToScreen helper, TracksUpdated event
        TtsService.cs              ← Windows SAPI TTS via System.Speech (serialised queue)
      Util/
        Geo.cs                     ← Great-circle distance / bearing / offset helpers

    SkyMasterATC.SimConnect/       ← ALL SimConnect interop – isolated from UI and Core
      Client/
        ISimConnectClient.cs       ← Interface: Connect, events, spawn, subscribe, transmit
        SimConnectClient.cs        ← Concrete client; WndProc pump; stub position simulation
      Interop/
        SimConnectConstants.cs     ← APP_NAME, WM_USER_SIMCONNECT
        SimConnectIds.cs           ← DataDefinitionId, DataRequestId, SimEventId enums
      Exceptions/
        SimConnectException.cs     ← Typed exception for SimConnect errors

    SkyMasterATC.Traffic/          ← AI spawning and ATC control
      SimObjectLibrary/
        SimObjectScanner.cs        ← INI parser scanning SimObjects for aircraft.cfg entries
      Spawning/
        AiSpawnManager.cs          ← Spawn / despawn AI aircraft; propagates position updates
      Control/
        AtcControllers.cs          ← Ground/Tower, Approach/Center, QRA controllers

  docs/
    architecture.md                ← This file
    simconnect-notes.md            ← SimConnect SDK setup and FSX/P3D compatibility notes
```

---

## Layer Responsibilities

| Layer | Project | Dependencies |
|---|---|---|
| **UI** | `SkyMasterATC.App` | Core, SimConnect, Traffic |
| **Business logic** | `SkyMasterATC.Core` | System.Speech (Windows SAPI) |
| **Simulator interface** | `SkyMasterATC.SimConnect` | Core |
| **Traffic management** | `SkyMasterATC.Traffic` | Core, SimConnect |

All projects target **net8.0-windows** with `EnableWindowsTargeting=true` so the solution builds on Linux CI without the Windows runtime.

---

## SimConnect Message Pump

SimConnect on Windows uses a Win32 message-based notification model:

1. `MainWindow.OnSourceInitialized` obtains the WPF HWND via `WindowInteropHelper`.
2. An `HwndSourceHook` (WndProc) is installed on that handle.
3. SimConnect is opened with `WM_USER_SIMCONNECT` (= `WM_USER + 0x2D`) as the notification message ID.
4. When the simulator has data ready it posts `WM_USER_SIMCONNECT` to the window.
5. WndProc calls `ISimConnectClient.ReceiveMessage()`, which calls `SimConnect.ReceiveMessage()`.
6. SimConnect delivers all pending callbacks **synchronously** during that call – no threads needed.

When the real SDK assembly is absent (`SIMCONNECT_AVAILABLE` not defined), a stub mode runs: `Connect()` sets `IsConnected = true` immediately, and `SubscribeAircraftPositionUpdates()` starts a 1 Hz timer that moves each spawned aircraft along its heading vector, raising `AircraftUpdated` events so the full rendering pipeline is exercised.

---

## Composition Root

`App.xaml.cs` constructs all services and wires the cross-cutting event subscriptions:

```
SimConnectClient
  └─ AiSpawnManager (subscribes AircraftUpdated / AiObjectRemoved)
       └─ RadarRendererService  ← UpdateTrack / RemoveTrack on every position update
  └─ GroundTowerController  (SimConnect events + TTS)
  └─ ApproachCenterController (SimConnect events + TTS)
  └─ QraController  (spawns fighter via AiSpawnManager, vectors via ApproachCenter)
TtsService  (System.Speech SpeechSynthesizer, serialised via SemaphoreSlim)
```

---

## Phases (all complete)

| Phase | Deliverable |
|---|---|
| 1 | SimConnect skeleton, WPF window, connection status UI |
| 2 | SimObject library scanner (INI parser), AI spawn manager, SimConnect position stub timer |
| 3 | WPF RadarControl (Canvas, range rings, data tags, zoom/pan), RadarRendererService with LatLonToScreen |
| 4 | ATC controllers: Ground/Tower (pushback/taxi/takeoff), Approach/Center (alt/hdg/spd), QRA scramble |
| 5 | TTS engine (Windows SAPI, SpeechSynthesizer, ATC phrase builder, voice selection) |

---

## Stub Mode vs Real SimConnect

The build flag `SIMCONNECT_AVAILABLE` controls whether the real managed SimConnect wrapper is compiled in.

| Feature | Stub (default) | Real SDK |
|---|---|---|
| Connect | Sets `IsConnected = true` instantly | Opens SimConnect session |
| Spawn | Generates uint ID, stores `SimAircraft` in memory | Calls `AICreateNonATCAircraft` |
| Position updates | 1 Hz timer, moves aircraft along heading | `RequestDataOnSimObjectType` |
| Client events | No-op | `TransmitClientEvent` |
