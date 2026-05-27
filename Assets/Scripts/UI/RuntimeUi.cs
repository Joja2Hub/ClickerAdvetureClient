using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public static class RuntimeUi
    {
        public static readonly Color Ink = new(0.14f, 0.11f, 0.09f);
        public static readonly Color Parchment = new(0.86f, 0.76f, 0.56f);
        public static readonly Color ParchmentDark = new(0.64f, 0.47f, 0.29f);
        public static readonly Color Night = new(0.06f, 0.10f, 0.14f);
        public static readonly Color Forest = new(0.12f, 0.38f, 0.29f);
        public static readonly Color Gold = new(1f, 0.78f, 0.28f);
        public static readonly Color Ruby = new(0.74f, 0.18f, 0.17f);
        public static readonly Color Sky = new(0.25f, 0.48f, 0.66f);
        public static readonly Color Muted = new(0.45f, 0.50f, 0.52f);

        private static readonly Dictionary<Color, Sprite> CircleSprites = new();

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            EnsureEventSystem();
            DesktopTestMode.Configure();

            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static void AdventureBackground(Transform parent, Color top, Color middle, Color bottom)
        {
            var root = Panel(parent, "Background", bottom);
            AddBand(root, "Sky", top, new Vector2(0.5f, 0.77f), new Vector2(1200, 900));
            AddBand(root, "Horizon", middle, new Vector2(0.5f, 0.47f), new Vector2(1200, 520));
            AddBand(root, "Ground", bottom, new Vector2(0.5f, 0.16f), new Vector2(1200, 620));
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        public static RectTransform Card(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0, -8);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.16f);
            outline.effectDistance = new Vector2(2, 2);
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Badge(Transform parent, string name, string label, Color color, Color textColor)
        {
            var badge = Card(parent, name, color);
            var text = Text(badge, "Label", label, 24, textColor);
            Stretch(text.rectTransform);
            return badge;
        }

        public static Text Text(Transform parent, string name, string value, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Color color, Action onClick)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0, -5);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(2, 2);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, Color.white, 0.12f),
                pressedColor = Color.Lerp(color, Color.black, 0.18f),
                selectedColor = color,
                disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.onClick.AddListener(() => onClick?.Invoke());

            var labelText = Text(go.transform, "Label", label, 30, Color.white);
            labelText.fontStyle = FontStyle.Bold;
            Stretch(labelText.rectTransform);
            return button;
        }

        public static Image ProgressBar(Transform parent, string name, Color backColor, Color fillColor)
        {
            var back = Card(parent, $"{name}_Back", backColor);
            var fillGo = new GameObject($"{name}_Fill", typeof(Image));
            fillGo.transform.SetParent(back, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(6, 6);
            fillRect.offsetMax = new Vector2(-6, -6);
            var fill = fillGo.GetComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            return fill;
        }

        public static Image Circle(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = GetCircleSprite(color);
            image.color = Color.white;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.33f);
            shadow.effectDistance = new Vector2(0, -8);
            return image;
        }

        public static RectTransform PathSegment(Transform parent, string name, Vector2 position, float length, float angle)
        {
            var segment = Card(parent, name, new Color(0.47f, 0.35f, 0.20f, 0.9f));
            SetRect(segment, new Vector2(0.5f, 0.5f), new Vector2(length, 18), position);
            segment.localEulerAngles = new Vector3(0, 0, angle);
            return segment;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void AddBand(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 size)
        {
            var band = new GameObject(name, typeof(Image));
            band.transform.SetParent(parent, false);
            band.GetComponent<Image>().color = color;
            SetRect(band.GetComponent<RectTransform>(), anchor, size, Vector2.zero);
        }

        private static Sprite GetCircleSprite(Color color)
        {
            if (CircleSprites.TryGetValue(color, out var sprite))
            {
                return sprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "RuntimeCircle";
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - size * 0.5f;
                    var dy = y - size * 0.5f;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    var alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(0.82f, 1f, distance));
                    var shade = Mathf.Lerp(1.15f, 0.78f, Mathf.Clamp01(distance));
                    texture.SetPixel(x, y, new Color(color.r * shade, color.g * shade, color.b * shade, color.a * alpha));
                }
            }

            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
            CircleSprites[color] = sprite;
            return sprite;
        }
    }
}
