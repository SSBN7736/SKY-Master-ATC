using SkyMasterATC.Core.Models;

namespace SkyMasterATC.Core.Services
{
    /// <summary>
    /// Maintains the set of radar tracks and provides coordinate-to-screen mapping.
    /// Raises <see cref="TracksUpdated"/> whenever the track list changes so the
    /// radar UI can redraw.
    /// </summary>
    public sealed class RadarRendererService
    {
        private readonly List<RadarTrack> _tracks = new();

        /// <summary>Raised whenever a track is added, updated, or removed.</summary>
        public event EventHandler? TracksUpdated;

        /// <summary>Current set of tracks.</summary>
        public IReadOnlyList<RadarTrack> Tracks => _tracks;

        /// <summary>
        /// Adds or updates a track from an updated <see cref="SimAircraft"/>.
        /// </summary>
        public void UpdateTrack(SimAircraft aircraft)
        {
            var existing = _tracks.FirstOrDefault(t => t.Aircraft.ObjectId == aircraft.ObjectId);
            bool selected = existing?.IsSelected ?? false;

            if (existing is not null)
                _tracks.Remove(existing);

            _tracks.Add(new RadarTrack { Aircraft = aircraft, IsSelected = selected });
            TracksUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Removes a track by object ID.</summary>
        public void RemoveTrack(uint objectId)
        {
            _tracks.RemoveAll(t => t.Aircraft.ObjectId == objectId);
            TracksUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Marks the specified track as selected and clears all others.</summary>
        public void SetSelected(uint objectId)
        {
            foreach (var t in _tracks)
                t.IsSelected = t.Aircraft.ObjectId == objectId;
        }

        // ── Coordinate maths (static so RadarControl can call without a service ref) ──

        /// <summary>
        /// Converts a geographic coordinate to a pixel position on the radar display.
        /// The radar is centred on (<paramref name="centerLat"/>, <paramref name="centerLon"/>)
        /// and covers ±<paramref name="rangeNm"/> nautical miles in each axis.
        /// </summary>
        public static (float X, float Y) LatLonToScreen(
            double lat, double lon,
            double centerLat, double centerLon,
            double rangeNm,
            double widthPx, double heightPx)
        {
            double dLat   = lat - centerLat;
            double dLon   = lon - centerLon;
            double cosLat = Math.Cos(centerLat * Math.PI / 180.0);

            double dxNm = dLon * 60.0 * cosLat;
            double dyNm = dLat * 60.0;

            double scale = Math.Min(widthPx, heightPx) / 2.0 / rangeNm;

            float x = (float)(widthPx  / 2.0 + dxNm * scale);
            float y = (float)(heightPx / 2.0 - dyNm * scale);
            return (x, y);
        }
    }
}
