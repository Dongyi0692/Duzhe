using System.Collections;
using Duzhe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// One gallery entry per chapter. Each entry replays its complete visual
    /// sequence without starting a chapter or writing progress.
    public class GalleryController : MonoBehaviour
    {
        /// A chapter's stable gallery id and its ordered presentations.
        public class GalleryEntry
        {
            public string presentationId;
            public string chapterId;
            public string sceneId;
            public string[] presentationIds;
        }

        /// Five chapter entries; montage images are parts, never extra rows.
        public static readonly GalleryEntry[] Entries =
        {
            new GalleryEntry { presentationId = "PR_CH01_Farewell", chapterId = "CH01", sceneId = DuzheIds.SceneFarmCourtyard,
                presentationIds = new[] { "PR_CH01_Memory", "PR_CH01_Farewell" } },
            new GalleryEntry { presentationId = "PR_CH02_Farewell", chapterId = "CH02", sceneId = DuzheIds.ScenePark,
                presentationIds = new[] { "PR_CH02_Memory", "PR_CH02_Farewell" } },
            new GalleryEntry { presentationId = "PR_CH03_Farewell", chapterId = "CH03", sceneId = DuzheIds.SceneRecordingStudio,
                presentationIds = new[] { "PR_CH03_Memory", "PR_CH03_Farewell" } },
            // The lesson, montage and farewell share one CH04 presentation in replay mode.
            new GalleryEntry { presentationId = "PR_CH04_Farewell", chapterId = "CH04", sceneId = DuzheIds.SceneClassroom,
                presentationIds = new[] { "PR_CH04_Farewell" } },
            new GalleryEntry { presentationId = "PR_CH05_Reincarnation", chapterId = "CH05", sceneId = DuzheIds.SceneUnderworld,
                presentationIds = new[] { "PR_CH05_Recall", "PR_CH05_Reincarnation" } }
        };

        private GameObject panel;
        private Transform rowsRoot;
        private GameObject replayBar;
        private bool built;

        private string pendingId = "";
        private string pendingChapter = "";
        private string pendingScene = "";
        private string pendingRun = "";
        private bool starting;
        private GalleryEntry activeEntry;
        private int partIndex;
        private bool exitRequested;

        public bool IsShowing { get { return panel != null && panel.activeSelf; } }

        private bool sceneReadyHooked;

        private void Start()
        {
            GameSession session = GameSession.Instance;
            if (session != null)
            {
                session.RunReset += OnRunReset;
                session.OnGameEvent += OnGameEvent;
            }
            Presentation.PresentationPlayer.PlaybackFailed += OnPlaybackFailed;
            EnsureSceneHook();
        }

        /// The transition service may not exist yet when this component is
        /// built, so the scene-ready hook is (re)bound lazily.
        private void EnsureSceneHook()
        {
            if (sceneReadyHooked) return;
            GameSession session = GameSession.Instance;
            if (session == null || session.Transitions == null) return;
            session.Transitions.OnSceneReady += OnSceneReady;
            sceneReadyHooked = true;
        }

        private void OnDestroy()
        {
            GameSession session = GameSession.Instance;
            if (session != null)
            {
                session.RunReset -= OnRunReset;
                if (sceneReadyHooked && session.Transitions != null)
                {
                    session.Transitions.OnSceneReady -= OnSceneReady;
                }
                session.OnGameEvent -= OnGameEvent;
            }
            sceneReadyHooked = false;
            Presentation.PresentationPlayer.PlaybackFailed -= OnPlaybackFailed;
        }

        // --- list ---------------------------------------------------------

        private void Build()
        {
            if (built) return;
            built = true;

            panel = new GameObject("GalleryPanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);
            DuzheUiFactory.AddFullStretchImage(panel.transform, "Dim", new Color32(6, 5, 9, 226), true);
            DuzheUiFactory.AddLabel(panel.transform, "GalleryTitle",
                new Vector2(340f, -96f), new Vector2(600f, 48f), 34,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "回忆馆";

            GameObject rows = new GameObject("GalleryRows");
            rowsRoot = rows.AddComponent<RectTransform>();
            rows.transform.SetParent(panel.transform, false);
            DuzheUiFactory.MakeFullStretch(rows);

            DuzheUiFactory.AddButton(panel.transform, "GalleryBack",
                new Vector2(540f, -620f), new Vector2(200f, 48f), "返回", 24, () => Hide());
            panel.SetActive(false);
        }

        public void Show()
        {
            Build();
            GameSession session = GameSession.Instance;
            if (session == null || session.Save == null) return;
            ClearRows();

            float rowY = 210f;
            int count = 0;
            foreach (GalleryEntry entry in Entries)
            {
                if (!CanView(session.Save, entry.presentationId)) continue;
                string id = entry.presentationId; // captured per row
                DuzheUiFactory.AddButton(rowsRoot, "GalleryRow_" + id,
                    new Vector2(380f, -rowY), new Vector2(520f, 52f),
                    TitleOf(id), 24, () => Play(id));
                rowY += 62f;
                count++;
            }
            if (count == 0)
            {
                DuzheUiFactory.AddLabel(rowsRoot, "GalleryEmpty",
                    new Vector2(340f, -210f), new Vector2(600f, 40f), 24,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("gallery.empty", "通关后，这里会收进一段段回忆。");
            }

            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // --- playback exit affordance (C12) -------------------------------

        /// Small always-visible button while a recollection plays: the player
        /// must not depend on knowing that ESC exists.
        private void BuildReplayBar()
        {
            if (replayBar != null) return;
            replayBar = new GameObject("GalleryReplayBar");
            replayBar.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(replayBar);
            DuzheUiFactory.AddButton(replayBar.transform, "GalleryReplayExit",
                new Vector2(990f, -20f), new Vector2(250f, 44f), "返回回忆馆（ESC）", 18,
                LeaveReplay);
            replayBar.SetActive(false);
        }

        private void ShowReplayBar()
        {
            BuildReplayBar();
            replayBar.transform.SetAsLastSibling();
            replayBar.SetActive(true);
        }

        private void HideReplayBar()
        {
            if (replayBar != null) replayBar.SetActive(false);
        }

        /// Leaves the running memory and returns to the recollection list.
        /// Never writes progress (PresentationReplay mode).
        public void LeaveReplay()
        {
            GameSession session = GameSession.Instance;
            if (session == null) return;
            if (session.Transitions != null && session.Transitions.IsTransitioning)
            {
                exitRequested = true;
                return;
            }
            session.ReturnToMenu(MenuTab.Gallery);
        }

        // Completed legacy saves qualify without adding or migrating unlocks.
        // A montage alone never exposes a chapter's still-unseen ending.
        public static bool CanView(SaveService save, string id)
        {
            if (save == null) return false;
            foreach (GalleryEntry entry in Entries)
            {
                if (entry.presentationId == id)
                    return save.HasCompletedChapter(entry.chapterId) || save.HasUnlockedPresentation(id);
            }
            return false;
        }

        public static string TitleOf(string presentationId)
        {
            foreach (GalleryEntry chapterEntry in Entries)
            {
                if (chapterEntry.presentationId != presentationId) continue;
                Chapters.ChapterConfig chapter = Chapters.ChapterCatalog.GetChapter(chapterEntry.chapterId);
                if (chapter != null) return "第" + int.Parse(chapterEntry.chapterId.Substring(2)) + "关 · " + chapter.displayName;
            }
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            Dialogue.PresentationEntry entry;
            if (lib != null && lib.TryGetPresentation(presentationId, out entry) &&
                !string.IsNullOrEmpty(entry.galleryTitle))
            {
                return entry.galleryTitle;
            }
            return presentationId;
        }

        private void ClearRows()
        {
            if (rowsRoot == null) return;
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = rowsRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // --- replay -------------------------------------------------------

        /// Entry point for the start menu's gallery page (same guards).
        public void PlayFromMenu(string presentationId)
        {
            Play(presentationId);
        }

        private void Play(string presentationId)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Save == null || session.Transitions == null) return;
            if (starting || session.Transitions.IsTransitioning) return; // double click guard
            GalleryEntry entry = null;
            foreach (GalleryEntry e in Entries)
            {
                if (e.presentationId == presentationId) { entry = e; break; }
            }
            if (entry == null)
            {
                Debug.LogWarning("[Duzhe] Gallery: unknown presentation " + presentationId);
                return;
            }
            if (!CanView(session.Save, presentationId))
            {
                Debug.LogWarning("[Duzhe] Gallery: presentation not unlocked: " + presentationId);
                return;
            }

            // BeginNewRun fires RunReset (which clears any pending request),
            // so the pending fields are written AFTER it.
            if (session.Audio != null) session.Audio.PlaySfx("SFX01_UIConfirm");
            session.BeginNewRun(GameMode.PresentationReplay);
            EnsureSceneHook();
            pendingRun = session.RunId;
            activeEntry = entry;
            partIndex = 0;
            session.SetVisualChapter(entry.chapterId);
            if (session.Hud != null) session.Hud.HideQuestItems();
            Hide();
            QueuePart(true);
        }

        private void QueuePart(bool reloadScene)
        {
            GameSession session = GameSession.Instance;
            if (session == null || activeEntry == null || session.RunId != pendingRun) return;
            pendingId = activeEntry.presentationIds[partIndex];
            pendingChapter = activeEntry.chapterId;
            Dialogue.PresentationEntry part;
            if (Dialogue.DialogueLibrary.Instance == null ||
                !Dialogue.DialogueLibrary.Instance.TryGetPresentation(pendingId, out part))
            {
                OnPlaybackFailed("缺少回忆片段 " + pendingId);
                return;
            }
            pendingScene = part.sceneId;
            starting = true;
            ShowReplayBar();
            if (reloadScene || session.Transitions.CurrentSceneId != pendingScene)
                session.Transitions.LoadScene(pendingScene);
            else OnSceneReady(pendingScene);
        }

        private IEnumerator ContinueReplay(string runId)
        {
            // Let the previous presentation finish publishing its event before
            // starting another coroutine on the same presentation player.
            yield return null;
            GameSession session = GameSession.Instance;
            if (session == null || session.RunId != runId || activeEntry == null ||
                session.Mode != GameMode.PresentationReplay) yield break;
            if (exitRequested) { LeaveReplay(); yield break; }
            QueuePart(false);
        }

        /// The presentation starts only after the target scene is really ready
        /// (player placed, transition finished) and only once per request.
        private void OnSceneReady(string sceneId)
        {
            GameSession session = GameSession.Instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Duzhe] Gallery OnSceneReady scene=" + sceneId + " pending=" + pendingId +
                " mode=" + (session != null ? session.Mode.ToString() : "null"));
#endif
            if (session == null || string.IsNullOrEmpty(pendingId)) return;
            if (session.Mode != GameMode.PresentationReplay) return;
            if (session.RunId != pendingRun) return;
            if (sceneId != pendingScene) return;
            if (exitRequested) { LeaveReplay(); return; }
            Debug.Log("[Duzhe] Gallery: scene ready, starting " + pendingId + " (" + pendingChapter + ").");

            string id = pendingId;
            string chapter = pendingChapter;
            pendingId = "";
            pendingChapter = "";
            pendingScene = "";
            starting = false;

            if (session.Presentations == null ||
                !session.Presentations.Play(id, chapter))
            {
                Debug.LogError("[Duzhe] Gallery could not start " + id + "; returning to the list.");
                session.ReturnToMenu(MenuTab.Gallery);
                return;
            }
            ShowReplayBar();
        }

        private void OnGameEvent(GameEvent e)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Mode != GameMode.PresentationReplay) return;
            if (e.type != GameEventType.PresentationFinished) return;
            if (e.runId != session.RunId || e.runId != pendingRun) return;
            if (activeEntry == null || starting || e.chapterId != activeEntry.chapterId ||
                e.sourceId != activeEntry.presentationIds[partIndex]) return;
            partIndex++;
            starting = true; // ignore duplicate completion events
            if (partIndex < activeEntry.presentationIds.Length)
            {
                StartCoroutine(ContinueReplay(pendingRun));
                return;
            }
            // Return only after the entire chapter recollection has ended.
            HideReplayBar();
            session.ReturnToMenu(MenuTab.Gallery);
        }

        private void OnPlaybackFailed(string reason)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Mode != GameMode.PresentationReplay) return;
            if (session.Hud != null)
            {
                session.Hud.ShowToast("回忆播放失败：" + reason);
            }
            HideReplayBar();
            session.ReturnToMenu(MenuTab.Gallery);
        }

        private void OnRunReset()
        {
            StopAllCoroutines();
            activeEntry = null;
            partIndex = 0;
            exitRequested = false;
            pendingId = "";
            pendingChapter = "";
            pendingScene = "";
            pendingRun = "";
            starting = false;
            HideReplayBar();
            Hide();
        }
    }
}
