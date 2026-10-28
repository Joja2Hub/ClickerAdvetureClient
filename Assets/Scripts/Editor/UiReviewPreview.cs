using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClickerAdventure.EditorTools
{
    /// <summary>Renders real UI components with local sample data, without login or network calls.</summary>
    public static class UiReviewPreview
    {
        [MenuItem("RPG Quest/UI/Export Review Previews")]
        public static void Export()
        {
            string directory = Path.GetFullPath("Library/UiReviewScreens");
            Directory.CreateDirectory(directory);
            Render(directory, "login", () =>
            {
                var screen = new GameObject("LoginPreview").AddComponent<LoginScreen>();
                InvokeBuild(screen);
            });
            Render(directory, "guild", () =>
            {
                var screen = new GameObject("GuildPreview").AddComponent<TownScreen>();
                InvokeBuild(screen);
                var hud = typeof(TownScreen).GetField("resourceHud",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(screen);
                typeof(ResourceHud).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hud, new object[] { new GameState { Level = 3, Xp = 85, Gold = 120 } });
                var list = (RectTransform)typeof(TownScreen).GetField("questList",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(screen);
                var prefab = Resources.Load<QuestCardView>("Prefabs/UI/QuestCard");
                var samples = new[]
                {
                    new RealLifeQuest { Id = "preview-study", Title = "Время для новых открытий",
                        Description = "Прочитай главу книги и расскажи, что запомнилось.",
                        Status = QuestStatus.Assigned, RewardGold = 20, RewardXp = 35, Recommended = true },
                    new RealLifeQuest { Id = "preview-help", Title = "Помощь по дому",
                        Description = "Наведи порядок на рабочем столе.",
                        Status = QuestStatus.Approved, RewardGold = 15, RewardXp = 25 }
                };
                list.sizeDelta = new Vector2(0, 740);
                for (int i = 0; i < samples.Length; i++)
                {
                    var view = UnityEngine.Object.Instantiate(prefab, list);
                    RuntimeUi.SetRect(view.RectTransform, new Vector2(0.5f, 1), new Vector2(820, 310),
                        new Vector2(0, -183 - i * 328));
                    view.Bind(samples[i], null, ArtLibrary.Quest("quest_icon_reading"),
                        ArtLibrary.Reward("reward_gold"), ArtLibrary.Reward("reward_xp"),
                        i == 0 ? "Активный" : "Одобрен", UiTheme.Primary,
                        i == 0 ? "Выполнить" : "Забрать награду", null, null);
                }
            });
            Debug.Log("UI previews exported: " + directory);
        }

        private static void InvokeBuild(MonoBehaviour screen)
        {
            screen.GetType().GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(screen, null);
        }

        private static void Render(string directory, string name, Action build)
        {
            Scene previous = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(previous.path))
            {
                if (!Application.isBatchMode)
                    throw new InvalidOperationException("Откройте сохранённую сцену перед экспортом UI.");
                EditorSceneManager.SaveScene(previous, "Assets/UiReviewBootstrap.unity");
            }
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            RenderTexture target = null;
            Texture2D image = null;
            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                SceneManager.SetActiveScene(preview);
                build();
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -100);
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = UiTheme.Surface;
                target = new RenderTexture(1080, 1920, 24);
                target.Create();
                camera.targetTexture = target;
                foreach (var root in preview.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10;
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler != null) scaler.enabled = false;
                    canvas.scaleFactor = 1;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var root in preview.GetRootGameObjects())
                foreach (var fill in root.GetComponentsInChildren<AspectFillImage>())
                    fill.SendMessage("Apply", SendMessageOptions.DontRequireReceiver);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(preview, true);
            }
        }
    }
}
