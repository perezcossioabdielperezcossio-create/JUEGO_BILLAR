using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace QuestBilliards.UI
{
    public static class WorldSpaceUIHelper
    {
        public static Canvas CreateMenuCanvas(string name, Transform head, UITheme theme)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0f, 0f, theme.menuDistance);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * theme.canvasScale;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = theme.menuSize;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 4f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            if (theme.menuBackingPrefab != null)
            {
                var backing = Object.Instantiate(theme.menuBackingPrefab, canvas.transform);
                backing.transform.localPosition = new Vector3(0f, 0f, 8f);
                backing.transform.localRotation = Quaternion.identity;
                backing.transform.localScale = Vector3.one / theme.canvasScale;
                foreach (var renderer in backing.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            EnsureEventSystem();
            return canvas;
        }

        public static UnityEngine.UI.Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, Color color, TMP_FontAsset font = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (font != null) text.font = font;
            return text;
        }

        public static UnityEngine.UI.Button CreateButton(Transform parent, string name, string label, UITheme theme, UnityAction onClick)
        {
            var image = CreateImage(parent, name, Color.white);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = theme.button,
                highlightedColor = theme.buttonHover,
                pressedColor = theme.frame,
                selectedColor = theme.buttonSelected,
                disabledColor = theme.button,
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };
            if (onClick != null) button.onClick.AddListener(onClick);
            button.interactable = false;
            button.interactable = true;

            var text = CreateText(image.transform, "Label", label, theme.buttonSize, theme.text, theme.bodyFont);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 8f);
            rect.offsetMax = new Vector2(-20f, -8f);
            return button;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = go.GetComponent<EventSystem>();
            }
            if (eventSystem.GetComponent<BaseInputModule>() != null) return;
            var input = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            input.enableXRInput = true;
            input.enableMouseInput = true;
            input.enableBuiltinActionsAsFallback = true;
        }
    }
}
