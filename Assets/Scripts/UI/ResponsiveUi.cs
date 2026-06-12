using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            rectTransform ??= GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            var anchorMin = safe.position;
            var anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }

    public sealed class AspectFillImage : MonoBehaviour
    {
        private Image image;
        private RectTransform rectTransform;
        private RectTransform parentRect;
        private Sprite lastSprite;
        private Vector2 lastParentSize;

        private void Awake()
        {
            image = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;
            Apply();
        }

        private void Update()
        {
            var parentSize = parentRect != null ? parentRect.rect.size : Vector2.zero;
            if (image != null && (image.sprite != lastSprite || parentSize != lastParentSize))
            {
                Apply();
            }
        }

        private void Apply()
        {
            image ??= GetComponent<Image>();
            rectTransform ??= GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;
            if (image == null || image.sprite == null || parentRect == null)
            {
                return;
            }

            lastSprite = image.sprite;
            lastParentSize = parentRect.rect.size;

            var spriteSize = image.sprite.rect.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f || lastParentSize.x <= 0f || lastParentSize.y <= 0f)
            {
                return;
            }

            var spriteAspect = spriteSize.x / spriteSize.y;
            var parentAspect = lastParentSize.x / lastParentSize.y;
            var size = lastParentSize;
            if (parentAspect > spriteAspect)
            {
                size.y = lastParentSize.x / spriteAspect;
            }
            else
            {
                size.x = lastParentSize.y * spriteAspect;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
