using System;

namespace ClickerAdventure
{
    public static class GeoDistance
    {
        private const double EarthRadiusMeters = 6371000;

        public static bool IsValidCoordinate(double latitude, double longitude)
        {
            return !double.IsNaN(latitude) && !double.IsNaN(longitude)
                && latitude >= -90 && latitude <= 90
                && longitude >= -180 && longitude <= 180;
        }

        public static double MetersBetween(double latitudeA, double longitudeA,
            double latitudeB, double longitudeB)
        {
            if (!IsValidCoordinate(latitudeA, longitudeA)
                || !IsValidCoordinate(latitudeB, longitudeB))
                throw new ArgumentOutOfRangeException(nameof(latitudeA), "Invalid coordinates.");

            const double radians = Math.PI / 180;
            double deltaLatitude = (latitudeB - latitudeA) * radians;
            double deltaLongitude = (longitudeB - longitudeA) * radians;
            double sinLatitude = Math.Sin(deltaLatitude / 2);
            double sinLongitude = Math.Sin(deltaLongitude / 2);
            double a = sinLatitude * sinLatitude
                + Math.Cos(latitudeA * radians) * Math.Cos(latitudeB * radians)
                * sinLongitude * sinLongitude;
            return EarthRadiusMeters * 2 * Math.Asin(Math.Sqrt(Math.Max(0, Math.Min(1, a))));
        }
    }
}

