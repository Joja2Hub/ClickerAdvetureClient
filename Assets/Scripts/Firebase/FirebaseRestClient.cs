using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ClickerAdventure
{
    public sealed class FirebaseRestClient
    {
        private readonly FirebaseWebConfig config;

        public string IdToken { get; private set; }
        public string Uid { get; private set; }
        public bool IsConfigured => config != null && config.IsValid;
        public bool IsSignedIn => IsConfigured && !string.IsNullOrWhiteSpace(IdToken);

        public FirebaseRestClient(FirebaseWebConfig config)
        {
            this.config = config;
        }

        public async Task<UserProfile> SignInChildAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={Uri.EscapeDataString(config.ApiKey)}";
            var body = new Dictionary<string, object>
            {
                ["email"] = email,
                ["password"] = password,
                ["returnSecureToken"] = true
            };
            var result = await SendJsonAsync("POST", url, body, false);
            IdToken = GetString(result, "idToken");
            Uid = GetString(result, "localId");

            var profile = await LoadUserProfileAsync(Uid);
            if (profile.Role != UserRole.Child || string.IsNullOrWhiteSpace(profile.ChildId))
            {
                SignOut();
                throw new InvalidOperationException("Этот аккаунт не является детским профилем.");
            }

            return profile;
        }

        public void SignOut()
        {
            IdToken = string.Empty;
            Uid = string.Empty;
        }

        public async Task<UserProfile> LoadUserProfileAsync(string uid)
        {
            var doc = await GetDocumentAsync($"users/{uid}");
            var fields = GetFields(doc);
            return new UserProfile
            {
                Uid = FireString(fields, "uid", uid),
                Role = ParseUserRole(FireString(fields, "role", "child")),
                FamilyId = FireString(fields, "familyId"),
                ChildId = FireString(fields, "childId"),
                DisplayName = FireString(fields, "displayName"),
                Email = FireString(fields, "email")
            };
        }

        public async Task<ChildProfile> LoadChildProfileAsync(string childId)
        {
            var doc = await GetDocumentAsync($"children/{childId}");
            var fields = GetFields(doc);
            return new ChildProfile
            {
                Id = childId,
                FamilyId = FireString(fields, "familyId"),
                DisplayName = FireString(fields, "displayName", "Герой"),
                Level = FireInt(fields, "level", 1),
                Xp = FireInt(fields, "xp"),
                Gold = FireInt(fields, "gold")
            };
        }

        public async Task<GameState> LoadGameStateAsync(string childId)
        {
            var doc = await GetDocumentAsync($"children/{childId}/gameState/main");
            var fields = GetFields(doc);
            return new GameState
            {
                Level = FireInt(fields, "level", 1),
                Xp = FireInt(fields, "xp"),
                Gold = FireInt(fields, "gold"),
                MaxHp = FireInt(fields, "maxHp", 100),
                Damage = FireInt(fields, "damage", 10),
                CritChance = FireFloat(fields, "critChance", 0.1f),
                RageGain = FireFloat(fields, "rageGain", 12f),
                UnlockedLocations = FireStringArray(fields, "unlockedLocations"),
                CompletedDungeons = FireStringArray(fields, "completedDungeons"),
                CurrentMapNodeId = FireString(fields, "currentMapNodeId", "sunny_forest"),
                DailyStreak = FireInt(fields, "dailyStreak")
            };
        }

        public async Task<List<RealLifeQuest>> LoadQuestsAsync(string childId)
        {
            var url = FirestoreUrl($"children/{childId}/quests?pageSize=100");
            var response = await SendJsonAsync("GET", url, null, true);
            if (!response.TryGetValue("documents", out var documentsObj) || documentsObj is not List<object> documents)
            {
                return new List<RealLifeQuest>();
            }

            return documents
                .OfType<Dictionary<string, object>>()
                .Select(ToQuest)
                .OrderBy(q => q.Status)
                .ThenBy(q => q.Title)
                .ToList();
        }

        public async Task SubmitQuestAsync(string childId, string questId, string childComment)
        {
            var url = FirestoreUrl($"children/{childId}/quests/{questId}?updateMask.fieldPaths=status&updateMask.fieldPaths=childComment&updateMask.fieldPaths=submittedAt");
            var body = new Dictionary<string, object>
            {
                ["fields"] = new Dictionary<string, object>
                {
                    ["status"] = StringField("submitted"),
                    ["childComment"] = StringField(childComment ?? string.Empty),
                    ["submittedAt"] = TimestampField(DateTime.UtcNow)
                }
            };
            await SendJsonAsync("PATCH", url, body, true);
        }

        public async Task ClaimQuestRewardAsync(string childId, string questId)
        {
            var url = $"https://{config.FunctionsRegion}-{config.ProjectId}.cloudfunctions.net/claimQuestReward";
            var body = new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object>
                {
                    ["childId"] = childId,
                    ["questId"] = questId
                }
            };
            await SendJsonAsync("POST", url, body, true);
        }

        public async Task<bool> TrySaveGameStateAsync(string childId, GameState state)
        {
            try
            {
                var url = FirestoreUrl($"children/{childId}/gameState/main?updateMask.fieldPaths=level&updateMask.fieldPaths=xp&updateMask.fieldPaths=gold&updateMask.fieldPaths=maxHp&updateMask.fieldPaths=damage&updateMask.fieldPaths=critChance&updateMask.fieldPaths=rageGain&updateMask.fieldPaths=unlockedLocations&updateMask.fieldPaths=completedDungeons&updateMask.fieldPaths=currentMapNodeId&updateMask.fieldPaths=dailyStreak&updateMask.fieldPaths=lastActiveAt");
                var body = new Dictionary<string, object>
                {
                    ["fields"] = new Dictionary<string, object>
                    {
                        ["level"] = IntegerField(state.Level),
                        ["xp"] = IntegerField(state.Xp),
                        ["gold"] = IntegerField(state.Gold),
                        ["maxHp"] = IntegerField(state.MaxHp),
                        ["damage"] = IntegerField(state.Damage),
                        ["critChance"] = DoubleField(state.CritChance),
                        ["rageGain"] = DoubleField(state.RageGain),
                        ["unlockedLocations"] = StringArrayField(state.UnlockedLocations),
                        ["completedDungeons"] = StringArrayField(state.CompletedDungeons),
                        ["currentMapNodeId"] = StringField(state.CurrentMapNodeId ?? string.Empty),
                        ["dailyStreak"] = IntegerField(state.DailyStreak),
                        ["lastActiveAt"] = TimestampField(DateTime.UtcNow)
                    }
                };
                await SendJsonAsync("PATCH", url, body, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameState sync skipped: {ex.Message}");
                return false;
            }
        }

        private async Task<Dictionary<string, object>> GetDocumentAsync(string path)
        {
            return await SendJsonAsync("GET", FirestoreUrl(path), null, true);
        }

        private string FirestoreUrl(string path)
        {
            return $"https://firestore.googleapis.com/v1/projects/{config.ProjectId}/databases/(default)/documents/{path}";
        }

        private async Task<Dictionary<string, object>> SendJsonAsync(string method, string url, Dictionary<string, object> body, bool authorize)
        {
            var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (body != null)
            {
                var payload = Encoding.UTF8.GetBytes(FirebaseJson.Serialize(body));
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (authorize)
            {
                request.SetRequestHeader("Authorization", $"Bearer {IdToken}");
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            var text = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"{request.responseCode}: {text}");
            }

            return FirebaseJson.ParseObject(text);
        }

        private static RealLifeQuest ToQuest(Dictionary<string, object> document)
        {
            var fields = GetFields(document);
            return new RealLifeQuest
            {
                Id = ExtractDocumentId(GetString(document, "name")),
                Title = FireString(fields, "title", "Квест"),
                Description = FireString(fields, "description"),
                Category = ParseQuestCategory(FireString(fields, "category", "custom")),
                Status = ParseQuestStatus(FireString(fields, "status", "assigned")),
                RewardXp = FireInt(fields, "rewardXp"),
                RewardGold = FireInt(fields, "rewardGold"),
                ChildComment = FireString(fields, "childComment"),
                ParentComment = FireString(fields, "parentComment"),
                ProofImageUrl = FireString(fields, "proofImageUrl")
            };
        }

        private static Dictionary<string, object> GetFields(Dictionary<string, object> document)
        {
            return document.TryGetValue("fields", out var fields) && fields is Dictionary<string, object> dict
                ? dict
                : new Dictionary<string, object>();
        }

        private static string ExtractDocumentId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var split = name.LastIndexOf('/');
            return split >= 0 ? name[(split + 1)..] : name;
        }

        private static Dictionary<string, object> StringField(string value)
        {
            return new Dictionary<string, object> { ["stringValue"] = value ?? string.Empty };
        }

        private static Dictionary<string, object> IntegerField(int value)
        {
            return new Dictionary<string, object> { ["integerValue"] = value.ToString(CultureInfo.InvariantCulture) };
        }

        private static Dictionary<string, object> DoubleField(float value)
        {
            return new Dictionary<string, object> { ["doubleValue"] = value };
        }

        private static Dictionary<string, object> TimestampField(DateTime value)
        {
            return new Dictionary<string, object> { ["timestampValue"] = value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) };
        }

        private static Dictionary<string, object> StringArrayField(IEnumerable<string> values)
        {
            return new Dictionary<string, object>
            {
                ["arrayValue"] = new Dictionary<string, object>
                {
                    ["values"] = values.Select(StringField).Cast<object>().ToList()
                }
            };
        }

        private static string FireString(Dictionary<string, object> fields, string key, string fallback = "")
        {
            if (!fields.TryGetValue(key, out var value) || value is not Dictionary<string, object> field)
            {
                return fallback;
            }

            return GetString(field, "stringValue", fallback);
        }

        private static int FireInt(Dictionary<string, object> fields, string key, int fallback = 0)
        {
            if (!fields.TryGetValue(key, out var value) || value is not Dictionary<string, object> field)
            {
                return fallback;
            }

            var raw = GetString(field, "integerValue");
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (field.TryGetValue("doubleValue", out var doubleValue))
            {
                return Mathf.RoundToInt(Convert.ToSingle(doubleValue, CultureInfo.InvariantCulture));
            }

            return fallback;
        }

        private static float FireFloat(Dictionary<string, object> fields, string key, float fallback = 0f)
        {
            if (!fields.TryGetValue(key, out var value) || value is not Dictionary<string, object> field)
            {
                return fallback;
            }

            if (field.TryGetValue("doubleValue", out var doubleValue))
            {
                return Convert.ToSingle(doubleValue, CultureInfo.InvariantCulture);
            }

            var raw = GetString(field, "integerValue");
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        private static List<string> FireStringArray(Dictionary<string, object> fields, string key)
        {
            if (!fields.TryGetValue(key, out var value) || value is not Dictionary<string, object> field)
            {
                return new List<string>();
            }

            if (!field.TryGetValue("arrayValue", out var arrayObj) || arrayObj is not Dictionary<string, object> array)
            {
                return new List<string>();
            }

            if (!array.TryGetValue("values", out var valuesObj) || valuesObj is not List<object> values)
            {
                return new List<string>();
            }

            return values
                .OfType<Dictionary<string, object>>()
                .Select(v => GetString(v, "stringValue"))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }

        private static string GetString(Dictionary<string, object> dict, string key, string fallback = "")
        {
            return dict.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : fallback;
        }

        private static UserRole ParseUserRole(string value)
        {
            return string.Equals(value, "parent", StringComparison.OrdinalIgnoreCase) ? UserRole.Parent : UserRole.Child;
        }

        private static QuestStatus ParseQuestStatus(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "submitted" => QuestStatus.Submitted,
                "approved" => QuestStatus.Approved,
                "rejected" => QuestStatus.Rejected,
                "claimed" => QuestStatus.Claimed,
                "cancelled" => QuestStatus.Cancelled,
                _ => QuestStatus.Assigned
            };
        }

        private static QuestCategory ParseQuestCategory(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "study" => QuestCategory.Study,
                "home" => QuestCategory.Home,
                "reading" => QuestCategory.Reading,
                "health" => QuestCategory.Health,
                "sport" => QuestCategory.Sport,
                "hygiene" => QuestCategory.Hygiene,
                "familyhelp" or "family_help" => QuestCategory.FamilyHelp,
                _ => QuestCategory.Custom
            };
        }
    }
}
