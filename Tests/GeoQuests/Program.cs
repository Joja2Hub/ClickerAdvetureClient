using ClickerAdventure;
using System;

internal static class Program
{
    private static int checks;

    private static void Equal<T>(T expected, T actual)
    {
        checks++;
        if (!Equals(expected, actual))
            throw new Exception($"Check {checks}: expected {expected}, got {actual}");
    }

    private static void Main()
    {
        var start = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.FromHours(3));
        var definition = new GeoQuestDefinition
        {
            Id = "school-week", Title = "Школа: 5 дней за неделю",
            Latitude = 55.75, Longitude = 37.62, RadiusMeters = 100,
            RequiredVisits = 5, UtcOffsetMinutes = 180,
            StartsAtUnixSeconds = start.ToUnixTimeSeconds(),
            EndsAtUnixSeconds = start.AddDays(7).ToUnixTimeSeconds()
        };
        var tracker = new GeoQuestTracker(definition);
        Equal(GeoVisitResult.OutsidePeriod, tracker.RegisterVisit(55.75, 37.62, 5, start.AddSeconds(-1)));
        Equal(GeoVisitResult.OutsidePeriod, tracker.RegisterVisit(55.75, 37.62, 5, start.AddDays(7)));
        Equal(GeoVisitResult.InvalidLocation, tracker.RegisterVisit(double.NaN, 37.62, 5, start));
        Equal(GeoVisitResult.InvalidLocation, tracker.RegisterVisit(91, 37.62, 5, start));
        Equal(GeoVisitResult.InaccurateLocation, tracker.RegisterVisit(55.75, 37.62, 101, start));
        Equal(GeoVisitResult.InaccurateLocation, tracker.RegisterVisit(55.75, 37.62, -1, start));
        Equal(GeoVisitResult.InaccurateLocation, tracker.RegisterVisit(55.75, 37.62, double.NaN, start));
        Equal(GeoVisitResult.OutsideArea, tracker.RegisterVisit(56, 37.62, 5, start));
        Equal(GeoVisitResult.OutsideArea, tracker.RegisterVisit(55.7508, 37.62, 20, start));
        Equal(0, tracker.VisitCount);
        Equal(GeoVisitResult.Counted, tracker.RegisterVisit(55.75, 37.62, 5, start));
        Equal(GeoVisitResult.AlreadyVisitedToday, tracker.RegisterVisit(55.75, 37.62, 5, start.AddHours(23)));
        Equal(GeoVisitResult.AlreadyVisitedToday, tracker.RegisterVisit(55.75, 37.62, 5, start.ToUniversalTime()));
        for (int day = 1; day < 5; day++)
            Equal(GeoVisitResult.Counted, tracker.RegisterVisit(55.75, 37.62, 5, start.AddDays(day)));
        Equal(5, tracker.VisitCount);
        Equal(true, tracker.IsCompleted);
        Equal(GeoVisitResult.Completed, tracker.RegisterVisit(55.75, 37.62, 5, start.AddDays(5)));
        Equal(5, tracker.VisitCount);
        Equal(0.0, GeoDistance.MetersBetween(0, 0, 0, 0));
        Equal(true, Math.Abs(GeoDistance.MetersBetween(0, 0, 0, 1) - 111195) < 1);
        Equal(true, GeoDistance.MetersBetween(0, 179.999, 0, -179.999) < 223);
        Equal(true, double.IsFinite(GeoDistance.MetersBetween(0, 0, 0, 180)));
        definition.RequiredVisits = 0;
        try
        {
            new GeoQuestTracker(definition);
            throw new Exception("Invalid goal accepted.");
        }
        catch (ArgumentException) { checks++; }
        Equal(5, tracker.VisitCount);
        var reading = new GeoLocationReading(55.75, 37.62, 5, start);
        Equal(true, reading.IsUsableAt(start, TimeSpan.FromSeconds(30)));
        Equal(true, reading.IsUsableAt(start.AddSeconds(30), TimeSpan.FromSeconds(30)));
        Equal(false, reading.IsUsableAt(start.AddSeconds(31), TimeSpan.FromSeconds(30)));
        Equal(false, reading.IsUsableAt(start.AddSeconds(-1), TimeSpan.FromSeconds(30)));
        Equal(false, reading.IsUsableAt(start, TimeSpan.FromSeconds(-1)));
        Equal(false, new GeoLocationReading(91, 0, 5, start).IsUsableAt(start, TimeSpan.Zero));
        Equal(false, new GeoLocationReading(0, 0, double.NaN, start).IsUsableAt(start, TimeSpan.Zero));
        Equal(false, new GeoLocationReading(0, 0, -1, start).IsUsableAt(start, TimeSpan.Zero));
        Console.WriteLine($"Geo quests: {checks} checks passed.");
    }
}

