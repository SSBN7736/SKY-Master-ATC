namespace SkyMasterATC.Core.Util
{
    /// <summary>
    /// Geographic coordinate utility methods used by the radar renderer and
    /// ATC controller logic.
    /// </summary>
    public static class Geo
    {
        private const double EarthRadiusNm = 3440.065;

        /// <summary>
        /// Returns the great-circle distance in nautical miles between two
        /// latitude/longitude positions.
        /// </summary>
        public static double DistanceNm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return EarthRadiusNm * 2 * Math.Asin(Math.Sqrt(a));
        }

        /// <summary>Returns the initial true bearing (degrees) from point 1 to point 2.</summary>
        public static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var y = Math.Sin(dLon) * Math.Cos(ToRadians(lat2));
            var x = Math.Cos(ToRadians(lat1)) * Math.Sin(ToRadians(lat2))
                  - Math.Sin(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Cos(dLon);
            return (ToDegrees(Math.Atan2(y, x)) + 360) % 360;
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;
        private static double ToDegrees(double rad) => rad * 180.0 / Math.PI;
    }
}
