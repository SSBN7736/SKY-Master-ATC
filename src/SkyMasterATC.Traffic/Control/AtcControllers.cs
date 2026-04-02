using SkyMasterATC.Core.Models;
using SkyMasterATC.SimConnect.Client;

namespace SkyMasterATC.Traffic.Control
{
    /// <summary>
    /// Handles ground and tower ATC commands: pushback, taxi, and takeoff clearances.
    /// Phase 3 will implement the full SimConnect event calls.
    /// </summary>
    public sealed class GroundTowerController
    {
        private readonly ISimConnectClient _simConnect;

        public GroundTowerController(ISimConnectClient simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>Issues a pushback clearance to the specified AI aircraft.</summary>
        public void IssuePushback(uint objectId) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.Pushback });

        /// <summary>Issues a taxi-to-runway clearance.</summary>
        public void IssueTaxi(uint objectId) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.TaxiClearance });

        /// <summary>Clears the aircraft for takeoff.</summary>
        public void ClearForTakeoff(uint objectId) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.TakeoffClearance });

        private void IssueCommand(AtcCommand command)
        {
            // TODO (Phase 3): translate AtcCommand to SimConnect client events.
        }
    }

    /// <summary>
    /// Handles approach and center ATC commands: vectoring and altitude assignments.
    /// Phase 4 will implement the full SimConnect event calls.
    /// </summary>
    public sealed class ApproachCenterController
    {
        private readonly ISimConnectClient _simConnect;

        public ApproachCenterController(ISimConnectClient simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>Assigns an altitude to an AI aircraft.</summary>
        public void AssignAltitude(uint objectId, double altitudeFt) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.AltitudeAssignment, Value = altitudeFt });

        /// <summary>Issues a heading vector to an AI aircraft.</summary>
        public void AssignHeading(uint objectId, double headingDeg) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.HeadingVector, Value = headingDeg });

        /// <summary>Issues a speed restriction to an AI aircraft.</summary>
        public void AssignSpeed(uint objectId, double speedKnots) =>
            IssueCommand(new AtcCommand { TargetObjectId = objectId, CommandType = AtcCommandType.SpeedRestriction, Value = speedKnots });

        private void IssueCommand(AtcCommand command)
        {
            // TODO (Phase 4): translate AtcCommand to SimConnect client events.
        }
    }

    /// <summary>
    /// Quick Reaction Alert (QRA) controller.
    /// Spawns fighter interceptors and vectors them toward a designated target.
    /// Phase 4 will implement full QRA logic.
    /// </summary>
    public sealed class QraController
    {
        private readonly ISimConnectClient _simConnect;

        public QraController(ISimConnectClient simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>
        /// Scrambles interceptors toward the AI aircraft identified by
        /// <paramref name="targetObjectId"/>.
        /// </summary>
        public Task ScrambleAsync(uint targetObjectId)
        {
            // TODO (Phase 4): spawn fighter jets near the target and issue intercept commands.
            return Task.CompletedTask;
        }
    }
}
