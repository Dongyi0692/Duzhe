using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// Code-built UGUI helpers. All UI is constructed at runtime so scene
    /// files stay free of UI objects and ProjectSetupUtility never has to
    /// duplicate them. Reference design size: 1280x720, origin top-left.
    public static class DuzheUiFactory
    {
        public const int CanvasSortingOrder = 100;

        public static GameObject CreateCanvas(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return go;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject es = new GameObject("DZ_EventSystem");
            // CRITICAL: parent under the persistent GameRoot. A scene-level
            // EventSystem dies on the first scene switch and every mouse
            // click in the whole game stops working afterwards.
            Core.GameBootstrap root = Core.GameBootstrap.Instance;
            if (root != null) es.transform.SetParent(root.transform, false);
            else Object.DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        public static T EnsureComponent<T>(Transform parent) where T : Component
        {
            GameObject go = new GameObject(typeof(T).Name);
            // Explicit RectTransform BEFORE parenting: the engine's automatic
            // Transform->RectTransform conversion under a Canvas is not
            // guaranteed synchronously after SetParent, and a plain Transform
            // carrier would offset all absolute-positioned children.
            RectTransform rt = go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return go.AddComponent<T>();
        }

        /// Stretches a plain GameObject (which auto-got a tiny centred
        /// RectTransform under a Canvas) to the full parent canvas area, so
        /// children with absolute reference coordinates land where intended.
        public static RectTransform MakeFullStretch(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image AddFullStretchImage(Transform parent, string name, Color color,
            bool blocksRaycast = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = blocksRaycast;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return img;
        }

        /// Panel anchored at the top-left of its parent; position is the
        /// top-left corner offset (x right, y down), size in reference pixels.
        public static RectTransform AddPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            // C10 (R03): use the delivered nine-slice skin when present; the
            // requested colour stays as the alpha only (design colours come
            // from the art, not from a flat tint).
            Sprite skin = PanelSkin;
            if (skin != null)
            {
                img.sprite = skin;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f; // 1:1 border scaling on the canvas
                img.color = new Color(1f, 1f, 1f, color.a);
            }
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        // R03 skins are optional: missing art falls back to flat colours.
        // The skins are ON (user decision 2026-10-30). The earlier "card edge
        // does not line up with the text" report was NOT the nine-slice
        // scaling: those card panels were positioned with a POSITIVE
        // anchoredPosition.y, which puts a top-left anchored rect ABOVE the
        // canvas. Fixed at the call sites; the skins themselves are fine.
        public const bool UseR03Skins = true;

        private static Sprite panelSkin;
        private static Sprite buttonSkin;
        private static bool skinsLoaded;

        private static void LoadSkins()
        {
            if (skinsLoaded) return;
            skinsLoaded = true;
            if (!UseR03Skins) return;
            panelSkin = Resources.Load<Sprite>("Art/UI/UI01_GenericPanel");
            buttonSkin = Resources.Load<Sprite>("Art/UI/UI01_Button");
        }

        public static Sprite PanelSkin
        {
            get { LoadSkins(); return panelSkin; }
        }

        public static Sprite ButtonSkin
        {
            get { LoadSkins(); return buttonSkin; }
        }

        /// Non-interactive sprite panel at a top-left position, used for the
        /// formal 640x360 minigame board art.
        public static Image AddSpritePanel(Transform parent, string name, Vector2 position,
            Vector2 size, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = false;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return img;
        }

        /// Full-width panel pinned to the BOTTOM edge (dialogue bar). Anchored
        /// to both bottom corners so it keeps its height and spans the whole
        /// canvas at any aspect ratio, instead of a fixed 1280px box that
        /// drifts off-centre on non-16:9 windows.
        public static Image AddBottomPanel(Transform parent, string name, float height, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            Sprite skin = PanelSkin;
            if (skin != null)
            {
                img.sprite = skin;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
                img.color = new Color(1f, 1f, 1f, color.a);
            }
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            return img;
        }

        public static Text AddLabel(Transform parent, string name, Vector2 position, Vector2 size,
            int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = Core.GameBootstrap.UiFont;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.supportRichText = false;
            RectTransform rt = t.rectTransform;
            // Semantics: position = the label's TOP-LEFT corner (x from the
            // left edge, y down from the top), same as AddPanel/AddButton.
            // Call sites compute centre positions as centreX - width/2.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button AddButton(Transform parent, string name, Vector2 position, Vector2 size,
            string label, int fontSize, System.Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color32(52, 48, 62, 255);
            // C10 (R03): nine-slice button skin when delivered.
            Sprite skin = ButtonSkin;
            if (skin != null)
            {
                img.sprite = skin;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f; // 1:1 border scaling on the canvas
                img.color = Color.white;
            }
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Button btn = go.AddComponent<Button>();
            btn.transition = Button.Transition.ColorTint;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            Text t = AddLabel(go.transform, "Label", new Vector2(0f, 0f), size, fontSize,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter);
            t.text = label;
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = Vector2.zero;
            return btn;
        }

        public static Slider AddSlider(Transform parent, string name, Vector2 position, Vector2 size,
            float value, System.Action<float> onChange)
        {
            // Fixed-size container, top-left semantics like AddPanel/AddButton.
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            // Track background, stretched inside the container.
            GameObject bgGo = new GameObject("Background");
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgGo.transform.SetParent(rt, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color32(38, 34, 44, 255);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill area with side padding; the Slider drives Fill's anchors,
            // so Fill must have ZERO sizeDelta (a leftover 100x100 default
            // turns the fill into an oversized block that swallows clicks).
            GameObject fillAreaGo = new GameObject("FillArea");
            RectTransform fillArea = fillAreaGo.AddComponent<RectTransform>();
            fillAreaGo.transform.SetParent(rt, false);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(4f, 3f);
            fillArea.offsetMax = new Vector2(-4f, -3f);

            GameObject fillGo = new GameObject("Fill");
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillGo.transform.SetParent(fillArea, false);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color32(150, 170, 220, 255);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = Vector2.zero;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Handle slide area with side padding; the Slider drives the
            // handle's anchor point along this area.
            GameObject handleAreaGo = new GameObject("HandleArea");
            RectTransform handleArea = handleAreaGo.AddComponent<RectTransform>();
            handleAreaGo.transform.SetParent(rt, false);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, -2f);
            handleArea.offsetMax = new Vector2(-8f, 2f);

            GameObject handleGo = new GameObject("Handle");
            RectTransform handleRt = handleGo.AddComponent<RectTransform>();
            handleGo.transform.SetParent(handleArea, false);
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color32(228, 224, 210, 255);
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(12f, 20f);
            handleRt.anchoredPosition = Vector2.zero;

            Slider slider = go.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }
    }
}
