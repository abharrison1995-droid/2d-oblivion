using System;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Runtime UI factory — no prefab wiring required on first Unity open.
    /// </summary>
    public static class UiFactory
    {
        static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvas;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            return rt;
        }

        public static UnityEngine.UI.Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(6, 4);
            rt.offsetMax = new Vector2(-6, -4);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.36f, 0.42f, 1f);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.2f, 1f);
            btn.colors = colors;
            Label(go.transform, "Label", caption, 28, TextAnchor.MiddleCenter, new Color(0.92f, 0.9f, 0.84f));
            if (onClick != null)
                btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>
        /// Character image loaded from Resources/Portraits/{portraitId}. Falls back to a
        /// deterministic colored square with initials when no art exists yet for that id.
        /// </summary>
        public static UnityEngine.UI.Image Portrait(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string portraitId)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<UnityEngine.UI.Image>();

            var sprite = PortraitLoader.Load(portraitId);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = PortraitLoader.PlaceholderColor(portraitId);
                var label = Label(go.transform, "Initials", PortraitLoader.Initials(portraitId), 36, TextAnchor.MiddleCenter, Color.white);
                var lrt = label.GetComponent<RectTransform>();
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            return img;
        }

        public static UnityEngine.UI.InputField Input(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 4);
            rt.offsetMax = new Vector2(-12, -4);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.11f, 0.13f, 0.95f);
            var input = go.AddComponent<UnityEngine.UI.InputField>();
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14, 8);
            textRt.offsetMax = new Vector2(-14, -8);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.font = DefaultFont;
            text.fontSize = 30;
            text.color = Color.white;
            text.supportRichText = false;
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(14, 8);
            phRt.offsetMax = new Vector2(-14, -8);
            var ph = phGo.AddComponent<UnityEngine.UI.Text>();
            ph.font = DefaultFont;
            ph.fontSize = 30;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1, 1, 1, 0.35f);
            ph.text = placeholder;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            return input;
        }
    }
}
