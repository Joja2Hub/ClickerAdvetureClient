using System;
using System.Text.Json;

// Allows the real local store to run without Unity; only the JSON/path boundary is replaced.
namespace UnityEngine
{
    public static class Application
    {
        public static string persistentDataPath => System.IO.Path.Combine(
            Environment.CurrentDirectory, "Temp", "GeoQuestChecks", "persistent");
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value) => JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string json)
        {
            try { return JsonSerializer.Deserialize<T>(json, Options); }
            catch (JsonException exception) { throw new ArgumentException("Invalid JSON.", exception); }
        }
    }
}
