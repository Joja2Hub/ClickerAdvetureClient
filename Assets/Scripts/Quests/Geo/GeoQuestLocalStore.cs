using System;
using System.IO;
using UnityEngine;

namespace ClickerAdventure
{
    /// <summary>Local cache only; never an authoritative proof for server rewards.</summary>
    public sealed class GeoQuestLocalStore
    {
        private readonly string directory;

        public GeoQuestLocalStore(string directory = null)
        {
            this.directory = Path.GetFullPath(directory ?? Path.Combine(Application.persistentDataPath, "GeoQuests"));
        }

        public GeoQuestTracker Load(string childId, GeoQuestDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            string path = SavePath(childId, definition.Id);
            if (!File.Exists(path)) return new GeoQuestTracker(definition);
            try
            {
                var data = JsonUtility.FromJson<GeoQuestProgressData>(File.ReadAllText(path));
                if (data == null) throw new ArgumentException("Empty progress document.");
                return new GeoQuestTracker(definition, data);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Cannot restore geo quest progress. The save is invalid or quest conditions changed.", exception);
            }
        }

        public void Save(string childId, GeoQuestTracker tracker)
        {
            if (tracker == null) throw new ArgumentNullException(nameof(tracker));
            string path = SavePath(childId, tracker.QuestId);
            Directory.CreateDirectory(directory);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonUtility.ToJson(tracker.CreateSaveData()));
                // Keep the previous save intact if writing the new document fails.
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private string SavePath(string childId, string questId)
        {
            if (string.IsNullOrWhiteSpace(childId)) throw new ArgumentException("Child ID is required.", nameof(childId));
            if (string.IsNullOrWhiteSpace(questId)) throw new ArgumentException("Quest ID is required.", nameof(questId));
            string key = GeoQuestProgressData.Hash(writer => { writer.Write(childId); writer.Write(questId); });
            return Path.Combine(directory, key + ".json");
        }
    }
}
