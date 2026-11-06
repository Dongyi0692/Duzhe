using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// One-shot full-screen fade used by the chapter closing beat
    /// (CH01_Complete). Fades to black over the given game-time duration
    /// (freezes with the pause overlay), invokes the callback when fully
    /// black, then fades back out and removes itself. Only one fade runs at
    /// a time; starting a new one replaces the running one.
    public class FadeOverlay : MonoBehaviour
    {
        private static FadeOverlay active;

        private Image img;
        private float phaseSeconds;
        private float remaining;
        private bool fadingOut;
        private System.Action onBlack;

        /// Begins a fade-to-black. Safe to call from any system.
        public static void Begin(float seconds)
        {
            if (seconds <= 0f) seconds = 0.1f;
            if (active != null) Destroy(active.gameObject);
            GameObject canvas = GameObject.Find(Core.GameBootstrap.UiCanvasName);
            if (canvas == null) return;
            GameObject go = new GameObject("DZ_FadeOverlay");
            go.transform.SetParent(canvas.transform, false);
            // A plain GameObject under a canvas is NOT full-screen by default;
            // stretch it explicitly so the fade really covers the view.
            DuzheUiFactory.MakeFullStretch(go);
            active = go.AddComponent<FadeOverlay>();
            active.img = go.AddComponent<Image>();
            active.img.color = new Color(0f, 0f, 0f, 0f);
            active.img.raycastTarget = true; // input is locked during the fade anyway
            active.phaseSeconds = seconds;
            active.remaining = seconds;
            active.onBlack = null;
            go.transform.SetAsLastSibling(); // above everything (incl. pause)
        }

        /// Cancels a running fade and drops its callback: a run teardown must
        /// never fire a stale onBlack into the next run.
        public static void Cancel()
        {
            if (active == null) return;
            FadeOverlay previous = active;
            active = null;
            previous.onBlack = null;
            previous.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(previous.gameObject);
        }

        /// Begin with a callback invoked once the screen is fully black.
        public static void Begin(float seconds, System.Action onBlack)
        {
            Begin(seconds);
            if (active != null) active.onBlack = onBlack;
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
        }

        private void Update()
        {
            if (!fadingOut)
            {
                remaining -= Time.deltaTime;
                float a = Mathf.Clamp01(1f - remaining / Mathf.Max(0.0001f, phaseSeconds));
                img.color = new Color(0f, 0f, 0f, a);
                if (remaining <= 0f)
                {
                    img.color = new Color(0f, 0f, 0f, 1f);
                    System.Action cb = onBlack;
                    onBlack = null;
                    fadingOut = true;
                    remaining = 0.5f; // fade back out over the freshly loaded scene
                    if (cb != null) cb();
                }
            }
            else
            {
                remaining -= Time.deltaTime;
                float a = Mathf.Clamp01(remaining / 0.5f);
                img.color = new Color(0f, 0f, 0f, a);
                if (remaining <= 0f) Destroy(gameObject);
            }
        }
    }
}
