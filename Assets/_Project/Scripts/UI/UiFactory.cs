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
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        /// <summary>
        /// Semantic button intent, each with its own palette so a screen full of buttons reads
        /// as a hierarchy instead of one flat wall of identical boxes.
        /// Primary = the main forward action in a row (recruit/buy/accept/train/equip).
        /// Secondary = neutral/navigation (tabs, the old default look — unstyled callers get this).
        /// Danger = costs the player something or ends/gives something up (release, retreat).
        /// </summary>
        public enum ButtonStyle { Secondary, Primary, Danger }

        struct Palette
        {
            public Color bg, highlighted, pressed, outline, label;
        }

        static Palette StyleOf(ButtonStyle style) => style switch
        {
            ButtonStyle.Primary => new Palette
            {
                bg = new Color(0.5f, 0.38f, 0.14f, 0.97f),
                highlighted = new Color(0.62f, 0.48f, 0.2f, 1f),
                pressed = new Color(0.35f, 0.26f, 0.08f, 1f),
                outline = new Color(0.85f, 0.72f, 0.4f, 0.75f),
                label = new Color(0.98f, 0.94f, 0.84f)
            },
            ButtonStyle.Danger => new Palette
            {
                bg = new Color(0.42f, 0.16f, 0.16f, 0.97f),
                highlighted = new Color(0.52f, 0.22f, 0.22f, 1f),
                pressed = new Color(0.28f, 0.1f, 0.1f, 1f),
                outline = new Color(0.75f, 0.4f, 0.4f, 0.7f),
                label = new Color(0.95f, 0.88f, 0.86f)
            },
            _ => new Palette
            {
                bg = new Color(0.2f, 0.24f, 0.31f, 0.97f),
                highlighted = new Color(0.3f, 0.38f, 0.45f, 1f),
                pressed = new Color(0.12f, 0.16f, 0.2f, 1f),
                outline = new Color(0.5f, 0.54f, 0.6f, 0.65f),
                label = new Color(0.92f, 0.9f, 0.84f)
            }
        };

        public static UnityEngine.UI.Button Button(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, Action onClick, ButtonStyle style = ButtonStyle.Secondary)
        {
            var pal = StyleOf(style);
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(6, 4);
            rt.offsetMax = new Vector2(-6, -4);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = pal.bg;
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = pal.highlighted;
            colors.pressedColor = pal.pressed;
            btn.colors = colors;

            // Defined edge so buttons read clearly against similarly dark panels.
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = pal.outline;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            var label = Label(go.transform, "Label", caption, 22, TextAnchor.MiddleCenter, pal.label);
            // Narrow best-fit range (was 12-28) — long captions ("Won't Ransom") still fit
            // without the font size swinging wildly between buttons in the same row.
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 22;

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public const float RowHeight = 0.09f;
        public const float RowGap = 0.015f;

        /// <summary>
        /// Advances a vertical list cursor by one row and returns its (top, bottom) bounds —
        /// the one piece of spacing math every Rebuild-a-list screen was hand-rolling slightly
        /// differently. Centralizing it means every list in the game uses the same row rhythm.
        /// </summary>
        public static (float top, float bottom) NextRow(ref float y, float rowHeight = RowHeight, float gap = RowGap)
        {
            var top = y;
            var bottom = y - rowHeight;
            y = bottom - gap;
            return (top, bottom);
        }

        /// <summary>
        /// Lays out evenly-sized buttons across one row (the "Recruit / Ransom / Release"
        /// three-way pattern, or any N-way variant), each with its own style.
        /// </summary>
        public static UnityEngine.UI.Button[] ButtonRow(Transform parent, string namePrefix, float top, float bottom,
            (string caption, Action onClick, ButtonStyle style)[] buttons, float innerGap = 0.02f)
        {
            var n = buttons.Length;
            var slot = 1f / n;
            var result = new UnityEngine.UI.Button[n];
            for (var i = 0; i < n; i++)
            {
                var xMin = i * slot + (i == 0 ? 0f : innerGap * 0.5f);
                var xMax = (i + 1) * slot - (i == n - 1 ? 0f : innerGap * 0.5f);
                result[i] = Button(parent, $"{namePrefix}_{i}", buttons[i].caption, new Vector2(xMin, bottom), new Vector2(xMax, top), buttons[i].onClick, buttons[i].style);
            }

            return result;
        }

        /// <summary>
        /// Character image loaded from Resources/Portraits/{portraitId}. Falls back to a
        /// deterministic colored square with initials when no art exists yet for that id.
        /// </summary>
        public static UnityEngine.UI.Image Portrait(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string portraitId) =>
            IconBox(parent, name, anchorMin, anchorMax, "Portraits/" + portraitId,
                PortraitLoader.PlaceholderColor(portraitId), PortraitLoader.Initials(portraitId));

        /// <summary>
        /// Generic image slot: loads a sprite from any Resources path, falling back to a
        /// solid placeholder color + short label when no art exists yet.
        /// </summary>
        public static UnityEngine.UI.Image IconBox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string resourcePath, Color placeholderColor, string placeholderLabel)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<UnityEngine.UI.Image>();

            var sprite = PortraitLoader.LoadRaw(resourcePath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = placeholderColor;
                var label = Label(go.transform, "Placeholder", placeholderLabel, 32, TextAnchor.MiddleCenter, Color.white);
                var lrt = label.GetComponent<RectTransform>();
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            return img;
        }

        public static UnityEngine.UI.InputField Input(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // Built inactive: InputField.OnEnable() runs the moment the component is added to an
            // active GameObject, and if textComponent/placeholder aren't wired up yet at that
            // point, its internal caret/render state gets stuck half-initialized — symptoms are
            // exactly "can't see what you type, only the first character seems to register."
            // Wiring everything up before activating avoids that.
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 4);
            rt.offsetMax = new Vector2(-12, -4);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.11f, 0.13f, 0.95f);
            var input = go.AddComponent<UnityEngine.UI.InputField>();
            input.targetGraphic = img;

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

            go.SetActive(true);
            return input;
        }
    }
}
