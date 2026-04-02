namespace SkyMasterATC.Core.Models
{
    /// <summary>
    /// Represents a live AI aircraft object tracked within the simulator.
    /// Populated from SimConnect <c>SIMCONNECT_DATA_INITPOSITION</c> / object data.
    /// </summary>
    public sealed class SimAircraft
    {
        /// <summary>SimConnect object ID assigned when the aircraft was spawned.</summary>
        public uint ObjectId { get; set; }

        /// <summary>ICAO callsign used by the AI flight (e.g., "AAL123").</summary>
        public string Callsign { get; set; } = string.Empty;

        /// <summary>ICAO type designator of the aircraft (e.g., "B738").</summary>
        public string TypeDesignator { get; set; } = string.Empty;

        /// <summary>Current latitude in decimal degrees.</summary>
        public double Latitude { get; set; }

        /// <summary>Current longitude in decimal degrees.</summary>
        public double Longitude { get; set; }

        /// <summary>Current altitude in feet MSL.</summary>
        public double AltitudeFt { get; set; }

        /// <summary>Current indicated airspeed in knots.</summary>
        public double SpeedKnots { get; set; }

        /// <summary>Current heading (true) in degrees.</summary>
        public double HeadingDeg { get; set; }

        /// <summary>Assigned transponder squawk code (e.g., "2000").</summary>
        public string Squawk { get; set; } = "2000";

        /// <summary>Time (UTC) at which this record was last updated.</summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
