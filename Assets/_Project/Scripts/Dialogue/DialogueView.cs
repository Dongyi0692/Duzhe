using Duzhe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.Dialogue
{
    /// UGUI dialogue panel, fully code-built (no scene assets). Shows one full
    /// line at a time; a click anywhere on the panel or Space/Return advances
    /// exactly one line. No typewriter, no auto-advance, no skip.
    [DefaultExecutionOrder(-25)]
    public class DialogueView : MonoBehaviour
    {
        private DialogueRunner runner;
        private GameObject panel;
        private Text speakerText;
        private Text lineText;
        private Text hintText;

        public void Bind(DialogueRunner dialogueRunner)
        {
            runner = dialogueRunner;
            if (runner != null) runner.BindView(this);
            BuildUi();
            HideLine();
        }

        private void BuildUi()
        {
            if (panel != null) return;

            panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);

            // Full-screen click catcher: any click on the panel advances.
            Image catcher = DuzheUiFactory.AddFullStretchImage(panel.transform, "Catcher", new Color32(0, 0, 0, 0));
            Button catchButton = catcher.gameObject.AddComponent<Button>();
            catchButton.transition = Button.Transition.None;
            catchButton.onClick.AddListener(() => { if (runner != null) runner.Advance(); });

            // Bottom dialogue bar (C12): pinned to the bottom edge and
            // stretched across the whole canvas, so it keeps its place at
            // every window size/aspect (it used to sit at the TOP because the
            // top-left anchored panel was placed at y = 0).
            Image bgImage = DuzheUiFactory.AddBottomPanel(panel.transform, "PanelBg",
                DialogueBarHeight, new Color32(14, 12, 18, 232));
            RectTransform bg = bgImage.rectTransform;
            // The bar and its texts must NOT eat the click: the full-screen
            // Catcher behind them owns "click anywhere to advance".
            bgImage.raycastTarget = false;
            // C10 (R03): the dialogue skin shares the DuzheUiFactory switch.
            if (DuzheUiFactory.UseR03Skins)
            {
                Sprite dialogueSkin = Resources.Load<Sprite>("Art/UI/UI01_DialoguePanel");
                if (dialogueSkin != null)
                {
                    bgImage.sprite = dialogueSkin;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.pixelsPerUnitMultiplier = 1f;
                    bgImage.color = new Color(1f, 1f, 1f, 0.94f);
                }
            }

            speakerText = DuzheUiFactory.AddLabel(bg.transform, "Speaker",
                new Vector2(70f, -18f), new Vector2(500f, 34f), 30, new Color32(222, 204, 128, 255),
                TextAnchor.UpperLeft);

            // The line stretches between fixed side margins so a wide window
            // never runs the text off the right edge.
            lineText = DuzheUiFactory.AddLabel(bg.transform, "Line",
                new Vector2(70f, -58f), new Vector2(1140f, 110f), 30, new Color32(238, 232, 218, 255),
                TextAnchor.UpperLeft);
            RectTransform lineRt = lineText.rectTransform;
            lineRt.anchorMin = new Vector2(0f, 1f);
            lineRt.anchorMax = new Vector2(1f, 1f);
            lineRt.pivot = new Vector2(0f, 1f);
            lineRt.offsetMin = new Vector2(70f, -168f);
            lineRt.offsetMax = new Vector2(-70f, -58f);

            // Hint centred on the bar regardless of the canvas width.
            hintText = DuzheUiFactory.AddLabel(bg.transform, "Hint",
                new Vector2(440f, -178f), new Vector2(400f, 26f), 20, new Color32(150, 144, 132, 255),
                TextAnchor.MiddleCenter);
            RectTransform hintRt = hintText.rectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 1f);
            hintRt.anchorMax = new Vector2(0.5f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -178f);
            hintRt.sizeDelta = new Vector2(600f, 26f);

            // Text graphics would otherwise swallow the advance click.
            speakerText.raycastTarget = false;
            lineText.raycastTarget = false;
            hintText.raycastTarget = false;
        }

        /// Height of the bottom dialogue bar in reference pixels.
        private const float DialogueBarHeight = 200f;

        public void ShowLine(string speaker, string text, int index, int total)
        {
            BuildUi();
            panel.SetActive(true);
            speakerText.text = speaker;
            lineText.text = text;
            // C12: no "(21/26)" counter - the player only sees how to continue.
            hintText.text = UiText.Get("hint.dialogue_next", "点击对白区域或按空格继续");
        }

        public void HideLine()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            // Keyboard advance path (Space/Return via the router, already
            // press-once semantics and only in Dialogue mode).
            Core.InputRouter router = Core.InputRouter.Instance;
            if (router != null && router.AdvancePressed && runner != null)
            {
                runner.Advance();
            }
        }
    }
}
