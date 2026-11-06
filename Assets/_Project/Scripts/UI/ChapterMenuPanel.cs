using Duzhe.Chapters;
using Duzhe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// C09 chapter list. Only chapters the save records as completed are
    /// listed (no spoilers, no locked placeholders); picking one starts a
    /// ChapterReplay run, which never writes progress.
    public class ChapterMenuPanel : MonoBehaviour
    {
        private GameObject panel;
        private Transform rowsRoot;
        private bool built;

        public bool IsShowing { get { return panel != null && panel.activeSelf; } }

        private void Build()
        {
            if (built) return;
            built = true;

            panel = new GameObject("ChapterMenuPanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);
            DuzheUiFactory.AddFullStretchImage(panel.transform, "Dim", new Color32(6, 5, 9, 226), true);
            DuzheUiFactory.AddLabel(panel.transform, "ChapterTitle",
                new Vector2(340f, -96f), new Vector2(600f, 48f), 34,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "章节目录";

            GameObject rows = new GameObject("ChapterRows");
            rowsRoot = rows.AddComponent<RectTransform>();
            rows.transform.SetParent(panel.transform, false);
            DuzheUiFactory.MakeFullStretch(rows);

            DuzheUiFactory.AddButton(panel.transform, "ChapterBack",
                new Vector2(540f, -620f), new Vector2(200f, 48f), "返回", 24, () => Hide());
            panel.SetActive(false);
        }

        public void Show()
        {
            Build();
            GameSession session = GameSession.Instance;
            if (session == null || session.Save == null) return;
            ClearRows();

            float rowY = 200f;
            int count = 0;
            foreach (string id in ChapterCatalog.FormalChapterIds)
            {
                if (!session.Save.HasCompletedChapter(id)) continue;
                ChapterConfig cfg = ChapterCatalog.GetChapter(id);
                if (cfg == null) continue;
                string chapterId = id; // captured per row
                DuzheUiFactory.AddButton(rowsRoot, "ChapterRow_" + chapterId,
                    new Vector2(440f, -rowY), new Vector2(400f, 52f),
                    cfg.displayName, 26, () => SelectCompletedChapter(chapterId));
                rowY += 62f;
                count++;
            }
            if (count == 0)
            {
                DuzheUiFactory.AddLabel(rowsRoot, "ChapterEmpty",
                    new Vector2(340f, -220f), new Vector2(600f, 40f), 24,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("chapters.empty", "通关后，可以在这里重玩章节。");
            }

            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// C12: ESC returns to the pause overlay (the pause menu leaves ESC to
        /// this panel while it is showing), so the chapter list is never a
        /// dead end.
        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            Core.InputRouter router = Core.InputRouter.Instance;
            if (router == null || !router.EscapePressed) return;
            router.ConsumePendingPresses();
            Hide();
        }

        /// Re-validates the click against the save (a stale button must never
        /// start a chapter the player has not finished).
        private void SelectCompletedChapter(string chapterId)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null || session.Save == null) return;
            if (!session.Save.HasCompletedChapter(chapterId))
            {
                Debug.LogWarning("[Duzhe] Chapter replay refused: not completed: " + chapterId);
                return;
            }
            if (session.Transitions != null && session.Transitions.IsTransitioning) return;
            if (session.Audio != null) session.Audio.PlaySfx("SFX01_UIConfirm");
            Hide();
            session.Chapters.StartChapter(chapterId, GameMode.ChapterReplay);
        }

        private void ClearRows()
        {
            if (rowsRoot == null) return;
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = rowsRoot.GetChild(i).gameObject;
                child.SetActive(false);   // no stale clickable frame
                Destroy(child);
            }
        }
    }
}
