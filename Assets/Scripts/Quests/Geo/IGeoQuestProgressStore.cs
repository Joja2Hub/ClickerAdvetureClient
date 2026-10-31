namespace ClickerAdventure
{
    public interface IGeoQuestProgressStore
    {
        GeoQuestTracker Load(string childId, GeoQuestDefinition definition);
        void Save(string childId, GeoQuestTracker tracker);
    }
}
