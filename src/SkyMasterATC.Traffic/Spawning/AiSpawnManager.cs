using SkyMasterATC.Core.Models;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.Traffic.SimObjectLibrary;

namespace SkyMasterATC.Traffic.Spawning
{
    /// <summary>
    /// Manages spawning and despawning of AI aircraft via SimConnect.
    /// Phase 2 will implement full spawn logic including gate assignment.
    /// </summary>
    public sealed class AiSpawnManager
    {
        private readonly ISimConnectClient _simConnect;
        private readonly Dictionary<uint, SimAircraft> _spawnedAircraft = new();

        public AiSpawnManager(ISimConnectClient simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>All currently spawned AI aircraft tracked by this manager.</summary>
        public IReadOnlyDictionary<uint, SimAircraft> SpawnedAircraft => _spawnedAircraft;

        /// <summary>
        /// Spawns an AI aircraft based on the supplied model at the given position.
        /// </summary>
        /// <param name="model">Aircraft model to spawn.</param>
        /// <param name="latitude">Spawn latitude (decimal degrees).</param>
        /// <param name="longitude">Spawn longitude (decimal degrees).</param>
        /// <param name="altitudeFt">Spawn altitude (feet MSL).</param>
        /// <param name="headingDeg">Initial heading (degrees true).</param>
        public Task<uint> SpawnAsync(
            SimObjectEntry model,
            double latitude,
            double longitude,
            double altitudeFt = 0,
            double headingDeg = 0)
        {
            // TODO (Phase 2): call SimConnect AICreateNonATCAircraft / AICreateParkedATCAircraft.
            return Task.FromResult(0u);
        }

        /// <summary>Removes a previously spawned AI aircraft from the simulator.</summary>
        public Task DespawnAsync(uint objectId)
        {
            // TODO (Phase 2): call SimConnect AIRemoveObject.
            _spawnedAircraft.Remove(objectId);
            return Task.CompletedTask;
        }
    }
}
