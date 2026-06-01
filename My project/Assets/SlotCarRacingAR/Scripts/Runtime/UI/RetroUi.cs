using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    public static class RetroUi
    {
        public static readonly Color Cream = Hex(0xEAE3CD);
        public static readonly Color CreamLight = Hex(0xFCF9F8);
        public static readonly Color Teal = Hex(0x23556D);
        public static readonly Color TealDark = Hex(0x173F52);
        public static readonly Color Red = Hex(0xD64030);
        public static readonly Color RedDark = Hex(0xA62118);
        public static readonly Color Yellow = Hex(0xF9B731);
        public static readonly Color Green = Hex(0x46C86B);
        public static readonly Color Black = Hex(0x1A1A1A);
        public static readonly Color White = Hex(0xFFF6E5);

        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;
        private static Sprite _logoSprite;
        private static Font _font;
        private const string LogoResourcePath = "UI/Logo Face2Race";

        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform CreateFullScreenBackground(Transform parent, string name, bool blockInput)
        {
            GameObject rootObj = new GameObject(name);
            rootObj.transform.SetParent(parent, false);
            RectTransform root = rootObj.AddComponent<RectTransform>();
            Fill(root);

            Image baseImage = rootObj.AddComponent<Image>();
            baseImage.color = Cream;
            baseImage.raycastTarget = blockInput;

            GameObject bandObj = new GameObject("TealSpeedBand");
            bandObj.transform.SetParent(root, false);
            RectTransform band = bandObj.AddComponent<RectTransform>();
            band.anchorMin = new Vector2(0.10f, -0.36f);
            band.anchorMax = new Vector2(1.08f, 0.88f);
            band.offsetMin = Vector2.zero;
            band.offsetMax = Vector2.zero;
            band.localEulerAngles = new Vector3(0f, 0f, -28f);
            Image bandImage = bandObj.AddComponent<Image>();
            bandImage.color = Teal;
            bandImage.raycastTarget = false;

            CreateCheckerAccent(root, "TopLeftCheckers", new Vector2(-0.02f, 0.76f), new Vector2(0.34f, 0.97f), 10, 3);
            CreateCheckerAccent(root, "BottomRightCheckers", new Vector2(0.70f, 0.02f), new Vector2(1.02f, 0.18f), 9, 2);

            return root;
        }

        public static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color fillColor,
            bool raycastTarget = false,
            bool shadow = true,
            bool outline = true)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.AddComponent<Image>();
            image.color = fillColor;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = raycastTarget;

            if (shadow)
            {
                Shadow shadowEffect = obj.AddComponent<Shadow>();
                shadowEffect.effectColor = Black;
                shadowEffect.effectDistance = new Vector2(8f, -8f);
                shadowEffect.useGraphicAlpha = true;
            }

            if (outline)
            {
                Outline outlineEffect = obj.AddComponent<Outline>();
                outlineEffect.effectColor = Black;
                outlineEffect.effectDistance = new Vector2(4f, -4f);
                outlineEffect.useGraphicAlpha = true;
            }

            return rect;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color fillColor,
            Color textColor,
            int fontSize)
        {
            RectTransform rect = CreatePanel(parent, name, anchorMin, anchorMax, fillColor, true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = fillColor;
            colors.highlightedColor = Color.Lerp(fillColor, White, 0.12f);
            colors.pressedColor = Color.Lerp(fillColor, Black, 0.18f);
            colors.selectedColor = fillColor;
            colors.disabledColor = WithAlpha(fillColor, 0.45f);
            button.colors = colors;
            rect.gameObject.AddComponent<RetroButtonPress>();

            Text text = CreateText(
                rect,
                "Label",
                label.ToUpperInvariant(),
                Vector2.zero,
                Vector2.one,
                fontSize,
                textColor,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                textColor != Black);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 14);
            text.resizeTextMaxSize = fontSize;

            return button;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string content,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            Color color,
            TextAnchor alignment,
            FontStyle style = FontStyle.Bold,
            bool outline = true)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            if (outline)
            {
                Outline outlineEffect = obj.AddComponent<Outline>();
                outlineEffect.effectColor = Black;
                outlineEffect.effectDistance = new Vector2(2.5f, -2.5f);

                Shadow shadow = obj.AddComponent<Shadow>();
                shadow.effectColor = Black;
                shadow.effectDistance = new Vector2(4f, -4f);
            }

            return text;
        }

        public static Image CreateLogo(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool preserveAspect = true)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.AddComponent<Image>();
            image.sprite = LogoSprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;

            if (image.sprite == null)
            {
                image.enabled = false;
                CreateText(
                    rect,
                    "LogoFallback",
                    "FACE 2 RACE",
                    Vector2.zero,
                    Vector2.one,
                    48,
                    White,
                    TextAnchor.MiddleCenter,
                    FontStyle.BoldAndItalic);
            }

            return image;
        }

        public static void StyleImageAsPanel(Image image, Color fillColor, bool shadow = true, bool outline = true)
        {
            if (image == null)
            {
                return;
            }

            image.color = fillColor;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;

            if (shadow && image.GetComponent<Shadow>() == null)
            {
                Shadow shadowEffect = image.gameObject.AddComponent<Shadow>();
                shadowEffect.effectColor = Black;
                shadowEffect.effectDistance = new Vector2(8f, -8f);
                shadowEffect.useGraphicAlpha = true;
            }

            if (outline && image.GetComponent<Outline>() == null)
            {
                Outline outlineEffect = image.gameObject.AddComponent<Outline>();
                outlineEffect.effectColor = Black;
                outlineEffect.effectDistance = new Vector2(4f, -4f);
                outlineEffect.useGraphicAlpha = true;
            }
        }

        public static void StyleImageAsCircle(Image image, Color fillColor, bool shadow = true, bool outline = true)
        {
            if (image == null)
            {
                return;
            }

            image.color = fillColor;
            image.sprite = CircleSprite;
            image.type = Image.Type.Simple;

            if (shadow && image.GetComponent<Shadow>() == null)
            {
                Shadow shadowEffect = image.gameObject.AddComponent<Shadow>();
                shadowEffect.effectColor = Black;
                shadowEffect.effectDistance = new Vector2(8f, -8f);
                shadowEffect.useGraphicAlpha = true;
            }

            if (outline && image.GetComponent<Outline>() == null)
            {
                Outline outlineEffect = image.gameObject.AddComponent<Outline>();
                outlineEffect.effectColor = Black;
                outlineEffect.effectDistance = new Vector2(5f, -5f);
                outlineEffect.useGraphicAlpha = true;
            }
        }

        public static Image CreateStatusLight(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.AddComponent<Image>();
            image.sprite = CircleSprite;
            image.color = color;
            image.raycastTarget = false;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = Black;
            outline.effectDistance = new Vector2(4f, -4f);

            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = Black;
            shadow.effectDistance = new Vector2(5f, -5f);

            return image;
        }

        public static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void CreateCheckerAccent(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int columns,
            int rows)
        {
            GameObject rootObj = new GameObject(name);
            rootObj.transform.SetParent(parent, false);
            RectTransform root = rootObj.AddComponent<RectTransform>();
            root.anchorMin = anchorMin;
            root.anchorMax = anchorMax;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localEulerAngles = new Vector3(0f, 0f, -8f);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (((x + y) & 1) != 0)
                    {
                        continue;
                    }

                    GameObject squareObj = new GameObject("Square");
                    squareObj.transform.SetParent(root, false);
                    RectTransform square = squareObj.AddComponent<RectTransform>();
                    square.anchorMin = new Vector2(x / (float)columns, y / (float)rows);
                    square.anchorMax = new Vector2((x + 1f) / columns, (y + 1f) / rows);
                    square.offsetMin = Vector2.zero;
                    square.offsetMax = Vector2.zero;
                    Image image = squareObj.AddComponent<Image>();
                    image.color = Black;
                    image.raycastTarget = false;
                }
            }
        }

        private static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite == null)
                {
                    _roundedSprite = CreateRoundedSprite(64, 18);
                }

                return _roundedSprite;
            }
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                {
                    _circleSprite = CreateCircleSprite(64);
                }

                return _circleSprite;
            }
        }

        private static Sprite LogoSprite
        {
            get
            {
                if (_logoSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(LogoResourcePath);
                    if (texture != null)
                    {
                        _logoSprite = Sprite.Create(
                            texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f);
                    }
                }

                return _logoSprite;
            }
        }

        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, size, radius);
                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * radius);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float center = (size - 1f) * 0.5f;
            float radius = center - 1f;
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? Color.white : clear);
                }
            }

            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
        {
            int left = radius;
            int right = size - radius - 1;
            int bottom = radius;
            int top = size - radius - 1;

            if ((x >= left && x <= right) || (y >= bottom && y <= top))
            {
                return true;
            }

            int cx = x < left ? left : right;
            int cy = y < bottom ? bottom : top;
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
