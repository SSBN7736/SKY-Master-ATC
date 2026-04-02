using SkyMasterATC.Core.Models;

namespace SkyMasterATC.Core.Services
{
    /// <summary>
    /// Stub for a future radar renderer loop.
    /// Phase 3 will replace this with a real GDI+/Skia rendering pipeline.
    /// </summary>
    public sealed class RadarRendererService
    {
        private readonly List<RadarTrack> _tracks = new();

        /// <summary>Current set of tracks to render.</summary>
        public IReadOnlyList<RadarTrack> Tracks => _tracks;

        /// <summary>
        /// Adds or updates a track from an updated <see cref="SimAircraft"/>.
        /// </summary>
        public void UpdateTrack(SimAircraft aircraft)
        {
            var existing = _tracks.FirstOrDefault(t => t.Aircraft.ObjectId == aircraft.ObjectId);
            if (existing is not null)
                _tracks.Remove(existing);

            _tracks.Add(new RadarTrack { Aircraft = aircraft });
        }

        /// <summary>Removes a track by object ID when the aircraft is removed from the sim.</summary>
        public void RemoveTrack(uint objectId)
        {
            _tracks.RemoveAll(t => t.Aircraft.ObjectId == objectId);
        }

        /// <summary>
        /// Stub render call.  Phase 3 will accept a drawing context and paint
        /// all tracks with data tags (callsign, altitude, speed, squawk).
        /// </summary>
        public void Render()
        {
            // TODO (Phase 3): iterate _tracks and draw each RadarTrack on the canvas.
        }
    }
}
