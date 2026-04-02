using SkyMasterATC.Core.Models;
using SkyMasterATC.Core.Services;
using SkyMasterATC.Core.Util;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.SimConnect.Interop;
using SkyMasterATC.Traffic.SimObjectLibrary;
using SkyMasterATC.Traffic.Spawning;

namespace SkyMasterATC.Traffic.Control
{
    /// <summary>
    /// Handles ground and tower ATC commands: pushback, taxi, and takeoff clearances.
    /// </summary>
    public sealed class GroundTowerController
    {
        private readonly ISimConnectClient _simConnect;
        private readonly TtsService _tts;
        private readonly AiSpawnManager _spawnManager;

        /// <summary>Raised whenever a command is issued, providing full command details.</summary>
        public event EventHandler<AtcCommand>? CommandIssued;

        public GroundTowerController(
            ISimConnectClient simConnect,
            TtsService tts,
            AiSpawnManager spawnManager)
        {
            _simConnect  = simConnect;
            _tts         = tts;
            _spawnManager = spawnManager;
        }

        /// <summary>Issues a pushback clearance to the specified AI aircraft.</summary>
        public void IssuePushback(uint objectId)
        {
            _simConnect.TransmitClientEvent(objectId, SimEventId.PushbackSet, 1);
            IssueCommand(new AtcCommand
            {
                TargetObjectId  = objectId,
                CommandType     = AtcCommandType.Pushback,
            });
        }

        /// <summary>Issues a taxi-to-runway clearance.</summary>
        public void IssueTaxi(uint objectId)
        {
            _simConnect.TransmitClientEvent(objectId, SimEventId.SetAtcActive, 1);
            IssueCommand(new AtcCommand
            {
                TargetObjectId = objectId,
                CommandType    = AtcCommandType.TaxiClearance,
            });
        }

        /// <summary>Clears the aircraft for takeoff.</summary>
        public void ClearForTakeoff(uint objectId)
        {
            _simConnect.TransmitClientEvent(objectId, SimEventId.SetAtcActive, 2);
            IssueCommand(new AtcCommand
            {
                TargetObjectId = objectId,
                CommandType    = AtcCommandType.TakeoffClearance,
            });
        }

        private void IssueCommand(AtcCommand command)
        {
            CommandIssued?.Invoke(this, command);
            var callsign = GetCallsign(command.TargetObjectId);
            _ = _tts.SpeakAtcPhrase(command, callsign);
        }

        private string GetCallsign(uint objectId)
            => _spawnManager.SpawnedAircraft.TryGetValue(objectId, out var a) ? a.Callsign : objectId.ToString();
    }

    /// <summary>
    /// Handles approach and center ATC commands: vectoring, altitude, and speed assignments.
    /// </summary>
    public sealed class ApproachCenterController
    {
        private readonly ISimConnectClient _simConnect;
        private readonly TtsService _tts;
        private readonly AiSpawnManager _spawnManager;

        /// <summary>Raised whenever a command is issued.</summary>
        public event EventHandler<AtcCommand>? CommandIssued;

        public ApproachCenterController(
            ISimConnectClient simConnect,
            TtsService tts,
            AiSpawnManager spawnManager)
        {
            _simConnect   = simConnect;
            _tts          = tts;
            _spawnManager = spawnManager;
        }

        /// <summary>Assigns an altitude to an AI aircraft.</summary>
        public void AssignAltitude(uint objectId, double altitudeFt)
            => IssueCommand(new AtcCommand
            {
                TargetObjectId = objectId,
                CommandType    = AtcCommandType.AltitudeAssignment,
                Value          = altitudeFt,
            });

        /// <summary>Issues a heading vector to an AI aircraft.</summary>
        public void AssignHeading(uint objectId, double headingDeg)
            => IssueCommand(new AtcCommand
            {
                TargetObjectId = objectId,
                CommandType    = AtcCommandType.HeadingVector,
                Value          = headingDeg,
            });

        /// <summary>Issues a speed restriction to an AI aircraft.</summary>
        public void AssignSpeed(uint objectId, double speedKnots)
            => IssueCommand(new AtcCommand
            {
                TargetObjectId = objectId,
                CommandType    = AtcCommandType.SpeedRestriction,
                Value          = speedKnots,
            });

        private void IssueCommand(AtcCommand command)
        {
            // Transmit appropriate SimConnect event where mappings exist.
            if (command.CommandType == AtcCommandType.SpeedRestriction)
                _simConnect.TransmitClientEvent(command.TargetObjectId, SimEventId.XpndrCodeSet, (uint)command.Value);

            CommandIssued?.Invoke(this, command);
            var callsign = GetCallsign(command.TargetObjectId);
            _ = _tts.SpeakAtcPhrase(command, callsign);
        }

        private string GetCallsign(uint objectId)
            => _spawnManager.SpawnedAircraft.TryGetValue(objectId, out var a) ? a.Callsign : objectId.ToString();
    }

    /// <summary>
    /// Quick Reaction Alert (QRA) controller.
    /// Spawns fighter interceptors and vectors them toward a designated target.
    /// </summary>
    public sealed class QraController
    {
        private readonly ISimConnectClient _simConnect;
        private readonly AiSpawnManager _spawnManager;
        private readonly ApproachCenterController _approachCenter;
        private readonly TtsService _tts;

        /// <summary>Raised when a scramble order has been issued.</summary>
        public event EventHandler<ScrambleEventArgs>? ScrambleInitiated;

        /// <summary>Fighter model template used for QRA spawns.</summary>
        public SimObjectEntry FighterModelTemplate { get; set; } = new SimObjectEntry
        {
            Title      = "F-16 Fighting Falcon",
            IcaoType   = "F16",
            FolderPath = string.Empty,
        };

        public QraController(
            ISimConnectClient simConnect,
            AiSpawnManager spawnManager,
            ApproachCenterController approachCenter,
            TtsService tts)
        {
            _simConnect     = simConnect;
            _spawnManager   = spawnManager;
            _approachCenter = approachCenter;
            _tts            = tts;
        }

        /// <summary>
        /// Scrambles an interceptor toward the AI aircraft identified by
        /// <paramref name="targetObjectId"/>.
        /// </summary>
        public async Task ScrambleAsync(uint targetObjectId)
        {
            if (!_spawnManager.SpawnedAircraft.TryGetValue(targetObjectId, out var target))
                return;

            // Spawn the interceptor ~10 NM behind the target's tail.
            double spawnBearing = (target.HeadingDeg + 180) % 360;
            var (spawnLat, spawnLon) = Geo.OffsetPosition(
                target.Latitude, target.Longitude, spawnBearing, 10.0);

            uint interceptorId = await _spawnManager.SpawnAsync(
                FighterModelTemplate,
                spawnLat,
                spawnLon,
                altitudeFt: 15_000,
                headingDeg: target.HeadingDeg);

            // Vector the interceptor toward the target.
            double bearing = Geo.BearingDeg(spawnLat, spawnLon, target.Latitude, target.Longitude);
            _approachCenter.AssignHeading(interceptorId, bearing);

            var args = new ScrambleEventArgs(targetObjectId, interceptorId);
            ScrambleInitiated?.Invoke(this, args);

            var scrambleCmd = new AtcCommand
            {
                TargetObjectId = interceptorId,
                CommandType    = AtcCommandType.Scramble,
            };
            await _tts.SpeakAtcPhrase(scrambleCmd,
                _spawnManager.SpawnedAircraft.TryGetValue(interceptorId, out var iv) ? iv.Callsign : "Interceptor");
        }
    }

    /// <summary>Event data for a QRA scramble order.</summary>
    public sealed class ScrambleEventArgs(uint targetObjectId, uint interceptorObjectId) : EventArgs
    {
        public uint TargetObjectId      { get; } = targetObjectId;
        public uint InterceptorObjectId { get; } = interceptorObjectId;
    }
}
