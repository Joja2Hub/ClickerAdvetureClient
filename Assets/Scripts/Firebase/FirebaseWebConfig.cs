using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ClickerAdventure
{
    public sealed class FirebaseWebConfig
    {
        public string ApiKey;
        public string AuthDomain;
        public string ProjectId;
        public string StorageBucket;
        public string MessagingSenderId;
        public string AppId;
        public string FunctionsRegion = "europe-west1";

        public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);

        public static FirebaseWebConfig LoadForLocalTesting()
        {
            var candidates = new[]
            {
                Path.Combine(Application.streamingAssetsPath, "firebase-web-config.env"),
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "rpg_quest_parent_web_starter", ".env.local")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "rpg_quest_parent_web_starter", ".env.example")),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "rpg_quest_parent_web_starter", ".env.local"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "rpg_quest_parent_web_starter", ".env.example")
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var config = FromEnvFile(path);
                if (config.IsValid)
                {
                    Debug.Log($"Firebase web config loaded for project '{config.ProjectId}'.");
                    return config;
                }
            }

            Debug.LogWarning("Firebase web config was not found. Unity client will use dev/offline data.");
            return new FirebaseWebConfig();
        }

        private static FirebaseWebConfig FromEnvFile(string path)
        {
            var values = new Dictionary<string, string>();
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var split = line.IndexOf('=');
                if (split <= 0)
                {
                    continue;
                }

                var key = line[..split].Trim();
                var value = line[(split + 1)..].Trim().Trim('"');
                values[key] = value;
            }

            return new FirebaseWebConfig
            {
                ApiKey = Get(values, "NEXT_PUBLIC_FIREBASE_API_KEY"),
                AuthDomain = Get(values, "NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN"),
                ProjectId = Get(values, "NEXT_PUBLIC_FIREBASE_PROJECT_ID"),
                StorageBucket = Get(values, "NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET"),
                MessagingSenderId = Get(values, "NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID"),
                AppId = Get(values, "NEXT_PUBLIC_FIREBASE_APP_ID"),
                FunctionsRegion = Get(values, "NEXT_PUBLIC_FIREBASE_FUNCTIONS_REGION", "europe-west1")
            };
        }

        private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback = "")
        {
            return values.TryGetValue(key, out var value) ? value : fallback;
        }
    }
}
