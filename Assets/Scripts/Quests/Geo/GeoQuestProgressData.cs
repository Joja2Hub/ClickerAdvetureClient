using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ClickerAdventure
{
    [Serializable]
    public sealed class GeoQuestProgressData
    {
        public int Version = 1;
        public string DefinitionKey;
        public List<long> VisitUnixSeconds = new List<long>();

        internal static string DefinitionHash(GeoQuestDefinition definition)
        {
            return Hash(writer =>
            {
                writer.Write(definition.Id ?? string.Empty);
                writer.Write(definition.Latitude);
                writer.Write(definition.Longitude);
                writer.Write(definition.RadiusMeters);
                writer.Write(definition.RequiredVisits);
                writer.Write(definition.StartsAtUnixSeconds);
                writer.Write(definition.EndsAtUnixSeconds);
                writer.Write(definition.UtcOffsetMinutes);
            });
        }

        internal static string Hash(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    write(writer);
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty);
            }
        }
    }
}
