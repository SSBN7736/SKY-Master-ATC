using SkyMasterATC.Core.Models;
using SkyMasterATC.SimConnect.Interop;

namespace SkyMasterATC.SimConnect.Client
{
    /// <summary>
    /// Abstraction over the SimConnect session.  Implementations handle opening
    /// a connection to FSX/P3D using a Win32 window handle and pumping SimConnect
    /// messages via a WndProc hook.
    /// </summary>
    public interface ISimConnectClient : IDisposable
    {
        /// <summary>Returns <see langword="true"/> when the session is open.</summary>
        bool IsConnected { get; }

        /// <summary>Raised on the UI thread when the SimConnect session opens successfully.</summary>
        event EventHandler? Connected;

        /// <summary>Raised on the UI thread when the SimConnect session closes or the sim exits.</summary>
        event EventHandler? Disconnected;

        /// <summary>Raised when a recoverable SimConnect error or exception occurs.</summary>
        event EventHandler<Exception>? Error;

        /// <summary>Raised when position data for an AI aircraft arrives.</summary>
        event EventHandler<SimAircraft>? AircraftUpdated;

        /// <summary>Raised when an AI object is added to the simulation.</summary>
        event EventHandler<uint>? AiObjectAdded;

        /// <summary>Raised when an AI object is removed from the simulation.</summary>
        event EventHandler<uint>? AiObjectRemoved;

        /// <summary>
        /// Opens a SimConnect session.  Must be called after the WPF window handle
        /// is available (i.e., from <c>OnSourceInitialized</c> or later).
        /// </summary>
        /// <param name="hwnd">Win32 window handle of the host WPF window.</param>
        void Connect(IntPtr hwnd);

        /// <summary>Closes the active SimConnect session cleanly.</summary>
        void Disconnect();

        /// <summary>
        /// Must be called each time the application receives the
        /// <see cref="SkyMasterATC.SimConnect.Interop.SimConnectConstants.WM_USER_SIMCONNECT"/>
        /// Windows message (from the WndProc hook).
        /// SimConnect delivers all callbacks synchronously during this call.
        /// </summary>
        void ReceiveMessage();

        /// <summary>Spawns an AI aircraft and returns the assigned SimConnect object ID.</summary>
        Task<uint> SpawnAiAircraftAsync(
            string title,
            double lat,
            double lon,
            double altFt,
            double headingDeg,
            bool onGround);

        /// <summary>Removes a previously spawned AI aircraft from the simulation.</summary>
        Task DespawnAiAircraftAsync(uint objectId);

        /// <summary>
        /// Begins subscribing to periodic position updates for all AI objects.
        /// Raises <see cref="AircraftUpdated"/> approximately once per second per aircraft.
        /// </summary>
        void SubscribeAircraftPositionUpdates();

        /// <summary>Transmits a client event to the simulator for the specified object.</summary>
        void TransmitClientEvent(uint objectId, SimEventId eventId, uint data);
    }
}
