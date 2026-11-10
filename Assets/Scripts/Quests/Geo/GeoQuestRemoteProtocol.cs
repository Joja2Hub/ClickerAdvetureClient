using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace ClickerAdventure
{
    public interface IGeoQuestRemoteClient
    {
        Task<GeoServerProgress> LoadGeoProgressAsync(string childId, GeoQuestDefinition definition);
        Task<GeoServerProgress> CheckInGeoQuestAsync(string childId, GeoQuestDefinition definition, GeoLocationReading reading);
    }

    public sealed class GeoServerProgress
    {
        public int VisitCount { get; }
        public string Status { get; }
        public bool AlreadyVisitedToday { get; }
        public bool CanCheckIn { get; }

        public GeoServerProgress(int count, int goal, string status, bool alreadyVisitedToday = false)
        {
            if (goal < 1 || count < 0 || count > goal
                || Array.IndexOf(new[] { "assigned", "rejected", "submitted", "approved", "claimed", "cancelled" }, status) < 0)
                throw new FormatException("Invalid server geo progress.");
            VisitCount = count;
            Status = status;
            AlreadyVisitedToday = alreadyVisitedToday;
            CanCheckIn = count < goal && (status == "assigned" || status == "rejected");
        }
    }

    public static class GeoQuestRemoteProtocol
    {
        public static void ValidateId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Contains("/") || value == "." || value == "..")
                throw new ArgumentException("Invalid child or quest ID.");
        }

        public static Dictionary<string, object> Request(string childId, string questId, GeoLocationReading reading)
        {
            ValidateId(childId);
            ValidateId(questId);
            if (reading == null || !GeoDistance.IsValidCoordinate(reading.Latitude, reading.Longitude)
                || double.IsNaN(reading.AccuracyMeters) || double.IsInfinity(reading.AccuracyMeters) || reading.AccuracyMeters < 0)
                throw new ArgumentException("Invalid location reading.");
            return new Dictionary<string, object> { ["data"] = new Dictionary<string, object>
            {
                ["childId"] = childId, ["questId"] = questId,
                ["latitude"] = reading.Latitude, ["longitude"] = reading.Longitude,
                ["accuracyMeters"] = reading.AccuracyMeters,
                ["observedAtUnixSeconds"] = reading.ObservedAt.ToUnixTimeSeconds()
            } };
        }

        public static GeoServerProgress Response(Dictionary<string, object> response, int goal)
        {
            if ((!response.TryGetValue("result", out var raw) && !response.TryGetValue("data", out raw))
                || !(raw is Dictionary<string, object> result)) throw new FormatException("Invalid callable response.");
            if (Int(result, "requiredVisits") != goal || !result.TryGetValue("alreadyVisitedToday", out var repeat) || !(repeat is bool))
                throw new FormatException("Server geo conditions do not match.");
            int count = Int(result, "visitCount");
            if (count < 1) throw new FormatException("Server did not confirm a visit.");
            return new GeoServerProgress(count, goal, Text(result, "status"), (bool)repeat);
        }

        public static GeoServerProgress Document(Dictionary<string, object> document, GeoQuestDefinition expected)
        {
            if (!document.TryGetValue("fields", out var raw) || !(raw is Dictionary<string, object> fields))
                throw new FormatException("Quest document is missing.");
            var actual = GeoQuestFirestoreReader.Read(fields, expected.Id, expected.Title);
            if (actual == null || GeoQuestProgressData.DefinitionHash(actual) != GeoQuestProgressData.DefinitionHash(expected))
                throw new FormatException("Geo quest conditions changed. Reopen the quest.");
            string status = Text(Map(fields, "status"), "stringValue");
            int count = 0;
            if (fields.ContainsKey("geoProgress"))
            {
                var progress = Map(Map(Map(fields, "geoProgress"), "mapValue"), "fields");
                count = GeoQuestFirestoreReader.Integer(progress, "visitCount");
                // The server owns this counter; never merge it with the local demo cache.
                string hash = Text(Map(progress, "definitionHash"), "stringValue");
                if (hash.Length != 64 || !Array.TrueForAll(hash.ToCharArray(), Uri.IsHexDigit))
                    throw new FormatException("Invalid geo progress fingerprint.");
            }
            return new GeoServerProgress(count, expected.RequiredVisits, status);
        }

        private static Dictionary<string, object> Map(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out var value) && value is Dictionary<string, object> map
                ? map : throw new FormatException("Missing server field: " + key);
        private static string Text(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out var value) && value is string text ? text : throw new FormatException("Invalid server field: " + key);
        private static int Int(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || !(value is long || value is int || value is double))
                throw new FormatException("Invalid server count.");
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || number < 0 || number > int.MaxValue || number != Math.Truncate(number))
                throw new FormatException("Invalid server count.");
            return (int)number;
        }
    }
}
