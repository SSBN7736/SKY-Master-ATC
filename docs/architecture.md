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
      App.xaml / App.xaml.cs       ← Composition root; creates services, launches MainWindow
      MainWindow.xaml / .cs        ← Main window: status bar, connect/disconnect, radar placeholder
      Assets/                      ← Icons and static resources

    SkyMasterATC.Core/             ← Pure business logic – no UI, no SimConnect
      Models/
        SimAircraft.cs             ← Live AI aircraft record (position, callsign, squawk, …)
        RadarTrack.cs              ← Radar return snapshot with screen coordinates
        AtcCommand.cs              ← ATC instruction model (altitude, heading, clearances, …)
      Services/
        RadarRendererService.cs    ← Track store + stub render loop (Phase 3)
        TtsService.cs              ← Text-to-speech stub (Phase 5)
      Util/
        Geo.cs                     ← Great-circle distance / bearing helpers

    SkyMasterATC.SimConnect/       ← ALL SimConnect interop – isolated from UI and Core
      Client/
        ISimConnectClient.cs       ← Interface: Connect, Disconnect, ReceiveMessage, events
        SimConnectClient.cs        ← Concrete client; WndProc pump; #if SIMCONNECT_AVAILABLE
      Interop/
        SimConnectConstants.cs     ← APP_NAME, WM_USER_SIMCONNECT
        SimConnectIds.cs           ← DataDefinitionId, DataRequestId, SimEventId enums
      Exceptions/
        SimConnectException.cs     ← Typed exception for SimConnect errors

    SkyMasterATC.Traffic/          ← AI spawning and ATC control stubs (Phase 2–4)
      SimObjectLibrary/
        SimObjectScanner.cs        ← Scans SimObjects folder; builds aircraft model index
      Spawning/
        AiSpawnManager.cs          ← Spawn / despawn AI aircraft via SimConnect
      Control/
        AtcControllers.cs          ← Ground/Tower, Approach/Center, QRA controller stubs

  docs/
    architecture.md                ← This file
    simconnect-notes.md            ← SimConnect SDK setup and FSX/P3D compatibility notes
```

---

## Layer Responsibilities

| Layer | Project | Dependencies |
|---|---|---|
| **UI** | `SkyMasterATC.App` | Core, SimConnect, Traffic |
| **Business logic** | `SkyMasterATC.Core` | _(none)_ |
| **Simulator interface** | `SkyMasterATC.SimConnect` | _(none – optional managed SDK assembly)_ |
| **Traffic management** | `SkyMasterATC.Traffic` | Core, SimConnect |

`Core` has **no dependencies** so it can be unit-tested without a simulator or UI framework.

---

## SimConnect Message Pump

SimConnect on Windows uses a Win32 message-based notification model:

1. `MainWindow.OnSourceInitialized` obtains the WPF HWND via `WindowInteropHelper`.
2. An `HwndSourceHook` (WndProc) is installed on that handle.
3. SimConnect is opened with `WM_USER_SIMCONNECT` (= `WM_USER + 0x2D`) as the notification message ID.
4. When the simulator has data ready it posts `WM_USER_SIMCONNECT` to the window.
5. WndProc calls `ISimConnectClient.ReceiveMessage()`, which calls `SimConnect.ReceiveMessage()`.
6. SimConnect delivers all pending callbacks **synchronously** during that call – no threads needed.

This design keeps CPU overhead near zero between simulator updates.

---

## Composition Root

`App.xaml.cs` is the composition root.  It constructs all services and injects them where needed.  For Phase 1 the wiring is manual (`new SimConnectClient()`, etc.).  Later phases can migrate to `Microsoft.Extensions.DependencyInjection` without changing the interfaces.

---

## Planned Phases

| Phase | Deliverable |
|---|---|
| 1 (current) | SimConnect skeleton, WPF window, connection status UI |
| 2 | SimObject library scanner, AI spawn manager |
| 3 | Radar renderer (GDI+ / Skia), real-time track display |
| 4 | ATC controllers: ground/tower/approach/QRA |
| 5 | TTS engine integration (SAPI / cloud) |
