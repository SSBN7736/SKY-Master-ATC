using System.Timers;
using SkyMasterATC.Core.Models;
using SkyMasterATC.SimConnect.Exceptions;
using SkyMasterATC.SimConnect.Interop;

// When the managed SimConnect assembly is available (copied from the FSX/P3D
// SDK into lib/SimConnect/), uncomment the line below and remove the
// SIMCONNECT_UNAVAILABLE conditional compilation symbol from the project.
//
// #define SIMCONNECT_AVAILABLE

namespace SkyMasterATC.SimConnect.Client
{
    /// <summary>
    /// Concrete SimConnect client.  Opens a session using a Win32 window handle
    /// and delegates message pumping to the WndProc hook installed by the WPF
    /// host window.
    /// </summary>
    public sealed class SimConnectClient : ISimConnectClient
    {
#if SIMCONNECT_AVAILABLE
        private Microsoft.FlightSimulator.SimConnect.SimConnect? _sim;
#else
        private object? _sim;
#endif

        private IntPtr _hwnd;
        private bool _disposed;

        private const double MinCosLat = 1e-9; // Guard against division by zero near poles

        private readonly Dictionary<uint, SimAircraft> _simAircraft = new();
        private readonly object _aircraftLock = new();
        private uint _nextObjectId = 1;
        private System.Timers.Timer? _positionTimer;
        private bool _positionUpdatesSubscribed;

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public event EventHandler? Connected;

        /// <inheritdoc/>
        public event EventHandler? Disconnected;

        /// <inheritdoc/>
        public event EventHandler<Exception>? Error;

        /// <inheritdoc/>
        public event EventHandler<SimAircraft>? AircraftUpdated;

        /// <inheritdoc/>
        public event EventHandler<uint>? AiObjectAdded;

        /// <inheritdoc/>
        public event EventHandler<uint>? AiObjectRemoved;

        /// <inheritdoc/>
        public void Connect(IntPtr hwnd)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsConnected)
                return;

            _hwnd = hwnd;

            try
            {
#if SIMCONNECT_AVAILABLE
                _sim = new Microsoft.FlightSimulator.SimConnect.SimConnect(
                    SimConnectConstants.APP_NAME,
                    hwnd,
                    SimConnectConstants.WM_USER_SIMCONNECT,
                    null,
                    0);

                _sim.OnRecvOpen      += OnRecvOpen;
                _sim.OnRecvQuit      += OnRecvQuit;
                _sim.OnRecvException += OnRecvException;
#else
                _sim = new object();
                IsConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
#endif
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Error?.Invoke(this, new SimConnectException("Failed to open SimConnect session.", ex));
            }
        }

        /// <inheritdoc/>
        public void ReceiveMessage()
        {
            if (!IsConnected || _sim is null)
                return;

            try
            {
#if SIMCONNECT_AVAILABLE
                ((Microsoft.FlightSimulator.SimConnect.SimConnect)_sim).ReceiveMessage();
#endif
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            StopPositionTimer();

            try
            {
#if SIMCONNECT_AVAILABLE
                var sim = (Microsoft.FlightSimulator.SimConnect.SimConnect?)_sim;
                if (sim is not null)
                {
                    sim.OnRecvOpen      -= OnRecvOpen;
                    sim.OnRecvQuit      -= OnRecvQuit;
                    sim.OnRecvException -= OnRecvException;
                    sim.Dispose();
                }
#endif
                _sim = null;
            }
            finally
            {
                IsConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            Disconnect();
            StopPositionTimer();
            _disposed = true;
        }

        // ── AI Spawn/Despawn ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public Task<uint> SpawnAiAircraftAsync(
            string title,
            double lat,
            double lon,
            double altFt,
            double headingDeg,
            bool onGround)
        {
#if SIMCONNECT_AVAILABLE
            // Real SimConnect: call AICreateNonATCAircraft with SIMCONNECT_DATA_INITPOSITION.
            // Returns objectId via OnRecvAssignedObjectId callback.
            // For now fall through to stub so the app remains functional.
#endif
            uint id;
            lock (_aircraftLock)
            {
                id = _nextObjectId++;
                var aircraft = new SimAircraft
                {
                    ObjectId      = id,
                    Callsign      = GenerateCallsign(title),
                    TypeDesignator = ExtractIcaoType(title),
                    Latitude      = lat,
                    Longitude     = lon,
                    AltitudeFt    = altFt,
                    HeadingDeg    = headingDeg,
                    SpeedKnots    = onGround ? 0 : 250,
                    Squawk        = GenerateSquawk(),
                    LastUpdatedUtc = DateTime.UtcNow,
                };
                _simAircraft[id] = aircraft;
            }

            AiObjectAdded?.Invoke(this, id);
            return Task.FromResult(id);
        }

        /// <inheritdoc/>
        public Task DespawnAiAircraftAsync(uint objectId)
        {
#if SIMCONNECT_AVAILABLE
            // Real SimConnect: call AIRemoveObject.
#endif
            lock (_aircraftLock)
            {
                _simAircraft.Remove(objectId);
            }

            AiObjectRemoved?.Invoke(this, objectId);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void SubscribeAircraftPositionUpdates()
        {
            if (_positionUpdatesSubscribed)
                return;

            _positionUpdatesSubscribed = true;

#if SIMCONNECT_AVAILABLE
            // Real SimConnect: RequestDataOnSimObjectType with SIMCONNECT_SIMOBJECT_TYPE.ALL
            // and a data definition containing PLANE_LATITUDE, PLANE_LONGITUDE, etc.
            // Fall through to stub timer so behaviour is consistent in dev builds.
#endif
            _positionTimer = new System.Timers.Timer(1000);
            _positionTimer.Elapsed += OnPositionTimerElapsed;
            _positionTimer.AutoReset = true;
            _positionTimer.Start();
        }

        /// <inheritdoc/>
        public void TransmitClientEvent(uint objectId, SimEventId eventId, uint data)
        {
#if SIMCONNECT_AVAILABLE
            var sim = (Microsoft.FlightSimulator.SimConnect.SimConnect?)_sim;
            sim?.TransmitClientEvent(
                objectId,
                (Microsoft.FlightSimulator.SimConnect.SIMCONNECT_CLIENT_EVENT_ID)eventId,
                data,
                Microsoft.FlightSimulator.SimConnect.SIMCONNECT_GROUP_PRIORITY.HIGHEST,
                Microsoft.FlightSimulator.SimConnect.SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
#endif
            // Stub: event is acknowledged silently; UI state updated via CommandIssued events.
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            List<SimAircraft> snapshot;
            lock (_aircraftLock)
            {
                snapshot = [.. _simAircraft.Values];
            }

            foreach (var aircraft in snapshot)
            {
                // Advance position by 1-second worth of travel at current speed.
                double distNm = aircraft.SpeedKnots / 3600.0;
                double headRad = aircraft.HeadingDeg * Math.PI / 180.0;
                double cosLat  = Math.Cos(aircraft.Latitude * Math.PI / 180.0);

                aircraft.Latitude  += (distNm * Math.Cos(headRad)) / 60.0;
                aircraft.Longitude += (distNm * Math.Sin(headRad)) / (60.0 * Math.Max(Math.Abs(cosLat), MinCosLat));
                aircraft.LastUpdatedUtc = DateTime.UtcNow;

                AircraftUpdated?.Invoke(this, aircraft);
            }
        }

        private void StopPositionTimer()
        {
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            _positionTimer = null;
            _positionUpdatesSubscribed = false;
        }

        private static string GenerateCallsign(string title)
        {
            var firstWord = title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .FirstOrDefault() ?? "AIR";
            var prefix = firstWord[..Math.Min(3, firstWord.Length)].ToUpper();
            return $"{prefix}{Random.Shared.Next(100, 999)}";
        }

        private static string ExtractIcaoType(string title)
        {
            // Best-effort: try to find a 4-char ICAO designator in the title.
            foreach (var part in title.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length is >= 2 and <= 4 && part.All(char.IsLetterOrDigit))
                    return part.ToUpper();
            }
            return "ZZZZ";
        }

        private static string GenerateSquawk()
        {
            // Return a random discrete squawk in range 2001-7776.
            int code = Random.Shared.Next(2001, 7778);
            return code.ToString();
        }

#if SIMCONNECT_AVAILABLE
        private void OnRecvOpen(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV_OPEN data)
        {
            IsConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private void OnRecvQuit(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        private void OnRecvException(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV_EXCEPTION data)
        {
            var ex = new SimConnectException(
                $"SimConnect exception: {data.dwException} (sendId={data.dwSendID}, index={data.dwIndex})");
            Error?.Invoke(this, ex);
        }
#endif
    }
}
