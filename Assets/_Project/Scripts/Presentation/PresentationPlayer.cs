using System.Collections;
using System.Collections.Generic;
using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Presentation
{
    /// Scene-side actor cast for one presentation. Assembled by
    /// ProjectSetupUtility; the player calls the two hooks around the
    /// dialogue phase. Visual notes from PresentationMap.json map onto these
    /// hooks per chapter (seating before, fade-out after).
    public abstract class PresentationCast : MonoBehaviour
    {
        [Tooltip("presentationId this cast performs.")]
        public string presentationId = "";

        /// Runs before the first dialogue line (seat the actors). The player
        /// waits for every matching cast to finish this coroutine.
        public virtual IEnumerator OnPresentationBegin(string presId) { yield break; }

        /// Runs after EACH dialogue segment finished, before the next one
        /// starts. Used by CH04 to reveal the written "再见" between the
        /// lesson and the farewell during a gallery replay.
        public virtual IEnumerator OnPresentationSegmentFinished(string presId, string segmentId) { yield break; }

        /// Runs after the last dialogue line (elder fade-out etc.). The
        /// PresentationFinished event is published once every cast finished.
        public virtual IEnumerator OnPresentationVisuals(string presId) { yield break; }
    }

    /// Plays a delivered presentation end to end: seating visuals -> the
    /// ordered dialogue segments (manual advance) -> post-dialogue visuals ->
    /// exactly one PresentationFinished event + gallery unlock (main run
    /// only). Mode-aware: replay runs never write any progress. Aborts
    /// silently if the run is abandoned (pause-menu leave, chapter switch).
    [DefaultExecutionOrder(-35)]
    public class PresentationPlayer : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        private string presentationId;
        private string[] activeSegmentIds = new string[0];
        private string runIdAtStart;
        /// Captured when Play() starts: the presentation never reads the
        /// "current" chapter/stage/mode again, so a chapter switch or a
        /// gallery replay cannot borrow the wrong context.
        private string contextChapterId = "";
        private string contextStageId = "";
        private GameMode contextMode = GameMode.MainMenu;
        private int segmentCursor;
        private bool waitingSegment;

        /// Raised when a presentation had to stop because of a real error
        /// (missing segment, failed start). The gallery uses it to return to
        /// its list instead of hanging on a black screen.
        public static event System.Action<string> PlaybackFailed;

        private static void NotifyFailed(string reason)
        {
            Debug.LogError("[Duzhe] Presentation failed: " + reason);
            if (PlaybackFailed != null) PlaybackFailed(reason);
        }

        private void Start()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent += HandleEvent;
        }

        private void OnDestroy()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent -= HandleEvent;
        }

        /// Starts the presentation. Returns false when it cannot start
        /// (unknown id, already playing). chapterIdOverride is for gallery
        /// replays outside a chapter run; empty = current chapter.
        public bool Play(string presId, string chapterIdOverride = "")
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[Duzhe] Presentation " + presId + " ignored: another presentation is playing.");
                return false;
            }
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            if (lib == null)
            {
                // Re-read after Load(): the first null check must not leave a
                // dead local behind (the old code did and skipped the lookup).
                Dialogue.DialogueLibrary.Load();
                lib = Dialogue.DialogueLibrary.Instance;
            }
            Dialogue.PresentationEntry entry;
            if (lib == null || !lib.TryGetPresentation(presId, out entry))
            {
                Debug.LogError("[Duzhe] Presentation not found in PresentationMap.json: " + presId);
                return false;
            }
            GameSession session = GameSession.Instance;
            if (session == null || session.Dialogue == null) return false;
            if (session.Dialogue.IsPlaying)
            {
                // Another segment is already running: do not "wait" for it by
                // mistake - that would complete this presentation on the
                // wrong DialogueFinished.
                Debug.LogWarning("[Duzhe] Presentation " + presId + " ignored: a dialogue is already playing.");
                return false;
            }

            // Capture the context BEFORE any coroutine starts.
            string chapterId = !string.IsNullOrEmpty(chapterIdOverride)
                ? chapterIdOverride
                : (session.Chapters != null ? session.Chapters.CurrentChapterId : "");
            if (string.IsNullOrEmpty(chapterId))
            {
                Debug.LogError("[Duzhe] Presentation " + presId + " has no chapter context.");
                return false;
            }
            GameMode mode = session.Mode;

            // The main run and a gallery replay must not share one playlist:
            // PR_CH04_Farewell keeps the lesson for the gallery, while the
            // main run has already played the class in its own stage.
            string[] segments = ResolveSegmentIds(entry, mode);
            if (segments.Length == 0)
            {
                Debug.LogError("[Duzhe] Presentation " + presId + " has no playable segments.");
                return false;
            }
            foreach (string segId in segments)
            {
                Dialogue.DialogueSegment seg;
                if (!lib.TryGetSegment(chapterId, segId, out seg))
                {
                    Debug.LogError("[Duzhe] Presentation " + presId + " aborted: segment missing: " + segId);
                    return false;
                }
            }

            presentationId = presId;
            activeSegmentIds = segments;
            contextChapterId = chapterId;
            contextMode = mode;
            contextStageId = mode == GameMode.PresentationReplay
                ? "Gallery/" + presId
                : (session.Chapters != null ? session.Chapters.CurrentStageId : "");
            runIdAtStart = session.RunId;
            IsPlaying = true;
            segmentCursor = 0;
            waitingSegment = false;
            session.SetInputMode(InputMode.Ritual); // locked during seating visuals
            StartCoroutine(RunRoutine(entry));
            return true;
        }

        /// Which dialogue segments this run plays. Only the CH04 farewell has
        /// a different main-run list (the class is its own stage there); a
        /// presentation replay plays the full delivered order.
        private static string[] ResolveSegmentIds(Dialogue.PresentationEntry entry, GameMode mode)
        {
            if (entry.presentationId == "PR_CH05_Recall" && mode == GameMode.PresentationReplay)
                return new[] { "CH05_Recall", "CH05_LetGo" };
            if (entry.presentationId == "PR_CH04_Farewell" && mode != GameMode.PresentationReplay)
            {
                return new[] { "CH04_Farewell" };
            }
            return entry.orderedSegmentIds == null
                ? new string[0]
                : (string[])entry.orderedSegmentIds.Clone();
        }

        /// Hard stop used by the pause-menu leave / run reset (no events, no
        /// unlock). Also clears the segment wait so a later event cannot
        /// resume a dead presentation.
        public void ForceAbort()
        {
            // Run-owned overlays must not leak into the menu.
            PresentationCG.Hide();
            BlackboardTextLayer.Hide();
            waitingSegment = false;
            if (!IsPlaying) return;
            IsPlaying = false;
            StopAllCoroutines();
        }

        private IEnumerator RunRoutine(Dialogue.PresentationEntry entry)
        {
            GameSession session = GameSession.Instance;
            List<PresentationCast> casts = CollectCasts(entry.presentationId);

            // 1) Seating phase (all casts in parallel).
            session.SetInputMode(InputMode.Ritual);
            List<Coroutine> begins = new List<Coroutine>();
            foreach (PresentationCast cast in casts)
            {
                begins.Add(StartCoroutine(cast.OnPresentationBegin(entry.presentationId)));
            }
            foreach (Coroutine c in begins) yield return c;
            if (Aborted()) yield break;

            // 2) Dialogue phase: each segment manually advanced.
            for (segmentCursor = 0; segmentCursor < activeSegmentIds.Length; segmentCursor++)
            {
                string segmentId = activeSegmentIds[segmentCursor];
                if (Aborted()) yield break;
                bool mainRun = contextMode == GameMode.MainRun && session.Chapters != null &&
                               !string.IsNullOrEmpty(contextChapterId);
                if (mainRun) session.Chapters.MarkSegmentPlayed(segmentId);

                // Arm the wait BEFORE starting the segment: a very short
                // segment may publish DialogueFinished synchronously.
                waitingSegment = true;
                session.Dialogue.StartSegment(contextChapterId, segmentId);
                if (!session.Dialogue.IsPlaying)
                {
                    // StartSegment failed (missing segment): abort WITHOUT a
                    // completion event - missing critical dialogue must never
                    // silently complete a chapter (spec section 10).
                    waitingSegment = false;
                    IsPlaying = false;
                    session.SetInputMode(contextMode == GameMode.PresentationReplay
                        ? InputMode.Gallery : InputMode.Exploring);
                    NotifyFailed(entry.presentationId + " / " + segmentId + " (segment missing)");
                    yield break;
                }
                while (waitingSegment)
                {
                    if (Aborted()) yield break;
                    yield return null;
                }

                // Between segments: casts may reveal something (CH04 gallery
                // replay shows the written "再见" after the lesson).
                if (Aborted()) yield break;
                session.SetInputMode(InputMode.Ritual);
                foreach (PresentationCast cast in casts)
                {
                    if (cast == null) continue;
                    yield return StartCoroutine(
                        cast.OnPresentationSegmentFinished(entry.presentationId, segmentId));
                    if (Aborted()) yield break;
                }
            }

            // 3) Post-dialogue visuals (elder fade etc.).
            session.SetInputMode(InputMode.Ritual);
            List<Coroutine> visuals = new List<Coroutine>();
            foreach (PresentationCast cast in casts)
            {
                visuals.Add(StartCoroutine(cast.OnPresentationVisuals(entry.presentationId)));
            }
            foreach (Coroutine c in visuals) yield return c;
            if (Aborted()) yield break;

            // 4) Done: restore input, unlock the gallery record only for a
            //    main run, publish exactly one PresentationFinished with the
            //    CAPTURED context (never the current chapter).
            IsPlaying = false;
            session.SetInputMode(contextMode == GameMode.PresentationReplay
                ? InputMode.Gallery : InputMode.Exploring);
            if (contextMode == GameMode.MainRun && session.Mode == GameMode.MainRun &&
                session.Save != null)
            {
                if (session.Save.UnlockPresentation(entry.presentationId))
                {
                    if (session.Hud != null)
                    {
                        session.Hud.ShowToast(Duzhe.UI.UiText.Get("feedback.gallery_unlocked", "一段回忆已收进回忆馆。"));
                    }
                }
            }
            session.PublishEvent(new GameEvent(GameEventType.PresentationFinished,
                contextChapterId, contextStageId, entry.presentationId, runIdAtStart));
            Debug.Log("[Duzhe] Presentation finished: " + entry.presentationId +
                " chapter=" + contextChapterId + " stage=" + contextStageId);
        }

        private bool Aborted()
        {
            GameSession session = GameSession.Instance;
            if (!IsPlaying) return true;
            if (session == null || session.RunId != runIdAtStart || session.Mode != contextMode)
            {
                IsPlaying = false;
                waitingSegment = false;
                return true;
            }
            return false;
        }

        private void HandleEvent(GameEvent e)
        {
            if (!IsPlaying || !waitingSegment) return;
            GameSession session = GameSession.Instance;
            if (session == null) return;
            if (e.type != GameEventType.DialogueFinished) return;
            // Run + captured chapter + the segment we are waiting for. The
            // dialogue's own stageId is its real stage (e.g. "CH05_Recall"),
            // so it is deliberately NOT compared with the synthetic
            // "Gallery/..." stage id.
            if (session.RunId != runIdAtStart || e.runId != runIdAtStart) return;
            if (e.chapterId != contextChapterId) return;
            if (e.sourceId != CurrentSegmentId()) return;
            waitingSegment = false;
        }

        private string CurrentSegmentId()
        {
            if (activeSegmentIds == null) return "";
            if (segmentCursor < 0 || segmentCursor >= activeSegmentIds.Length) return "";
            return activeSegmentIds[segmentCursor];
        }

        private static List<PresentationCast> CollectCasts(string presId)
        {
            List<PresentationCast> result = new List<PresentationCast>();
            PresentationCast[] all = FindObjectsOfType<PresentationCast>();
            foreach (PresentationCast cast in all)
            {
                if (cast != null && cast.presentationId == presId) result.Add(cast);
            }
            return result;
        }
    }
}
