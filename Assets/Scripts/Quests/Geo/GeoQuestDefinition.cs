using System;

namespace ClickerAdventure
{
    /// <summary>A single visit goal within a fixed time window. Times are Unix seconds.</summary>
    [Serializable]
    public class GeoQuestDefinition
    {
        public string Id;
        public string Title;
        public double Latitude;
        public double Longitude;
        public double RadiusMeters = 100;
        public int RequiredVisits = 5;
        public long StartsAtUnixSeconds;
        public long EndsAtUnixSeconds;
        public int UtcOffsetMinutes = 180;
    }

    public enum GeoVisitResult
    {
        Counted,
        OutsidePeriod,
        InvalidLocation,
        InaccurateLocation,
        OutsideArea,
        AlreadyVisitedToday,
        Completed
    }
}

