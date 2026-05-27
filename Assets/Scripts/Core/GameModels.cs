using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClickerAdventure
{
    public enum UserRole { Parent, Child }
    public enum QuestStatus { Assigned, Submitted, Approved, Rejected, Claimed, Cancelled }
    public enum QuestCategory { Study, Home, Reading, Health, Sport, Hygiene, FamilyHelp, Custom }
    public enum ItemType { Weapon, Armor, Accessory, Consumable, Trophy, Material, Quest }
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum MapNodeType { Dungeon, Village, Town, City, Castle }
    public enum SkillType { PowerAttack, Heal, RageStrike }
    public enum EquipmentSlot { Weapon, Helmet, Chest, Boots, Ring, Amulet }

    [Serializable]
    public class UserProfile
    {
        public string Uid;
        public UserRole Role;
        public string FamilyId;
        public string ChildId;
        public string DisplayName;
        public string Email;
    }

    [Serializable]
    public class ChildProfile
    {
        public string Id;
        public string FamilyId;
        public string DisplayName;
        public int Level;
        public int Xp;
        public int Gold;
    }

    [Serializable]
    public class GameState
    {
        public int Level = 1;
        public int Xp;
        public int Gold;
        public int MaxHp = 100;
        public int Damage = 10;
        public float CritChance = 0.1f;
        public float RageGain = 12f;
        public List<string> UnlockedLocations = new();
        public List<string> CompletedDungeons = new();
        public string CurrentMapNodeId = "sunny_forest";
        public int DailyStreak;

        public GameState Clone()
        {
            return new GameState
            {
                Level = Level,
                Xp = Xp,
                Gold = Gold,
                MaxHp = MaxHp,
                Damage = Damage,
                CritChance = CritChance,
                RageGain = RageGain,
                UnlockedLocations = new List<string>(UnlockedLocations),
                CompletedDungeons = new List<string>(CompletedDungeons),
                CurrentMapNodeId = CurrentMapNodeId,
                DailyStreak = DailyStreak
            };
        }
    }

    [Serializable]
    public class RealLifeQuest
    {
        public string Id;
        public string Title;
        public string Description;
        public QuestCategory Category;
        public QuestStatus Status;
        public int RewardXp;
        public int RewardGold;
        public string ChildComment;
        public string ParentComment;
        public string ProofImageUrl;
    }

    [Serializable]
    public class InventoryItemInstance
    {
        public string InstanceId;
        public string ItemId;
        public ItemType ItemType;
        public Rarity Rarity;
        public int Quantity = 1;
        public bool Equipped;
        public EquipmentSlot Slot;
    }

    [Serializable]
    public class BattleReward
    {
        public int Xp;
        public int Gold;
        public List<string> ItemIds = new();

        public static BattleReward Empty => new();
    }

    public readonly struct MapNodeState
    {
        public readonly MapNodeConfig Config;
        public readonly bool IsUnlocked;

        public MapNodeState(MapNodeConfig config, bool isUnlocked)
        {
            Config = config;
            IsUnlocked = isUnlocked;
        }
    }

    public sealed class BattleSession
    {
        public DungeonConfig Dungeon { get; }
        public int CurrentWave { get; private set; } = 1;
        public int EnemiesDefeated { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public BattleReward Reward { get; } = new();

        public BattleSession(DungeonConfig dungeon)
        {
            Dungeon = dungeon;
            Reward.Xp = dungeon != null ? dungeon.BaseXpReward : 0;
            Reward.Gold = dungeon != null ? dungeon.BaseGoldReward : 0;
        }

        public void RegisterHit(bool critical)
        {
            Combo += critical ? 2 : 1;
            BestCombo = Mathf.Max(BestCombo, Combo);
        }

        public void ResetCombo()
        {
            Combo = 0;
        }

        public void RegisterEnemyDefeated(EnemyConfig enemy)
        {
            EnemiesDefeated++;
            if (enemy == null)
            {
                return;
            }

            Reward.Xp += enemy.XpReward;
            Reward.Gold += enemy.GoldReward;
        }

        public bool AdvanceWave()
        {
            if (Dungeon == null || CurrentWave >= Dungeon.WavesCount)
            {
                return false;
            }

            CurrentWave++;
            return true;
        }

        public void AddPerformanceBonus()
        {
            Reward.Xp += Mathf.RoundToInt(BestCombo * 0.5f);
            Reward.Gold += Mathf.RoundToInt(BestCombo * 0.35f);
        }
    }
}
