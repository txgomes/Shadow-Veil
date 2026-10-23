using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowVeil.Summoner
{
    /// <summary>Small helpers for building uGUI from code, in the dark-brown and gold First Summoner style.</summary>
    public static class Ui
    {
        public static readonly Color Panel = new Color(0.09f, 0.07f, 0.06f, 0.92f);
        public static readonly Color Frame = new Color(0.55f, 0.42f, 0.24f, 1f);
        public static readonly Color Gold = new Color(0.95f, 0.80f, 0.50f, 1f);
        public static readonly Color Parchment = new Color(0.86f, 0.80f, 0.68f, 1f);
        public static readonly Color ManaPurple = new Color(0.58f, 0.25f, 0.90f, 1f);

        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            GameObject obj = new GameObject(name);
            Canvas canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;
            obj.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return canvas;
        }

        public static RectTransform Area(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image Box(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            RectTransform rect = Area(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A panel with a thin gold border, like the game's dialogue frames.</summary>
        public static Image Framed(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color fill, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            Image border = Box(name, parent, anchorMin, anchorMax, Frame, offsetMin, offsetMax);
            Box("Fill", border.transform, Vector2.zero, Vector2.one, fill, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            return border;
        }

        public static Text Label(string name, Transform parent, string value, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = Area(name, parent, anchorMin, anchorMax);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        public static Image Bar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, bool vertical = false)
        {
            Image back = Box(name + " back", parent, anchorMin, anchorMax, new Color(0.03f, 0.02f, 0.03f, 0.9f));
            Image fill = Box(name, back.transform, Vector2.zero, Vector2.one, color, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.sprite = WhiteSprite();
            return fill;
        }

        public static Button ActionButton(string name, Transform parent, string caption, Vector2 anchorMin, Vector2 anchorMax, UnityAction action)
        {
            Image frame = Framed(name, parent, anchorMin, anchorMax, new Color(0.20f, 0.34f, 0.16f, 1f));
            frame.raycastTarget = true;
            Button button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(action);
            Label("Caption", frame.transform, caption, 30, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
            return button;
        }

        private static Sprite white;

        /// <summary>Filled images need a sprite to clip against.</summary>
        public static Sprite WhiteSprite()
        {
            if (white != null) return white;
            Texture2D texture = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            white = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            Art.Own(texture);
            Art.Own(white);
            return white;
        }

        public static void ReleaseCache()
        {
            white = null;
        }
    }
}
