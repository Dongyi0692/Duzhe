using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.Presentation
{
    /// Engine-rendered blackboard text layer (R14 blackboardRule): the CG04
    /// blackboard is delivered BLANK; lesson text and the farewell tracing
    /// result are drawn by this overlay with the engine font, never baked
    /// into the PNG. The rect is measured on the delivered CG04 illustration
    /// itself (board face (265,25)-(503,95) in its 640x360 canvas ->
    /// (530,50)-(1006,190) in the 1280x720 reference, centre (768,120),
    /// 480x140). The R04 Classroom SCENE board (544,40,448,112) is a
    /// different surface used when the world scene shows the board.
    public static class BlackboardTextLayer
    {
        private static GameObject current;

        public static bool IsShowing { get { return current != null; } }

        public static void Show(string[] lines)
        {
            Hide();
            GameObject canvas = GameObject.Find(Core.GameBootstrap.UiCanvasName);
            if (canvas == null)
            {
                Debug.LogError("[Duzhe] BlackboardTextLayer.Show: UI canvas not found.");
                return;
            }
            current = new GameObject("DZ_BlackboardText");
            current.transform.SetParent(canvas.transform, false);
            RectTransform rt = current.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // Just above a presentation CG, below HUD/dialogue/pause.
            current.transform.SetSiblingIndex(1);

            // The child object carries the Text so the Text's own
            // RectTransform uses the board rect without fighting the
            // stretch root.
            GameObject textGo = new GameObject("BoardText");
            textGo.transform.SetParent(current.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = Core.GameBootstrap.UiFont;
            text.fontSize = 34;
            text.color = new Color32(240, 240, 232, 255); // chalk
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // never swallow clicks meant for the dialogue
            // CG04 board face in the 1280x720 reference (top-left origin).
            // anchor=(0,1) + pivot=(0,0.5): anchoredPosition.y is measured
            // UP from the canvas TOP, so a board centre 120px BELOW the top
            // must be y = -120. (+120 pushed the text off-screen; the old
            // comment claimed the opposite.)
            RectTransform trt = text.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(530f, -120f);
            trt.sizeDelta = new Vector2(476f, 140f);
            text.text = string.Join("\n", lines ?? new string[0]);
        }

        public static void Hide()
        {
            if (current != null)
            {
                UnityEngine.Object.Destroy(current);
                current = null;
            }
        }
    }
}
