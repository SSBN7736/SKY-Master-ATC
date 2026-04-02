# SimConnect Notes

## What is SimConnect?

SimConnect is the SDK used to communicate with Microsoft Flight Simulator X (FSX) and Lockheed Martin Prepar3D (P3D).  It exposes a client API (either a managed .NET assembly or a native DLL) that allows external applications to:

- Query and set simulator variables.
- Create, move, and remove AI objects.
- Subscribe to simulator system events.

---

## Required Redistributables

### FSX (SP2 / Acceleration)

| File | Where to find it |
|---|---|
| `SimConnect.dll` (native) | `<FSX install>\SDK\Core Utilities Kit\SimConnect SDK\lib\` |
| `Microsoft.FlightSimulator.SimConnect.dll` (managed) | Same SDK folder |

Install the **FSX SP2 SimConnect** redistributable (`SimConnect.msi`) on any machine that will run SkyMaster ATC against FSX.  The MSI is found at `<FSX install>\SDK\Core Utilities Kit\SimConnect SDK\LegacyInterfaces\FSX-SP1\SimConnect.msi` (and the SP2 variant).

### P3D v4 / v5 / v6

Each P3D version ships its own SimConnect version.  When targeting P3D, use the **P3D-matching** SDK:

| Version | Redistributable location |
|---|---|
| P3D v4 | `<P3D install>\redist\Interface\FSX-SP2-XPACK\retail\lib\SimConnect.msi` |
| P3D v5/v6 | Similar structure; consult the P3D SDK documentation |

> **Important:** SimConnect versions are not cross-compatible.  An application built against the P3D v5 SimConnect will not connect to FSX SP2 and vice versa.  Use the lowest common SDK version (FSX SP2) for broadest compatibility.

---

## Adding the Managed Assembly to the Project

SkyMaster ATC is designed to compile **without** the SimConnect assembly present (using `#if SIMCONNECT_AVAILABLE` guards in `SimConnectClient.cs`).

To enable real SimConnect functionality:

1. Copy the following files into `lib/SimConnect/` at the root of the repository:
   - `Microsoft.FlightSimulator.SimConnect.dll`
   - `SimConnect.dll` (native, must accompany the managed DLL at runtime)

2. Add a project reference in `SkyMasterATC.SimConnect.csproj`:
   ```xml
   <ItemGroup>
     <Reference Include="Microsoft.FlightSimulator.SimConnect">
       <HintPath>..\..\lib\SimConnect\Microsoft.FlightSimulator.SimConnect.dll</HintPath>
       <Private>true</Private>
     </Reference>
   </ItemGroup>
   ```

3. Define `SIMCONNECT_AVAILABLE` in the project:
   ```xml
   <PropertyGroup>
     <DefineConstants>$(DefineConstants);SIMCONNECT_AVAILABLE</DefineConstants>
   </PropertyGroup>
   ```

4. Rebuild the solution.  The real `SimConnect` constructor and event handlers in `SimConnectClient.cs` will now compile.

> **Note:** Do **not** commit the SimConnect DLLs to this repository – they are proprietary Lockheed Martin / Microsoft redistributables covered by their respective SDK licenses.

---

## FSX vs P3D Compatibility Strategy

- Target the **FSX SP2** SimConnect headers/assembly for maximum compatibility.
- P3D v4+ is backwards-compatible with FSX SP2 SimConnect for the subset of APIs used by SkyMaster ATC (AI object creation, data definitions, system events).
- If a P3D-specific feature is needed (e.g., P3D v5 `SimConnect_AICreateSimulatedObject`), gate it with a runtime version check or a separate build configuration.

---

## Connection Flow

```
WPF MainWindow (OnSourceInitialized)
  └─ WindowInteropHelper.Handle  ──► Win32 HWND
       └─ SimConnectClient.Connect(hwnd)
            └─ new SimConnect(APP_NAME, hwnd, WM_USER_SIMCONNECT, null, 0)
                 │
                 ▼  (simulator posts WM_USER_SIMCONNECT to hwnd)
            HwndSourceHook (WndProc)
                 └─ SimConnectClient.ReceiveMessage()
                      └─ SimConnect.ReceiveMessage()
                           ├─ OnRecvOpen      → Connected event
                           ├─ OnRecvQuit      → Disconnected event
                           └─ OnRecvException → Error event
```

---

## Handling "Sim Not Running"

When SimConnect is opened and the simulator is not running:

- The `SimConnect` constructor throws a `COMException` (HRESULT 0xC000014B or similar).
- `SimConnectClient.Connect` catches this and fires the `Error` event.
- `MainWindow` shows a warning dialog and leaves the UI in "Not Connected" state.
- The user can retry by clicking **Connect**.

A future phase will add an automatic reconnect loop with exponential back-off.
