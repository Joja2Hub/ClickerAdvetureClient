using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public sealed class MapPanZoom : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private float minScale = 0.78f;
        [SerializeField] private float maxScale = 1.65f;
        [SerializeField] private float wheelStep = 0.12f;
        [SerializeField] private float pinchSensitivity = 0.004f;

        private RectTransform content;
        private ScrollRect scrollRect;
        private RectTransform viewport;
        private float scale = 1f;
        private float previousPinchDistance;
        private bool isPinching;

        public void Initialize(RectTransform mapContent, ScrollRect mapScrollRect)
        {
            content = mapContent;
            scrollRect = mapScrollRect;
            viewport = mapScrollRect != null ? mapScrollRect.viewport : null;
            scale = content != null ? content.localScale.x : 1f;
            SetScale(scale);
        }

        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                EndPinch();
                return;
            }

            var first = touchscreen.touches[0];
            var second = touchscreen.touches[1];
            if (!first.press.isPressed || !second.press.isPressed)
            {
                EndPinch();
                return;
            }

            var distance = Vector2.Distance(first.position.ReadValue(), second.position.ReadValue());
            if (!isPinching)
            {
                isPinching = true;
                previousPinchDistance = distance;
                if (scrollRect != null)
                {
                    scrollRect.StopMovement();
                    scrollRect.enabled = false;
                }
                return;
            }

            var delta = distance - previousPinchDistance;
            previousPinchDistance = distance;
            SetScale(scale + delta * pinchSensitivity);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Mathf.Approximately(eventData.scrollDelta.y, 0f))
            {
                return;
            }

            Zoom(eventData.scrollDelta.y > 0f ? wheelStep : -wheelStep);
            eventData.Use();
        }

        public void ZoomIn()
        {
            Zoom(wheelStep);
        }

        public void ZoomOut()
        {
            Zoom(-wheelStep);
        }

        private void Zoom(float delta)
        {
            SetScale(scale + delta);
        }

        private void SetScale(float value)
        {
            if (content == null)
            {
                return;
            }

            scale = Mathf.Clamp(value, EffectiveMinScale(), maxScale);
            content.localScale = Vector3.one * scale;
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                Canvas.ForceUpdateCanvases();
            }
        }

        private float EffectiveMinScale()
        {
            if (content == null || viewport == null)
            {
                return minScale;
            }

            var contentSize = content.rect.size;
            var viewportSize = viewport.rect.size;
            if (contentSize.x <= 0f || contentSize.y <= 0f)
            {
                return minScale;
            }

            var coverScale = Mathf.Max(
                viewportSize.x / contentSize.x,
                viewportSize.y / contentSize.y);
            return Mathf.Clamp(Mathf.Max(minScale, coverScale), minScale, maxScale);
        }

        private void EndPinch()
        {
            if (!isPinching)
            {
                return;
            }

            isPinching = false;
            previousPinchDistance = 0f;
            if (scrollRect != null)
            {
                scrollRect.enabled = true;
            }
        }
    }
}
