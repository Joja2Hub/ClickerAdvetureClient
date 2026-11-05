using System;
using System.Collections.Generic;
using System.Globalization;
using ClickerAdventure;

internal static class FirestoreReaderChecks
{
    public static int Run()
    {
        int checks = 0;
        void Check(bool value) { checks++; if (!value) throw new Exception($"Geo Firestore check {checks} failed."); }
        object Integer(long value) => new Dictionary<string, object> { ["integerValue"] = value.ToString(CultureInfo.InvariantCulture) };
        object Number(double value) => new Dictionary<string, object> { ["doubleValue"] = value };
        var geo = new Dictionary<string, object>
        {
            ["version"] = Integer(1), ["latitude"] = Number(55.75), ["longitude"] = Number(37.62),
            ["radiusMeters"] = Integer(100), ["requiredVisits"] = Integer(5), ["utcOffsetMinutes"] = Integer(180),
            ["startsAtUnixSeconds"] = Integer(1792962000), ["endsAtUnixSeconds"] = Integer(1793566800)
        };
        var fields = new Dictionary<string, object>();
        Check(GeoQuestFirestoreReader.Read(fields, "quest-1", "School") == null);
        fields["geo"] = new Dictionary<string, object> { ["nullValue"] = null };
        Check(GeoQuestFirestoreReader.Read(fields, "quest-1", "School") == null);
        fields["geo"] = new Dictionary<string, object>
        { ["mapValue"] = new Dictionary<string, object> { ["fields"] = geo } };
        GeoQuestDefinition Read() => GeoQuestFirestoreReader.Read(fields, "quest-1", "School");
        var definition = Read();
        Check(definition.Id == "quest-1" && definition.Title == "School");
        Check(definition.Latitude == 55.75 && definition.Longitude == 37.62);
        Check(definition.RadiusMeters == 100 && definition.RequiredVisits == 5);
        Check(definition.UtcOffsetMinutes == 180 && definition.StartsAtUnixSeconds == 1792962000);
        var clone = definition.Clone();
        clone.RequiredVisits = 1;
        Check(definition.RequiredVisits == 5);
        var previousCulture = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU"); Check(Read().Longitude == 37.62); }
        finally { CultureInfo.CurrentCulture = previousCulture; }

        void Invalid(string name, object value)
        {
            object original = geo[name];
            geo[name] = value;
            try { Read(); throw new Exception("Invalid geo field accepted: " + name); }
            catch (FormatException) { checks++; }
            finally { geo[name] = original; }
        }
        Invalid("version", Integer(2));
        Invalid("latitude", Number(91));
        Invalid("longitude", Number(double.NaN));
        Invalid("radiusMeters", Number(double.PositiveInfinity));
        Invalid("radiusMeters", Integer(0));
        Invalid("requiredVisits", Number(2.5));
        Invalid("requiredVisits", Integer(-1));
        Invalid("utcOffsetMinutes", Integer(841));
        Invalid("endsAtUnixSeconds", Integer(1792962000));
        Invalid("startsAtUnixSeconds", Number(1792962000.5));
        Invalid("endsAtUnixSeconds", Integer(long.MaxValue));
        Invalid("latitude", new Dictionary<string, object> { ["doubleValue"] = true });
        Invalid("longitude", new Dictionary<string, object> { ["stringValue"] = "37.62" });
        foreach (string field in new List<string>(geo.Keys))
        {
            object original = geo[field];
            geo.Remove(field);
            try { Read(); throw new Exception("Missing field accepted: " + field); }
            catch (FormatException) { checks++; }
            finally { geo[field] = original; }
        }
        fields["geo"] = new Dictionary<string, object> { ["stringValue"] = "broken" };
        try { Read(); throw new Exception("Invalid map accepted."); }
        catch (FormatException) { checks++; }
        return checks;
    }
}
