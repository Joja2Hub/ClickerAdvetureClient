using System;
using System.Collections.Generic;
using System.Globalization;

namespace ClickerAdventure
{
    /// <summary>Reads the optional versioned geo map from Firestore REST fields.</summary>
    public static class GeoQuestFirestoreReader
    {
        public static GeoQuestDefinition Read(Dictionary<string, object> fields, string questId, string title)
        {
            if (!fields.TryGetValue("geo", out var raw)) return null;
            if (raw is Dictionary<string, object> nullable && nullable.ContainsKey("nullValue")) return null;
            if (!(raw is Dictionary<string, object> value)
                || !value.TryGetValue("mapValue", out var map) || !(map is Dictionary<string, object> document)
                || !document.TryGetValue("fields", out var nested) || !(nested is Dictionary<string, object> geo))
                throw new FormatException("Geo conditions must be a Firestore map.");
            if (Integer(geo, "version") != 1) throw new FormatException("Unsupported geo conditions version.");
            if (string.IsNullOrWhiteSpace(questId)) throw new FormatException("Quest ID is required.");

            var definition = new GeoQuestDefinition
            {
                Id = questId, Title = title,
                Latitude = Number(geo, "latitude"), Longitude = Number(geo, "longitude"),
                RadiusMeters = Number(geo, "radiusMeters"), RequiredVisits = Integer(geo, "requiredVisits"),
                StartsAtUnixSeconds = UnixSeconds(geo, "startsAtUnixSeconds"),
                EndsAtUnixSeconds = UnixSeconds(geo, "endsAtUnixSeconds"),
                UtcOffsetMinutes = Integer(geo, "utcOffsetMinutes")
            };
            try
            {
                new GeoQuestTracker(definition);
                var offset = TimeSpan.FromMinutes(definition.UtcOffsetMinutes);
                DateTimeOffset.FromUnixTimeSeconds(definition.StartsAtUnixSeconds).ToOffset(offset);
                DateTimeOffset.FromUnixTimeSeconds(definition.EndsAtUnixSeconds).ToOffset(offset);
            }
            catch (ArgumentException exception) { throw new FormatException("Invalid geo quest conditions.", exception); }
            return definition;
        }

        private static double Number(Dictionary<string, object> fields, string name)
        {
            if (fields.TryGetValue(name, out var raw) && raw is Dictionary<string, object> field)
            {
                if (field.TryGetValue("integerValue", out var integer) && integer is string text
                    && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)) return parsed;
                if (field.TryGetValue("doubleValue", out var number)
                    && (number is double || number is float || number is long || number is int))
                {
                    double result = Convert.ToDouble(number, CultureInfo.InvariantCulture);
                    if (!double.IsNaN(result) && !double.IsInfinity(result)) return result;
                }
            }
            throw new FormatException("Missing or invalid geo field: " + name);
        }

        private static int Integer(Dictionary<string, object> fields, string name)
        {
            double number = Number(fields, name);
            if (number != Math.Truncate(number) || number < int.MinValue || number > int.MaxValue)
                throw new FormatException("Geo field must be an integer: " + name);
            return (int)number;
        }

        private static long UnixSeconds(Dictionary<string, object> fields, string name)
        {
            double number = Number(fields, name);
            if (number != Math.Truncate(number) || number < -62135596800L || number > 253402300799L)
                throw new FormatException("Invalid geo time: " + name);
            return (long)number;
        }
    }
}
