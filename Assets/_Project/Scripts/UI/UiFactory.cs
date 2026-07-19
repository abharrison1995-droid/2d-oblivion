using System;
using UnityEngine;

namespace Voidovia
{
    public static class UiFactory
    {
        public static class Theme
        {
            // Dark, gritty Kenshi background with a touch of Oblivion parchment
            public static Color PanelBackground = new Color(0.85f, 0.85f, 0.85f, 1f); // Tint for the texture
            
            // Text colors
            public static Color TextTitle = new Color(0.95f, 0.88f, 0.70f, 1f); // Pale parchment/gold
            public static Color TextBody = new Color(0.82f, 0.78f, 0.72f, 1f); // Dirty parchment
            public static Color TextDim = new Color(0.60f, 0.58f, 0.52f, 1f); // Faded gritty text
            public static Color TextDark = new Color(0.10f, 0.08f, 0.05f, 1f); // Dark ink

            // Fallback flat colors
            public static Color PanelFallback = new Color(0.08f, 0.08f, 0.09f, 0.98f);
            public static Color OverlayDark = new Color(0.04f, 0.03f, 0.02f, 0.85f);
            public static Color InputFieldBg = new Color(0.06f, 0.05f, 0.04f, 0.95f);
        }

        static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts/MedievalFont");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite LoadSprite(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        static Sprite _panelBg;
        public static Sprite PanelBg
        {
            get
            {
                if (_panelBg != null) return _panelBg;
                _panelBg = LoadSprite("UI/panel_bg");
                return _panelBg;
            }
        }

        static Sprite _buttonBg;
        public static Sprite ButtonBg
        {
            get
            {
                if (_buttonBg != null) return _buttonBg;
                _buttonBg = LoadSprite("UI/button_bg");
                return _buttonBg;
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
            
            // If the caller requested a highly transparent color, it's likely an overlay
            if (color.a < 0.5f)
            {
                img.color = color;
            }
            else
            {
                img.sprite = PanelBg;
                img.type = UnityEngine.UI.Image.Type.Sliced; // Even if not a perfect 9-slice, it helps with stretching
                img.color = img.sprite != null ? color : Theme.PanelFallback;
            }
            
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
            
            // Add a subtle drop shadow to text for readability against gritty backgrounds
            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            
            return t;
        }

        public enum ButtonStyle { Secondary, Primary, Danger }

        struct Palette
        {
            public Color bg, highlighted, pressed, outline, label;
        }

        static Palette StyleOf(ButtonStyle style) => style switch
        {
            ButtonStyle.Primary => new Palette
            {
                bg = new Color(0.85f, 0.75f, 0.55f, 1f), // Parchment/gold tint for primary
                highlighted = new Color(1f, 0.95f, 0.75f, 1f),
                pressed = new Color(0.5f, 0.4f, 0.2f, 1f),
                outline = new Color(0.1f, 0.05f, 0.02f, 0.8f),
                label = Theme.TextDark
            },
            ButtonStyle.Danger => new Palette
            {
                bg = new Color(0.8f, 0.35f, 0.35f, 1f), // Rusty blood red
                highlighted = new Color(1f, 0.5f, 0.5f, 1f),
                pressed = new Color(0.4f, 0.15f, 0.15f, 1f),
                outline = new Color(0.15f, 0.05f, 0.05f, 0.8f),
                label = Theme.TextTitle
            },
            _ => new Palette
            {
                bg = new Color(0.7f, 0.7f, 0.7f, 1f), // Let the dark gritty texture show
                highlighted = new Color(0.9f, 0.9f, 0.9f, 1f),
                pressed = new Color(0.4f, 0.4f, 0.4f, 1f),
                outline = new Color(0.05f, 0.05f, 0.05f, 0.9f),
                label = Theme.TextBody
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
            img.sprite = ButtonBg;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = pal.bg;
            
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = pal.highlighted;
            colors.pressedColor = pal.pressed;
            btn.colors = colors;

            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = pal.outline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;

            var label = Label(go.transform, "Label", caption, 22, TextAnchor.MiddleCenter, pal.label);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 24;
            
            // If primary (dark text), remove the drop shadow for clarity
            if (style == ButtonStyle.Primary)
            {
                var shadow = label.GetComponent<UnityEngine.UI.Shadow>();
                if (shadow != null) UnityEngine.Object.DestroyImmediate(shadow);
            }

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public const float RowHeight = 0.09f;
        public const float RowGap = 0.015f;

        public static (float top, float bottom) NextRow(ref float y, float rowHeight = RowHeight, float gap = RowGap)
        {
            var top = y;
            var bottom = y - rowHeight;
            y = bottom - gap;
            return (top, bottom);
        }

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

        public static UnityEngine.UI.Image Portrait(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string portraitId) =>
            IconBox(parent, name, anchorMin, anchorMax, "Portraits/" + portraitId,
                PortraitLoader.PlaceholderColor(portraitId), PortraitLoader.Initials(portraitId));

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
            
            // Add a gritty border around icons
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.05f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            return img;
        }

        public static UnityEngine.UI.InputField Input(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 4);
            rt.offsetMax = new Vector2(-12, -4);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = Theme.InputFieldBg;
            var input = go.AddComponent<UnityEngine.UI.InputField>();
            input.targetGraphic = img;
            
            // Gritty border for input
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.2f, 0.15f, 0.1f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

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
            text.color = Theme.TextTitle;
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
            ph.color = Theme.TextDim;
            ph.text = placeholder;

            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;

            go.SetActive(true);
            return input;
        }
    }
}
