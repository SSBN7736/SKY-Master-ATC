namespace SkyMasterATC.Core.Models
{
    /// <summary>
    /// A snapshot of one radar return, derived from a <see cref="SimAircraft"/>.
    /// Carries all data needed to paint a single data tag on the radar display.
    /// </summary>
    public sealed class RadarTrack
    {
        /// <summary>Source object this track was derived from.</summary>
        public required SimAircraft Aircraft { get; init; }

        /// <summary>Radar screen X position in pixels (set by the renderer).</summary>
        public float ScreenX { get; set; }

        /// <summary>Radar screen Y position in pixels (set by the renderer).</summary>
        public float ScreenY { get; set; }

        /// <summary>Whether the data tag is currently selected by the controller.</summary>
        public bool IsSelected { get; set; }
    }
}
