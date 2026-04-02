namespace SkyMasterATC.Core.Models
{
    /// <summary>
    /// Represents an ATC command issued to an AI aircraft (e.g., altitude assignment,
    /// heading vector, speed restriction).  Used by the ATC controller stubs.
    /// </summary>
    public sealed class AtcCommand
    {
        /// <summary>SimConnect object ID of the target aircraft.</summary>
        public uint TargetObjectId { get; set; }

        /// <summary>Type of command being issued.</summary>
        public AtcCommandType CommandType { get; set; }

        /// <summary>Numeric value associated with the command (altitude ft, heading deg, etc.).</summary>
        public double Value { get; set; }

        /// <summary>Free-text instruction (used for TTS generation in a later phase).</summary>
        public string? TextInstruction { get; set; }

        /// <summary>UTC timestamp when the command was created.</summary>
        public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Identifies the type of ATC instruction.</summary>
    public enum AtcCommandType
    {
        /// <summary>Assign a target altitude (feet MSL).</summary>
        AltitudeAssignment,

        /// <summary>Assign a heading (degrees true).</summary>
        HeadingVector,

        /// <summary>Assign a speed (knots).</summary>
        SpeedRestriction,

        /// <summary>Clear the aircraft for takeoff.</summary>
        TakeoffClearance,

        /// <summary>Clear the aircraft to land.</summary>
        LandingClearance,

        /// <summary>Initiate pushback from gate.</summary>
        Pushback,

        /// <summary>Issue a taxi clearance.</summary>
        TaxiClearance,

        /// <summary>Scramble order (QRA – Quick Reaction Alert).</summary>
        Scramble,
    }
}
