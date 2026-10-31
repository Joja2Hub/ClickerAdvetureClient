using System;
using System.IO;
using System.Threading.Tasks;
using ClickerAdventure;

internal static class CheckInChecks
{
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool value) { count++; if (!value) throw new Exception($"Check-in check {count} failed."); }
        var start = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.FromHours(3));
        var now = start.AddHours(10);
        var definition = new GeoQuestDefinition
        {
            Id = "school", Latitude = 55.75, Longitude = 37.62, RequiredVisits = 5,
            StartsAtUnixSeconds = start.ToUnixTimeSeconds(), EndsAtUnixSeconds = start.AddDays(7).ToUnixTimeSeconds()
        };
        var source = new LocationSource();
        var store = new ProgressStore();
        var service = new GeoQuestCheckInService(source, store, () => now);
        GeoLocationReading Reading() => new GeoLocationReading(55.75, 37.62, 5, now);
        source.Reading = Reading();
        var result = await service.CheckInAsync("child", definition);
        Check(result.Status == GeoCheckInStatus.VisitChecked && result.VisitResult == GeoVisitResult.Counted);
        Check(result.VisitCount == 1 && store.Saves == 1);
        Check(store.LastChild == "child" && store.LastQuest == "school");
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitResult == GeoVisitResult.AlreadyVisitedToday && result.VisitCount == 1);
        Check(store.Saves == 1);

        now = now.AddDays(1);
        source.Reading = Reading();
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitResult == GeoVisitResult.Counted && result.VisitCount == 2);
        foreach (GeoLocationError error in Enum.GetValues(typeof(GeoLocationError)))
        {
            source.Error = error;
            result = await service.CheckInAsync("child", definition);
            Check(result.Status == GeoCheckInStatus.LocationFailed && result.LocationError == error && result.VisitCount == 2);
        }
        Check(store.Saves == 2);
        source.Error = null;
        source.Reading = new GeoLocationReading(55.75, 37.62, 5, now.AddMinutes(-1));
        Check((await service.CheckInAsync("child", definition)).Status == GeoCheckInStatus.InvalidReading);
        source.Reading = new GeoLocationReading(55.75, 37.62, 5, now.AddSeconds(1));
        Check((await service.CheckInAsync("child", definition)).Status == GeoCheckInStatus.InvalidReading);
        source.Reading = null;
        Check((await service.CheckInAsync("child", definition)).Status == GeoCheckInStatus.InvalidReading);
        source.Reading = new GeoLocationReading(56, 37.62, 5, now);
        Check((await service.CheckInAsync("child", definition)).VisitResult == GeoVisitResult.OutsideArea);
        Check(store.Saves == 2);

        now = now.AddDays(1);
        source.Hold = true;
        Task<GeoCheckInResult> pending = service.CheckInAsync("child", definition);
        int requests = source.Requests;
        Check((await service.CheckInAsync("child", definition)).Status == GeoCheckInStatus.Busy);
        Check(source.Requests == requests);
        source.Complete(Reading());
        result = await pending;
        Check(result.VisitResult == GeoVisitResult.Counted && result.VisitCount == 3);
        source.Hold = false;

        now = now.AddDays(1);
        source.Reading = Reading();
        store.FailSave = true;
        try { await service.CheckInAsync("child", definition); Check(false); }
        catch (IOException) { Check(true); }
        Check(store.Load("child", definition).VisitCount == 3);
        store.FailSave = false;
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitResult == GeoVisitResult.Counted && result.VisitCount == 4);
        now = now.AddDays(1);
        source.Reading = Reading();
        Check((await service.CheckInAsync("child", definition)).VisitCount == 5);
        requests = source.Requests;
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitResult == GeoVisitResult.Completed && result.VisitCount == 5);
        Check(source.Requests == requests);
        return count;
    }

    private sealed class LocationSource : IGeoLocationSource
    {
        public GeoLocationReading Reading;
        public GeoLocationError? Error;
        public bool Hold;
        public int Requests;
        private Action<GeoLocationReading> pending;
        public void RequestLocation(Action<GeoLocationReading> success, Action<GeoLocationError> failure)
        {
            Requests++;
            if (Hold) pending = success;
            else if (Error.HasValue) failure(Error.Value);
            else success(Reading);
        }
        public void Complete(GeoLocationReading reading) { var callback = pending; pending = null; callback(reading); }
    }

    private sealed class ProgressStore : IGeoQuestProgressStore
    {
        public bool FailSave;
        public int Saves;
        public string LastChild;
        public string LastQuest;
        private GeoQuestProgressData data;
        public GeoQuestTracker Load(string childId, GeoQuestDefinition definition) => new GeoQuestTracker(definition, data);
        public void Save(string childId, GeoQuestTracker tracker)
        {
            if (FailSave) throw new IOException("Disk unavailable");
            Saves++;
            LastChild = childId;
            LastQuest = tracker.QuestId;
            data = tracker.CreateSaveData();
        }
    }
}
