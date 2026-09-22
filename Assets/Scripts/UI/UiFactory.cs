using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Monopoly.UI
{
    /// <summary>Сборка примитивов uGUI: панели, надписи, кнопки. Общая для HUD и доски.</summary>
    public static class UiFactory
    {
        public static Image Panel(Transform parent, string objectName, Color color, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(Transform parent, string value, int size, Color color,
            Vector2 min, Vector2 max, FontStyle style = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bestFit = true)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = obj.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.resizeTextForBestFit = bestFit;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = size;
            return text;
        }

        public static Button Button(Transform parent, string label, Color color,
            Vector2 min, Vector2 max, UnityAction action, int fontSize = 18)
        {
            var image = Panel(parent, label, color, min, max);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Label(image.transform, label, fontSize, Color.white,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), FontStyle.Bold);
            return button;
        }

        public static string ButtonCaption(Button button)
        {
            var text = button.GetComponentInChildren<Text>();
            return text != null ? text.text : string.Empty;
        }

        public static void SetButtonCaption(Button button, string caption)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = caption;
        }
    }
}
