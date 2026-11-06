using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.Presentation
{
    /// Full-screen presentation CG tableau (R14). The delivered CGs are
    /// STATIC baked tableaus: they may be shown while the farewell dialogue
    /// plays, but a spirit departure must NEVER be faked by fading the whole
    /// image - PresentationLayers.json requires switching back to the clean
    /// scene and fading only the spirit actors (see CH01FarewellCast).
    public static class PresentationCG
    {
        private static GameObject current;

        public static bool IsShowing { get { return current != null; } }

        /// Shows a formal illustration full-screen (640x360 art stretched to
        /// the 1280x720 reference; same 16:9, no distortion).
        public static void Show(string assetId)
        {
            Sprite sprite = Core.FormalArt.LoadIllustration(assetId);
            if (sprite == null)
            {
                Debug.LogError("[Duzhe] PresentationCG.Show: illustration missing: " + assetId);
                return;
            }
            Hide();
            GameObject canvas = GameObject.Find(Core.GameBootstrap.UiCanvasName);
            if (canvas == null)
            {
                Debug.LogError("[Duzhe] PresentationCG.Show: UI canvas not found.");
                return;
            }
            current = new GameObject("DZ_PresentationCG");
            current.transform.SetParent(canvas.transform, false);
            RectTransform rt = current.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = current.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            // Bottom of the canvas stack: HUD, dialogue and pause all render
            // above the tableau; the 3D scene is fully covered.
            current.transform.SetAsFirstSibling();
        }

        // A cinematic area above the 200 reference-pixel dialogue bar keeps
        // the entire boat visible instead of cropping it under the text.
        public static void ShowRecollection(string assetId)
        {
            Show(assetId);
            if (current == null) return;
            Image matte = current.GetComponent<Image>();
            Sprite sprite = matte.sprite;
            matte.sprite = null;
            matte.color = new Color32(12, 25, 39, 255);
            GameObject art = new GameObject("RecollectionArt");
            art.transform.SetParent(current.transform, false);
            Image image = art.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, 204f);
            rt.offsetMax = new Vector2(0f, -4f);
        }

        public static void SetOpacity(float alpha)
        {
            if (current == null) return;
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group == null) group = current.AddComponent<CanvasGroup>();
            group.alpha = Mathf.Clamp01(alpha);
            group.blocksRaycasts = false;
        }

        public static void Hide()
        {
            if (current != null)
            {
                current.SetActive(false);
                UnityEngine.Object.Destroy(current);
                current = null;
            }
        }
    }
}
