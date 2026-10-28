using System;
using System.Collections.Generic;

namespace ClickerAdventure
{
    /// <summary>
    /// In-memory visit counter. The caller supplies a fresh location and its observation time.
    /// One visit per calendar day in the quest's fixed UTC offset; end time is exclusive.
    /// </summary>
    public sealed class GeoQuestTracker
    {
        private readonly double latitude;
        private readonly double longitude;
        private readonly double radiusMeters;
        private readonly int requiredVisits;
        private readonly DateTimeOffset startsAt;
        private readonly DateTimeOffset endsAt;
        private readonly TimeSpan utcOffset;
        private readonly HashSet<DateTime> visitedDays = new HashSet<DateTime>();

        public int VisitCount => visitedDays.Count;
        public bool IsCompleted => VisitCount >= requiredVisits;

        public GeoQuestTracker(GeoQuestDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!GeoDistance.IsValidCoordinate(definition.Latitude, definition.Longitude))
                throw new ArgumentException("Invalid quest coordinates.", nameof(definition));
            if (double.IsNaN(definition.RadiusMeters) || double.IsInfinity(definition.RadiusMeters)
                || definition.RadiusMeters <= 0 || definition.RequiredVisits <= 0)
                throw new ArgumentException("Radius and visit goal must be positive.", nameof(definition));
            if (definition.StartsAtUnixSeconds >= definition.EndsAtUnixSeconds)
                throw new ArgumentException("The end must be after the start.", nameof(definition));
            if (definition.UtcOffsetMinutes < -840 || definition.UtcOffsetMinutes > 840)
                throw new ArgumentException("UTC offset must be within 14 hours.", nameof(definition));

            latitude = definition.Latitude;
            longitude = definition.Longitude;
            radiusMeters = definition.RadiusMeters;
            requiredVisits = definition.RequiredVisits;
            startsAt = DateTimeOffset.FromUnixTimeSeconds(definition.StartsAtUnixSeconds);
            endsAt = DateTimeOffset.FromUnixTimeSeconds(definition.EndsAtUnixSeconds);
            utcOffset = TimeSpan.FromMinutes(definition.UtcOffsetMinutes);
        }

        public GeoVisitResult RegisterVisit(double observedLatitude, double observedLongitude,
            double accuracyMeters, DateTimeOffset observedAt)
        {
            if (IsCompleted)
                return GeoVisitResult.Completed;
            if (observedAt < startsAt || observedAt >= endsAt)
                return GeoVisitResult.OutsidePeriod;
            if (!GeoDistance.IsValidCoordinate(observedLatitude, observedLongitude))
                return GeoVisitResult.InvalidLocation;
            if (double.IsNaN(accuracyMeters) || double.IsInfinity(accuracyMeters)
                || accuracyMeters < 0 || accuracyMeters > radiusMeters)
                return GeoVisitResult.InaccurateLocation;

            double distance = GeoDistance.MetersBetween(latitude, longitude,
                observedLatitude, observedLongitude);
            // Require the entire reported accuracy circle to fit inside the target area.
            if (distance + accuracyMeters > radiusMeters)
                return GeoVisitResult.OutsideArea;

            DateTime day = observedAt.ToOffset(utcOffset).Date;
            if (!visitedDays.Add(day))
                return GeoVisitResult.AlreadyVisitedToday;

            return GeoVisitResult.Counted;
        }
    }
}

