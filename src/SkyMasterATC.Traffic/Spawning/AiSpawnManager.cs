using SkyMasterATC.Core.Models;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.Traffic.SimObjectLibrary;

namespace SkyMasterATC.Traffic.Spawning
{
    /// <summary>
    /// Manages spawning and despawning of AI aircraft via SimConnect.
    /// Tracks live position updates and exposes the current aircraft state.
    /// </summary>
    public sealed class AiSpawnManager : IDisposable
    {
        private readonly ISimConnectClient _simConnect;
        private readonly Dictionary<uint, SimAircraft> _spawnedAircraft = new();
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>Raised whenever a tracked aircraft's position data is refreshed.</summary>
        public event EventHandler<SimAircraft>? AircraftUpdated;

        public AiSpawnManager(ISimConnectClient simConnect)
        {
            _simConnect = simConnect;
            _simConnect.AircraftUpdated  += OnSimAircraftUpdated;
            _simConnect.AiObjectRemoved  += OnSimAiObjectRemoved;
        }

        /// <summary>All currently spawned AI aircraft tracked by this manager.</summary>
        public IReadOnlyDictionary<uint, SimAircraft> SpawnedAircraft
        {
            get
            {
                lock (_lock)
                    return new Dictionary<uint, SimAircraft>(_spawnedAircraft);
            }
        }

        /// <summary>
        /// Spawns an AI aircraft based on the supplied model at the given position.
        /// </summary>
        public async Task<uint> SpawnAsync(
            SimObjectEntry model,
            double latitude,
            double longitude,
            double altitudeFt = 0,
            double headingDeg = 0)
        {
            bool onGround = altitudeFt < 500;
            uint objectId = await _simConnect.SpawnAiAircraftAsync(
                model.Title, latitude, longitude, altitudeFt, headingDeg, onGround);

            var aircraft = new SimAircraft
            {
                ObjectId       = objectId,
                TypeDesignator = model.IcaoType,
                Callsign       = GenerateCallsign(model.IcaoType),
                Latitude       = latitude,
                Longitude      = longitude,
                AltitudeFt     = altitudeFt,
                HeadingDeg     = headingDeg,
                SpeedKnots     = onGround ? 0 : 250,
                Squawk         = Random.Shared.Next(2001, 7778).ToString(),
                LastUpdatedUtc = DateTime.UtcNow,
            };

            lock (_lock)
                _spawnedAircraft[objectId] = aircraft;

            return objectId;
        }

        /// <summary>Removes a previously spawned AI aircraft from the simulator.</summary>
        public async Task DespawnAsync(uint objectId)
        {
            await _simConnect.DespawnAiAircraftAsync(objectId);
            lock (_lock)
                _spawnedAircraft.Remove(objectId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _simConnect.AircraftUpdated -= OnSimAircraftUpdated;
            _simConnect.AiObjectRemoved -= OnSimAiObjectRemoved;
            _disposed = true;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnSimAircraftUpdated(object? sender, SimAircraft aircraft)
        {
            lock (_lock)
            {
                if (!_spawnedAircraft.ContainsKey(aircraft.ObjectId))
                    return;

                _spawnedAircraft[aircraft.ObjectId] = aircraft;
            }

            AircraftUpdated?.Invoke(this, aircraft);
        }

        private void OnSimAiObjectRemoved(object? sender, uint objectId)
        {
            lock (_lock)
                _spawnedAircraft.Remove(objectId);
        }

        private static string GenerateCallsign(string icaoType)
        {
            var prefix = icaoType.Length >= 3 ? icaoType[..3] : icaoType.PadRight(3, 'X');
            return $"{prefix}{Random.Shared.Next(100, 999)}";
        }
    }
}
