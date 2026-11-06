using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Chapters
{
    /// THE single owner of chapter progression. Validates entry conditions,
    /// consumes completion events and advances stages. Idempotency: after a
    /// stage advances, its stageId no longer matches, so repeated or stale
    /// events are dropped by the filters below. Only this class may mark a
    /// chapter completed. Main run and replays are strictly separated:
    /// replays never write main-run progress.
    [DefaultExecutionOrder(-40)]
    public class ChapterDirector : MonoBehaviour
    {
        /// The active chapter id for route-visibility gates. Set on chapter
        /// start, cleared when the run is abandoned.
        public static string ActiveChapterId { get; private set; } = "";
        /// Fired when the active chapter changes (payload = new chapterId).
        public static event System.Action<string> ChapterChanged;

        private static void SetActiveChapterId(string chapterId)
        {
            ActiveChapterId = chapterId ?? "";
            if (ChapterChanged != null) ChapterChanged(ActiveChapterId);
        }

        public string CurrentChapterId { get; private set; } = "";
        public string CurrentStageId
        {
            get
            {
                if (config == null || runtime == null || runtime.stageIndex < 0 ||
                    runtime.stageIndex >= config.stages.Length) return "";
                return config.stages[runtime.stageIndex].stageId;
            }
        }

        /// CH04 gate: true only after the complete classroom lesson dialogue
        /// ended in this run (runtime flag, never persisted).
        public bool LessonFinished { get { return runtime != null && runtime.lessonFinished; } }

        /// CH05: recall presentation finished / "let go" dialogue finished.
        public bool MemoryRestored { get { return runtime != null && runtime.memoryRestored; } }
        public bool LetGoFinished { get { return runtime != null && runtime.letGoFinished; } }

        private Core.GameSession session;
        private ChapterConfig config;
        private ChapterRuntimeState runtime;
        private int pendingStageIndex = -1;
        private float autoAdvanceRemaining;
        private bool autoAdvanceRunning;

        private void Start()
        {
            session = Core.GameSession.Instance;
            if (session != null) session.OnGameEvent += HandleEvent;
            if (session != null && session.Transitions != null)
                session.Transitions.OnSceneReady += OnSceneReady;
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.OnGameEvent -= HandleEvent;
                if (session.Transitions != null) session.Transitions.OnSceneReady -= OnSceneReady;
            }
        }

        /// Begins a chapter in the given run mode.
        public void StartChapter(string chapterId, Core.GameMode mode)
        {
            if (session == null) session = Core.GameSession.Instance;
            ChapterConfig cfg = ChapterCatalog.GetChapter(chapterId);
            if (cfg == null)
            {
                Debug.LogError("[Duzhe] StartChapter: unknown chapterId '" + chapterId + "'.");
                return;
            }
            if (session == null || session.Transitions == null)
            {
                Debug.LogError("[Duzhe] StartChapter: GameSession/Transitions missing.");
                return;
            }
            if (session.Transitions.IsTransitioning)
            {
                Debug.LogWarning("[Duzhe] StartChapter ignored: a scene transition is already running.");
                return;
            }

            // A new chapter is a new run: renew RunId and tear down anything
            // the previous run left behind BEFORE the new state is built.
            session.BeginNewRun(mode);
            config = cfg;
            runtime = new ChapterRuntimeState { chapterId = chapterId };
            // One river seed per run: retries reuse it, replays get a new one.
            runtime.riverSeed = UnityEngine.Random.Range(1, int.MaxValue);
            CurrentChapterId = chapterId;
            SetActiveChapterId(chapterId);
            session.SetVisualChapter(chapterId);
            session.Chapters = this;
            EnterStage(0);
        }

        /// First unfinished chapter of the main chain; when everything is
        /// finished the first chapter is returned (replay-style re-run keeps
        /// all unlocks, spec section 9).
        public string GetFirstUnfinishedChapterId()
        {
            return ChapterCatalog.GetFirstUnfinishedChapterId(session != null ? session.Save : null);
        }

        /// Entry-point helper used by the main menu "start" button.
        public void StartFirstUnfinished(Core.GameMode fallbackModeWhenAllDone)
        {
            string id = GetFirstUnfinishedChapterId();
            bool allDone = session.Save != null && session.Save.HasCompletedChapter(id);
            StartChapter(id, allDone ? fallbackModeWhenAllDone : Core.GameMode.MainRun);
        }

        // --- stage machine -------------------------------------------------

        private void EnterStage(int index)
        {
            if (config == null || index < 0 || index >= config.stages.Length)
            {
                Debug.LogError("[Duzhe] EnterStage: invalid index " + index + " for chapter " + CurrentChapterId);
                return;
            }
            StageDefinition stage = config.stages[index];

            // Explicit entry conditions: required items must already exist in
            // this run's state (chain order normally guarantees this).
            if (runtime != null && stage.requiredItemIds != null)
            {
                foreach (string itemId in stage.requiredItemIds)
                {
                    if (!runtime.HasItemCollected(itemId))
                    {
                        Debug.LogError("[Duzhe] Stage " + stage.stageId +
                            " requires item " + itemId + " which is not collected; stage blocked (config error).");
                        return;
                    }
                }
            }

            // CH04: the "write goodbye" stage must not run before the whole
            // classroom lesson finished. This is a real gate, not a visual
            // hint - jumping here without the lesson would skip the class.
            if (runtime != null && stage.stageId == "CH04_WriteGoodbye" && !runtime.lessonFinished)
            {
                Debug.LogError("[Duzhe] Stage CH04_WriteGoodbye blocked: the lesson (CH04_Lesson) has not finished.");
                return;
            }

            // CH05: boarding is only legal after the recall AND the "let go"
            // dialogue. Report the config error instead of auto-filling flags.
            if (runtime != null && stage.stageId == "CH05_Board" &&
                (!runtime.memoryRestored || !runtime.letGoFinished))
            {
                Debug.LogError("[Duzhe] Stage CH05_Board blocked: memoryRestored=" + runtime.memoryRestored +
                    " letGoFinished=" + runtime.letGoFinished + " (config/flow error).");
                return;
            }

            runtime.stageIndex = index;
            runtime.stageConditionKeys.Clear();
            autoAdvanceRunning = false;
            autoAdvanceRemaining = 0f;
            string currentScene = session.Transitions != null ? session.Transitions.CurrentSceneId : "";
            if (!string.IsNullOrEmpty(stage.sceneId) && stage.sceneId != currentScene)
            {
                pendingStageIndex = index;
                session.Transitions.LoadScene(stage.sceneId, config.spawnPointId);
            }
            else
            {
                FinishEnterStage(index);
            }
        }

        private void OnSceneReady(string sceneId)
        {
            if (pendingStageIndex < 0) return;
            int idx = pendingStageIndex;
            // Only the scene this stage asked for may finish the entry; an
            // unrelated Ready (menu return, other load) must not advance.
            if (config != null && idx >= 0 && idx < config.stages.Length)
            {
                StageDefinition waiting = config.stages[idx];
                if (!string.IsNullOrEmpty(waiting.sceneId) && waiting.sceneId != sceneId)
                {
                    Debug.LogWarning("[Duzhe] Scene ready '" + sceneId + "' ignored: stage " +
                        waiting.stageId + " waits for '" + waiting.sceneId + "'.");
                    return;
                }
            }
            pendingStageIndex = -1;
            FinishEnterStage(idx);
        }

        private void FinishEnterStage(int index)
        {
            StageDefinition stage = config.stages[index];
            session.SetInputMode(stage.inputMode);

            string objective = stage.objectiveText;
            Dialogue.ChapterDialogue ch;
            if (Dialogue.DialogueLibrary.Instance != null &&
                Dialogue.DialogueLibrary.Instance.TryGetChapter(config.dialogueChapterId, out ch))
            {
                objective = Dialogue.DialogueLibrary.Instance.GetObjectiveText(
                    config.dialogueChapterId, stage.stageId, stage.objectiveText);
            }
            if (session.Hud != null) session.Hud.SetObjective(objective);

            // Optional one-shot entry hint (first-stage movement tutorial etc.).
            if (!string.IsNullOrEmpty(stage.entryHintKey) && session.Hud != null)
            {
                session.Hud.ShowToast(Duzhe.UI.UiText.Get(stage.entryHintKey, ""));
            }

            // Optional auto-start dialogue segment for this stage (used by
            // later chapters; empty by default).
            if (!string.IsNullOrEmpty(stage.autoStartSegmentId) && session.Dialogue != null &&
                !session.Dialogue.IsPlaying)
            {
                MarkSegmentPlayed(stage.autoStartSegmentId);
                session.Dialogue.StartSegment(config.dialogueChapterId, stage.autoStartSegmentId);
            }

            // Optional auto-start presentation (C04: PR_CH01_Farewell).
            if (!string.IsNullOrEmpty(stage.autoStartPresentationId))
            {
                Presentation.PresentationPlayer player = session.Presentations;
                if (player != null) player.Play(stage.autoStartPresentationId);
                else Debug.LogError("[Duzhe] Stage " + stage.stageId + " wants presentation " +
                    stage.autoStartPresentationId + " but no PresentationPlayer exists.");
            }

            // CH05_Complete: the chapter ends only when the player confirms
            // the credits (CreditsConfirmed). No auto-advance here.
            if (stage.stageId == "CH05_Complete")
            {
                if (session.Credits != null) session.Credits.Show();
                else Debug.LogError("[Duzhe] CH05_Complete wants the credits panel but GameSession.Credits is missing.");
            }

            // Auto-advance stage (C04: CH01_Complete closing fade). The fade
            // duration equals the advance delay; both freeze while paused.
            if (stage.autoAdvanceSeconds > 0f)
            {
                autoAdvanceRemaining = stage.autoAdvanceSeconds;
                autoAdvanceRunning = true;
                Duzhe.UI.FadeOverlay.Begin(stage.autoAdvanceSeconds);
            }
        }

        private void Update()
        {
            if (!autoAdvanceRunning) return;
            Core.GameSession s = session;
            if (s == null || s.Paused) return; // frozen with the pause overlay
            autoAdvanceRemaining -= Time.deltaTime;
            if (autoAdvanceRemaining > 0f) return;
            autoAdvanceRunning = false;
            if (runtime == null || config == null) return;
            int index = runtime.stageIndex;
            if (index < 0 || index >= config.stages.Length) return;
            if (config.stages[index].stageId != CurrentStageId) return; // already moved on
            AdvanceFrom(index);
        }

        // --- event handling ------------------------------------------------

        private void HandleEvent(Core.GameEvent e)
        {
            if (session == null || runtime == null || config == null) return;
            if (e.runId != session.RunId) return;               // stale run callback
            if (e.chapterId != CurrentChapterId) return;        // other/old chapter
            if (runtime.chapterCompleted) return;               // chapter already done

            string stageId = CurrentStageId;
            if (e.stageId != stageId) return;                    // stale stage event (idempotency)

            int index = runtime.stageIndex;
            StageDefinition stage = config.stages[index];

            // Multi-condition stage: EVERY condition must fire (any order);
            // non-matching events are dropped, matching ones are recorded once.
            if (stage.requireAll != null && stage.requireAll.Length > 0)
            {
                foreach (Chapters.StageCondition cond in stage.requireAll)
                {
                    if (cond == null || cond.type != e.type) continue;
                    if (!string.IsNullOrEmpty(cond.sourceId) && cond.sourceId != e.sourceId) continue;

                    string key = cond.type + "#" + cond.sourceId;
                    if (runtime.stageConditionKeys.Add(key))
                    {
                        if (e.type == Core.GameEventType.MinigameCompleted)
                        {
                            MarkMinigameCompleted(e.sourceId);
                        }
                        if (!string.IsNullOrEmpty(cond.grantsItemId))
                        {
                            MarkItemCollected(cond.grantsItemId);
                            if (session.Hud != null) session.Hud.ShowQuestItem(cond.grantsItemId);
                        }
                    }
                    if (AllConditionsMet(stage))
                    {
                        AdvanceFrom(index);
                    }
                    return; // the event belonged to this stage's conditions
                }
                return; // unrelated event type for a requireAll stage
            }

            if (e.type != stage.completionEvent) return;        // not the completion event
            if (!string.IsNullOrEmpty(stage.expectedSourceId) && e.sourceId != stage.expectedSourceId) return;

            if (e.type == Core.GameEventType.MinigameCompleted)
            {
                MarkMinigameCompleted(e.sourceId);
            }

            // CH04: the "lesson finished" flag is set ONLY by the completed
            // classroom lesson segment (never by opening a dialogue, showing
            // its last line or entering the room).
            if (CurrentChapterId == "CH04" && stage.stageId == "CH04_Lesson" &&
                e.type == Core.GameEventType.DialogueFinished && e.sourceId == "CH04_Lesson")
            {
                runtime.lessonFinished = true;
            }

            // CH05: the recall presentation and the "let go" dialogue set
            // their flags only on their OWN completion events (a river retry
            // never touches them).
            if (CurrentChapterId == "CH05")
            {
                if (stage.stageId == "CH05_Recall") runtime.memoryRestored = true;
                if (stage.stageId == "CH05_LetGo") runtime.letGoFinished = true;
            }

            AdvanceFrom(index);
        }

        private bool AllConditionsMet(StageDefinition stage)
        {
            foreach (Chapters.StageCondition cond in stage.requireAll)
            {
                if (cond == null) continue;
                if (!runtime.stageConditionKeys.Contains(cond.type + "#" + cond.sourceId)) return false;
            }
            return true;
        }

        private void AdvanceFrom(int index)
        {
            StageDefinition stage = config.stages[index];
            if (string.IsNullOrEmpty(stage.nextStageId))
            {
                CompleteChapter();
            }
            else
            {
                int next = IndexOfStage(stage.nextStageId);
                if (next < 0)
                {
                    Debug.LogError("[Duzhe] Stage " + stage.stageId + " points at unknown next stage " +
                        stage.nextStageId + "; progression halted.");
                    return;
                }
                EnterStage(next);
            }
        }

        private int IndexOfStage(string stageId)
        {
            for (int i = 0; i < config.stages.Length; i++)
            {
                if (config.stages[i].stageId == stageId) return i;
            }
            return -1;
        }

        private void CompleteChapter()
        {
            if (runtime == null || runtime.chapterCompleted || session == null) return;
            runtime.chapterCompleted = true;
            string chapterId = CurrentChapterId;
            string stageId = CurrentStageId;
            Core.GameMode mode = session.Mode;

            if (mode == Core.GameMode.MainRun && session.Save != null)
            {
                // Idempotent by SaveService contract.
                session.Save.MarkChapterCompleted(chapterId);
            }
            session.PublishEvent(Core.GameEventType.ChapterCompleted, chapterId, stageId, chapterId);

            // Both first playthrough and replay continue through the complete campaign.
            bool continueCampaign = mode == Core.GameMode.MainRun ||
                mode == Core.GameMode.ChapterReplay;
            if (continueCampaign)
            {
                string next = GetNextFormalChapterId(chapterId);
                if (next != null)
                {
                    if (ChapterCatalog.GetChapter(next) == null)
                    {
                        Debug.LogError("[Duzhe] Missing next formal chapter: " + next +
                            " (chapter flow halted).");
                        return;
                    }
                    Debug.Log("[Duzhe] Chapter " + chapterId + " complete -> continuing to " + next + ".");
                    if (session.Hud != null) session.Hud.HideQuestItems();
                    StartChapter(next, mode);
                    return;
                }
            }

            if (session.Hud != null) session.Hud.ShowToast(
                Duzhe.UI.UiText.Get("feedback.chapter_complete", "这一程，已经完成。"));
            Core.MenuTab tab = Core.MenuTab.Home;
            if (!session.ReturnToMenu(tab))
            {
                // A transition was already running: do not pretend the menu
                // loaded. Log it so the state is recoverable.
                Debug.LogWarning("[Duzhe] Chapter " + chapterId +
                    " completed, but returning to the menu was refused (transition in progress).");
            }
        }

        /// The formal chapter after the given one, by the FROZEN formal order
        /// (never the DEV-including run order). Null at the end of the chain.
        private static string GetNextFormalChapterId(string current)
        {
            string[] order = ChapterCatalog.FormalChapterIds;
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == current)
                {
                    return i + 1 < order.Length ? order[i + 1] : null;
                }
            }
            return null;
        }

        /// Leaves the current run without touching any save data.
        public void AbandonCurrentRun()
        {
            runtime = null;
            config = null;
            CurrentChapterId = "";
            pendingStageIndex = -1;
            autoAdvanceRunning = false;
            SetActiveChapterId("");
            if (session != null && session.Hud != null) session.Hud.HideQuestItems();
        }

        // --- run-state queries used by interaction & dialogue -------------

        /// River seed of the current run (reused on retries, refreshed on
        /// chapter replays). 0 when no chapter is running.
        public int CurrentRiverSeed
        {
            get { return runtime != null ? runtime.riverSeed : 0; }
        }

        public bool HasItemCollected(string itemId)
        {
            return runtime != null && runtime.HasItemCollected(itemId);
        }

        public void MarkItemCollected(string itemId)
        {
            if (runtime == null || string.IsNullOrEmpty(itemId)) return;
            runtime.collectedItemIds.Add(itemId);
        }

        public void MarkItemReturned(string itemId)
        {
            if (runtime == null || string.IsNullOrEmpty(itemId)) return;
            runtime.returnedItemIds.Add(itemId);
        }

        public void MarkMinigameCompleted(string minigameId)
        {
            if (runtime == null || string.IsNullOrEmpty(minigameId)) return;
            runtime.completedMinigameIds.Add(minigameId);
        }

        public void MarkSegmentPlayed(string segmentId)
        {
            if (runtime == null || string.IsNullOrEmpty(segmentId)) return;
            runtime.playedSegmentIds.Add(segmentId);
        }

        /// Auto-talk permission: segment must belong to the current stage and
        /// must not have played yet this run ("no infinite re-popup").
        public bool CanAutoStartSegment(string segmentId)
        {
            if (runtime == null || string.IsNullOrEmpty(segmentId)) return false;
            if (runtime.HasSegmentPlayed(segmentId)) return false;
            Dialogue.DialogueSegment seg;
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            if (lib == null || !lib.TryGetSegment(config != null ? config.dialogueChapterId : "", segmentId, out seg))
                return false;
            return seg.stageId == CurrentStageId;
        }

        /// Same gate for hand-over segments started by E.
        public bool CanStartReturnSegment(string segmentId)
        {
            return CanAutoStartSegment(segmentId);
        }

        // --- development entry (never present in shipped menus) -------------

        public void DevMarkChapterCompleted()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (runtime == null || config == null)
            {
                Debug.LogWarning("[Duzhe] DEV complete: no chapter is running.");
                return;
            }
            CompleteChapter();
#endif
        }
    }
}
