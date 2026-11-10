using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClickerAdventure;

internal static class RemoteChecks
{
    public static async Task<int> Run()
    {
        int checks = 0;
        void Check(bool value) { checks++; if (!value) throw new Exception($"Remote geo check {checks} failed."); }
        var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var definition = new GeoQuestDefinition { Id = "school", RequiredVisits = 5 };
        var reading = new GeoLocationReading(55.750001, 37.620002, 5.5, now);
        var payload = GeoQuestRemoteProtocol.Request("child", "school", reading);
        var wire = FirebaseJson.ParseObject(FirebaseJson.Serialize(payload));
        var data = (Dictionary<string, object>)wire["data"];
        Check((string)data["childId"] == "child" && (string)data["questId"] == "school");
        Check(Convert.ToDouble(data["latitude"]) == reading.Latitude);
        Check(Convert.ToInt64(data["observedAtUnixSeconds"]) == now.ToUnixTimeSeconds());
        Check(!data.ContainsKey("visitCount") && !data.ContainsKey("status"));
        try { GeoQuestRemoteProtocol.Request("child", "../school", reading); Check(false); }
        catch (ArgumentException) { Check(true); }

        var body = new Dictionary<string, object> { ["visitCount"] = 1L, ["requiredVisits"] = 5L,
            ["status"] = "assigned", ["alreadyVisitedToday"] = false };
        var response = new Dictionary<string, object> { ["result"] = body };
        Check(GeoQuestRemoteProtocol.Response(response, 5).VisitCount == 1);
        Check(GeoQuestRemoteProtocol.Response(new Dictionary<string, object> { ["data"] = body }, 5).CanCheckIn);
        foreach (var invalid in new[] { -1.0, 0.0, 1.5, 6.0, double.NaN })
        {
            body["visitCount"] = invalid;
            try { GeoQuestRemoteProtocol.Response(response, 5); Check(false); }
            catch (FormatException) { Check(true); }
        }
        body["visitCount"] = 1L;
        body["requiredVisits"] = 6L;
        try { GeoQuestRemoteProtocol.Response(response, 5); Check(false); }
        catch (FormatException) { Check(true); }
        body["requiredVisits"] = 5L;
        body["status"] = "unknown";
        try { GeoQuestRemoteProtocol.Response(response, 5); Check(false); }
        catch (FormatException) { Check(true); }

        object Integer(long value) => new Dictionary<string, object> { ["integerValue"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        object Number(double value) => new Dictionary<string, object> { ["doubleValue"] = value };
        object Text(string value) => new Dictionary<string, object> { ["stringValue"] = value };
        object Map(object fields) => new Dictionary<string, object> { ["mapValue"] = new Dictionary<string, object> { ["fields"] = fields } };
        var geoFields = new Dictionary<string, object>
        {
            ["version"] = Integer(1), ["latitude"] = Number(55.75), ["longitude"] = Number(37.62),
            ["radiusMeters"] = Integer(100), ["requiredVisits"] = Integer(5), ["utcOffsetMinutes"] = Integer(180),
            ["startsAtUnixSeconds"] = Integer(now.AddDays(-1).ToUnixTimeSeconds()),
            ["endsAtUnixSeconds"] = Integer(now.AddDays(6).ToUnixTimeSeconds())
        };
        var fields = new Dictionary<string, object> { ["geo"] = Map(geoFields), ["status"] = Text("assigned") };
        var expected = GeoQuestFirestoreReader.Read(fields, "school", "School");
        var document = new Dictionary<string, object> { ["fields"] = fields };
        Check(GeoQuestRemoteProtocol.Document(document, expected).VisitCount == 0);
        var storedProgress = new Dictionary<string, object> { ["visitCount"] = Integer(2), ["definitionHash"] = Text(new string('a', 64)) };
        fields["geoProgress"] = Map(storedProgress);
        Check(GeoQuestRemoteProtocol.Document(document, expected).VisitCount == 2);
        geoFields["radiusMeters"] = Integer(200);
        try { GeoQuestRemoteProtocol.Document(document, expected); Check(false); }
        catch (FormatException) { Check(true); }
        geoFields["radiusMeters"] = Integer(100);
        storedProgress["visitCount"] = Integer(6);
        try { GeoQuestRemoteProtocol.Document(document, expected); Check(false); }
        catch (FormatException) { Check(true); }
        storedProgress["visitCount"] = Integer(2);
        fields["status"] = Text("cancelled");
        Check(!GeoQuestRemoteProtocol.Document(document, expected).CanCheckIn);

        var source = new Source { Reading = reading };
        var remote = new Remote();
        var service = new GeoQuestCheckInService(source, null, () => now, remote: remote);
        var result = await service.CheckInAsync("child", definition);
        Check(result.VisitCount == 1 && result.VisitResult == GeoVisitResult.Counted && result.RemoteStatus == "assigned");
        Check(remote.Loads == 1 && remote.Claims == 1 && ReferenceEquals(remote.Reading, reading));
        remote.Repeat = true;
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitResult == GeoVisitResult.AlreadyVisitedToday && result.VisitCount == 1);
        remote.Status = "submitted";
        remote.Count = 5;
        int requests = source.Requests;
        result = await service.CheckInAsync("child", definition);
        Check(result.RemoteStatus == "submitted" && result.VisitCount == 5 && source.Requests == requests);

        remote.Status = "assigned";
        remote.Count = 1;
        remote.Repeat = false;
        remote.FailAfterCommit = true;
        try { await service.CheckInAsync("child", definition); Check(false); }
        catch (InvalidOperationException) { Check(true); }
        Check(remote.Count == 2);
        remote.FailAfterCommit = false;
        remote.Repeat = true;
        result = await service.CheckInAsync("child", definition);
        Check(result.VisitCount == 2 && result.VisitResult == GeoVisitResult.AlreadyVisitedToday);
        source.Reading = new GeoLocationReading(55.75, 37.62, 5, now.AddMinutes(-1));
        int claims = remote.Claims;
        result = await service.CheckInAsync("child", definition);
        Check(result.Status == GeoCheckInStatus.InvalidReading && remote.Claims == claims);
        remote.Status = "cancelled";
        result = await service.CheckInAsync("child", definition);
        Check(result.RemoteStatus == "cancelled" && remote.Claims == claims);
        return checks;
    }

    private sealed class Source : IGeoLocationSource
    {
        public GeoLocationReading Reading;
        public int Requests;
        public void RequestLocation(Action<GeoLocationReading> success, Action<GeoLocationError> failure)
        { Requests++; success(Reading); }
    }
    private sealed class Remote : IGeoQuestRemoteClient
    {
        public int Loads, Claims, Count;
        public bool Repeat, FailAfterCommit;
        public string Status = "assigned";
        public GeoLocationReading Reading;
        public Task<GeoServerProgress> LoadGeoProgressAsync(string childId, GeoQuestDefinition definition)
        { Loads++; return Task.FromResult(new GeoServerProgress(Count, 5, Status)); }
        public Task<GeoServerProgress> CheckInGeoQuestAsync(string childId, GeoQuestDefinition definition, GeoLocationReading reading)
        {
            Claims++; Reading = reading;
            if (!Repeat) Count++;
            if (FailAfterCommit) throw new InvalidOperationException("Response lost after commit");
            return Task.FromResult(new GeoServerProgress(Count, 5, Status, Repeat));
        }
    }
}
