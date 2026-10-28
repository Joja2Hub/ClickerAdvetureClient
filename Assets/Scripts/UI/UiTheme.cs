using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    /// <summary>Shared UI chrome. Illustrated world art remains in ArtLibrary.</summary>
    public static class UiTheme
    {
        public static readonly Color Paper = new(0.98f, 0.96f, 0.90f);
        public static readonly Color Ink = new(0.16f, 0.22f, 0.23f);
        public static readonly Color SecondaryInk = new(0.39f, 0.45f, 0.44f);
        public static readonly Color Surface = new(0.10f, 0.18f, 0.20f);
        public static readonly Color Primary = new(0.15f, 0.39f, 0.32f);
        public static readonly Color Secondary = new(0.24f, 0.33f, 0.36f);
        private static Sprite rounded;

        public static void ApplySurface(Image image, Color color)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = 1;
            var outline = image.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            var shadow = image.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0.02f, 0.06f, 0.07f, 0.16f);
                shadow.effectDistance = new Vector2(0, -3);
            }
        }

        public static void ApplyButton(Button button, Color color)
        {
            ApplySurface((Image)button.targetGraphic, color);
            button.transition = Selectable.Transition.ColorTint;
            // ColorTint multiplies Image.color; these must be neutral multipliers.
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.12f, 1.12f, 1.12f),
                selectedColor = Color.white,
                pressedColor = new Color(0.82f, 0.82f, 0.82f),
                disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.45f),
                colorMultiplier = 1,
                fadeDuration = 0.12f
            };
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64;
                const float radius = 16;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "UI_RoundedSurface",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius), 0);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius), 0);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
                rounded.name = "UI_RoundedSurface";
                rounded.hideFlags = HideFlags.HideAndDontSave;
                return rounded;
            }
        }
    }
}
