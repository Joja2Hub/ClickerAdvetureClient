using System;

namespace ClickerAdventure
{
    public sealed class GeoLocationReading
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public double AccuracyMeters { get; }
        public DateTimeOffset ObservedAt { get; }

        public GeoLocationReading(double latitude, double longitude, double accuracyMeters,
            DateTimeOffset observedAt)
        {
            Latitude = latitude;
            Longitude = longitude;
            AccuracyMeters = accuracyMeters;
            ObservedAt = observedAt;
        }

        public bool IsUsableAt(DateTimeOffset now, TimeSpan maximumAge)
        {
            return maximumAge >= TimeSpan.Zero && ObservedAt <= now
                && now - ObservedAt <= maximumAge
                && GeoDistance.IsValidCoordinate(Latitude, Longitude)
                && !double.IsNaN(AccuracyMeters) && !double.IsInfinity(AccuracyMeters)
                && AccuracyMeters >= 0;
        }
    }
}

