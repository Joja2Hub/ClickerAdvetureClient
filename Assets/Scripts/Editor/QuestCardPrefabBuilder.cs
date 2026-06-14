using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure.EditorTools
{
    public static class QuestCardPrefabBuilder
    {
        public const string PrefabPath = "Assets/Resources/Prefabs/UI/QuestCard.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleCreate()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("RPG Quest/UI/Create Quest Card Prefab")]
        public static void CreateIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder("Assets/Resources/Prefabs/UI");

            var root = new GameObject(
                "QuestCard",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Shadow),
                typeof(Outline),
                typeof(QuestCardView));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(820, 210);

            var background = root.GetComponent<Image>();
            background.color = new Color(1f, 0.95f, 0.78f, 0.96f);
            background.raycastTarget = false;
            var rootShadow = root.GetComponent<Shadow>();
            rootShadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
            rootShadow.effectDistance = new Vector2(0, -7);
            var rootOutline = root.GetComponent<Outline>();
            rootOutline.effectColor = new Color(1f, 1f, 1f, 0.16f);
            rootOutline.effectDistance = new Vector2(2, 2);

            var categoryIcon = CreateImage(root.transform, "CategoryIcon", new Vector2(0.12f, 0.60f), new Vector2(106, 106));
            var title = CreateText(root.transform, "Title", "Название задания\nОписание задания", 24, RuntimeUi.Ink, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.42f, 0.62f), new Vector2(470, 118));

            var recommended = CreateBadge(root.transform, "Recommended", "Совет дня", new Color(0.86f, 0.58f, 0.12f));
            SetRect(recommended.Rect, new Vector2(0.50f, 0.88f), new Vector2(180, 42));

            var goldIcon = CreateImage(root.transform, "GoldIcon", new Vector2(0.71f, 0.75f), new Vector2(44, 44));
            var xpIcon = CreateImage(root.transform, "XpIcon", new Vector2(0.71f, 0.52f), new Vector2(44, 44));
            var reward = CreateText(root.transform, "Reward", "+10\n+25", 22, new Color(0.42f, 0.25f, 0.08f), TextAnchor.MiddleLeft);
            SetRect(reward.rectTransform, new Vector2(0.80f, 0.63f), new Vector2(120, 94));

            var status = CreateBadge(root.transform, "Status", "Активный", new Color(0.24f, 0.52f, 0.35f));
            SetRect(status.Rect, new Vector2(0.20f, 0.20f), new Vector2(230, 48));

            var details = CreateButton(root.transform, "DetailsButton", "Подробнее", new Color(0.46f, 0.32f, 0.20f));
            SetRect(details.Button.GetComponent<RectTransform>(), new Vector2(0.55f, 0.22f), new Vector2(190, 56));
            var action = CreateButton(root.transform, "ActionButton", "Выполнить", new Color(0.36f, 0.45f, 0.70f));
            SetRect(action.Button.GetComponent<RectTransform>(), new Vector2(0.82f, 0.22f), new Vector2(220, 56));

            var view = root.GetComponent<QuestCardView>();
            var serialized = new SerializedObject(view);
            SetReference(serialized, "background", background);
            SetReference(serialized, "categoryIcon", categoryIcon);
            SetReference(serialized, "goldIcon", goldIcon);
            SetReference(serialized, "xpIcon", xpIcon);
            SetReference(serialized, "titleText", title);
            SetReference(serialized, "rewardText", reward);
            SetReference(serialized, "statusText", status.Label);
            SetReference(serialized, "recommendedText", recommended.Label);
            SetReference(serialized, "detailsButtonText", details.Label);
            SetReference(serialized, "actionButtonText", action.Label);
            SetReference(serialized, "statusBackground", status.Background);
            SetReference(serialized, "recommendedBadge", recommended.Rect.gameObject);
            SetReference(serialized, "detailsButton", details.Button);
            SetReference(serialized, "actionButton", action.Button);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"Editable quest card prefab created: {PrefabPath}");
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            SetRect(image.rectTransform, anchor, size);
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Color color,
            TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static BadgeParts CreateBadge(Transform parent, string name, string label, Color color)
        {
            var root = CreatePanel(parent, name, color);
            var text = CreateText(root, "Label", label, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return new BadgeParts(root, root.GetComponent<Image>(), text);
        }

        private static ButtonParts CreateButton(Transform parent, string name, string label, Color color)
        {
            var root = CreatePanel(parent, name, color);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            var text = CreateText(root, "Label", label, 26, Color.white, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform);
            return new ButtonParts(button, text);
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Shadow),
                typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Shadow>().effectDistance = new Vector2(0, -4);
            go.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.16f);
            return go.GetComponent<RectTransform>();
        }

        private static void SetReference(SerializedObject serialized, string property, Object value)
        {
            serialized.FindProperty(property).objectReferenceValue = value;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        private readonly struct BadgeParts
        {
            public readonly RectTransform Rect;
            public readonly Image Background;
            public readonly Text Label;

            public BadgeParts(RectTransform rect, Image background, Text label)
            {
                Rect = rect;
                Background = background;
                Label = label;
            }
        }

        private readonly struct ButtonParts
        {
            public readonly Button Button;
            public readonly Text Label;

            public ButtonParts(Button button, Text label)
            {
                Button = button;
                Label = label;
            }
        }
    }
}
