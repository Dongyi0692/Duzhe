using Duzhe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// CH05 end credits. Shown once when the chapter reaches CH05_Complete;
    /// the chapter is completed ONLY by the player pressing the confirm
    /// button, which publishes exactly one CreditsConfirmed event captured
    /// when the panel was shown (a stale button from an old run can never
    /// complete a new one). Hiding without confirming (pause-menu leave,
    /// scene exit) publishes nothing.
    public class CreditsPanel : MonoBehaviour
    {
        private GameObject panel;
        private bool built;
        private bool sent;
        private string capturedRunId;
        private GameEvent completion;

        public bool IsShowing { get { return panel != null && panel.activeSelf; } }

        private void Build()
        {
            if (built) return;
            built = true;

            panel = new GameObject("CreditsPanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);
            DuzheUiFactory.AddFullStretchImage(panel.transform, "Dim", new Color32(8, 6, 10, 236), true);

            DuzheUiFactory.AddLabel(panel.transform, "CreditsTitle",
                new Vector2(340f, -64f), new Vector2(600f, 70f), 56,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("game.title", "渡者");
            DuzheUiFactory.AddLabel(panel.transform, "CreditsSubtitle",
                new Vector2(340f, -142f), new Vector2(600f, 34f), 24,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("menu.subtitle", "把没说完的话，好好说完。");

            DuzheUiFactory.AddLabel(panel.transform, "CreditsHeading",
                new Vector2(340f, -212f), new Vector2(600f, 32f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "制作名单";

            string[] roles = { "策划与叙事设计", "美术与视觉设计", "程序与关卡集成" };
            string[] names = { "東壹", "Kimi · ChatGPT", "Codely · Kimi · DeepSeek · ChatGPT" };
            for (int i = 0; i < roles.Length; i++)
            {
                float y = -270f - i * 46f;
                DuzheUiFactory.AddLabel(panel.transform, "CreditsRole_" + i,
                    new Vector2(160f, y), new Vector2(320f, 34f), 24,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleRight).text = roles[i];
                DuzheUiFactory.AddLabel(panel.transform, "CreditsName_" + i,
                    new Vector2(520f, y), new Vector2(620f, 34f), 24,
                    new Color32(238, 232, 218, 255), TextAnchor.MiddleLeft).text = names[i];
            }
            DuzheUiFactory.AddLabel(panel.transform, "CreditsSpecialThanks",
                new Vector2(340f, -426f), new Vector2(600f, 32f), 23,
                new Color32(206, 184, 141, 255), TextAnchor.MiddleCenter).text = "特别鸣谢";
            DuzheUiFactory.AddLabel(panel.transform, "CreditsThanksNames",
                new Vector2(240f, -466f), new Vector2(800f, 34f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "Unity 中国 · 团结 Codely";
            DuzheUiFactory.AddLabel(panel.transform, "CreditsEngine",
                new Vector2(340f, -524f), new Vector2(600f, 30f), 20,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text = "团结引擎 1.10.4";
            DuzheUiFactory.AddLabel(panel.transform, "CreditsThanks",
                new Vector2(240f, -566f), new Vector2(800f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text = "感谢你陪他们走完这一程。";

            DuzheUiFactory.AddButton(panel.transform, "CreditsConfirm",
                new Vector2(490f, -626f), new Vector2(300f, 56f), "返回开始界面", 24,
                () => Confirm());

            panel.SetActive(false);
        }

        /// Shows the credits for the current run and captures the completion
        /// event. Idempotent per run: re-entering the stage does not reset
        /// the "already confirmed" state of a finished chapter.
        public void Show()
        {
            Build();
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null) return;
            if (IsShowing) return;

            capturedRunId = session.RunId;
            completion = new GameEvent(GameEventType.CreditsConfirmed,
                "CH05", "CH05_Complete", "CH05_CREDITS", session.RunId);
            sent = false;
            // The carrier is a direct Canvas child; moving only its child
            // cannot raise the credits above the scene's warm-fade overlay.
            transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        private void Confirm()
        {
            if (sent) return;
            GameSession session = GameSession.Instance;
            if (session == null) return;
            if (session.RunId != capturedRunId) return;        // stale button, other run
            if (session.Paused) return;                         // pause overlay owns input
            if (session.Transitions != null && session.Transitions.IsTransitioning) return;
            if (session.Chapters == null || session.Chapters.CurrentStageId != "CH05_Complete") return;

            sent = true;
            HideWithoutEvent();
            session.PublishEvent(completion);
            Debug.Log("[Duzhe] Credits confirmed: CH05 complete.");
        }

        /// Closes the panel WITHOUT completing the chapter (pause-menu leave,
        /// scene exit). Never destroys the persistent canvas.
        public void HideWithoutEvent()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            // Leaving the stage by any other route must not leave the panel
            // floating over the next scene.
            if (!IsShowing) return;
            GameSession session = GameSession.Instance;
            if (session == null) return;
            if (session.RunId != capturedRunId ||
                (session.Chapters != null && session.Chapters.CurrentStageId != "CH05_Complete"))
            {
                HideWithoutEvent();
            }
        }
    }
}
