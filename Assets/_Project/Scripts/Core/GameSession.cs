using System;
using UnityEngine;

namespace Duzhe.Core
{
    /// The one and only persistent gameplay session. Created by GameBootstrap,
    /// never duplicated (bootstrap guards by object name). Owns the runId used
    /// to reject stale events, the current GameMode (main/replay separated),
    /// the current InputMode plus the pause overlay, and the event bus.
    [DefaultExecutionOrder(-100)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        /// Unique per run; every GameEvent carries it and consumers drop
        /// events from other runs (old scene callbacks etc.).
        public string RunId { get; private set; }

        public GameMode Mode { get; private set; } = GameMode.MainMenu;

        public InputMode InputMode { get; private set; } = InputMode.Menu;

        /// Pause overlay flag. While true the previous InputMode is stored in
        /// pausedReturnMode and all gameplay input is blocked by InputRouter.
        public bool Paused { get; private set; }
        private InputMode pausedReturnMode = InputMode.Exploring;

        public event Action<GameEvent> OnGameEvent;

        // --- service references (wired by GameBootstrap) ---
        [NonSerialized] public Dialogue.DialogueRunner Dialogue;
        [NonSerialized] public Dialogue.DialogueView DialogueView;
        [NonSerialized] public Audio.AudioService Audio;
        [NonSerialized] public UI.ObjectiveHud Hud;
        [NonSerialized] public UI.PauseMenu Pause;
        [NonSerialized] public UI.MainMenuPanel MainMenu;
        [NonSerialized] public UI.CreditsPanel Credits;
        [NonSerialized] public UI.GalleryController Gallery;
        [NonSerialized] public UI.ChapterMenuPanel ChapterMenu;
        [NonSerialized] public Chapters.ChapterDirector Chapters;
        [NonSerialized] public Minigames.MinigameHost Minigames;
        [NonSerialized] public Presentation.PresentationPlayer Presentations;
        [NonSerialized] public SceneTransitionService Transitions;
        [NonSerialized] public SaveService Save;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RunId = Guid.NewGuid().ToString("N");
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PublishEvent(GameEvent e)
        {
            if (OnGameEvent != null) OnGameEvent(e);
        }

        public void PublishEvent(GameEventType type, string chapterId, string stageId, string sourceId)
        {
            PublishEvent(new GameEvent(type, chapterId, stageId, sourceId, RunId));
        }

        public void SetMode(GameMode mode)
        {
            Mode = mode;
        }

        // --- C09: run boundaries, menu routing, display chapter -------------

        /// Menu page to open next (set by ReturnToMenu, consumed by the menu).
        public MenuTab PendingMenuTab { get; private set; } = MenuTab.Home;

        /// Fired after a run was torn down (RunId already renewed). UI that
        /// holds run-owned state subscribes to close it without publishing
        /// any completion event.
        public event System.Action RunReset;

        /// Chapter id used ONLY to decide which route objects are visible.
        /// Never a substitute for ChapterDirector.CurrentChapterId.
        public string VisualChapterId { get; private set; } = "";

        /// Starts a brand-new run: invalidates every old callback by renewing
        /// RunId first, then tears down run-owned UI/state, and finally sets
        /// the requested mode. River retries never call this.
        public void BeginNewRun(GameMode mode)
        {
            RunId = System.Guid.NewGuid().ToString("N");
            if (Pause != null && Pause.IsOpen) Pause.Close();
            SetPaused(false);
            if (Minigames != null) Minigames.Close();
            if (Presentations != null) Presentations.ForceAbort();
            if (Dialogue != null) Dialogue.ForceStop();
            if (Chapters != null) Chapters.AbandonCurrentRun();
            if (Credits != null) Credits.HideWithoutEvent();
            // C12: the chapter list is an overlay too - a run teardown must
            // never leave it (or its dim) on top of the next screen.
            if (ChapterMenu != null) ChapterMenu.Hide();
            UI.FadeOverlay.Cancel();
            SetVisualChapter("");
            Presentation.PresentationCG.Hide();
            Presentation.BlackboardTextLayer.Hide();
            if (RunReset != null) RunReset();
            SetMode(mode);
            SetInputMode(InputMode.Transition);
        }

        /// Tears the current run down and returns to the start menu on the
        /// requested page. Returns false while another transition is running.
        public bool ReturnToMenu(MenuTab tab)
        {
            if (Transitions == null || Transitions.IsTransitioning) return false;
            PendingMenuTab = tab;
            BeginNewRun(GameMode.MainMenu);
            Transitions.LoadScene(DuzheIds.SceneMainMenu);
            return true;
        }

        public void SetPendingMenuTab(MenuTab tab)
        {
            PendingMenuTab = tab;
        }

        /// Applies the display chapter to every route gate in the ACTIVE
        /// scene (including inactive objects) so a same-scene chapter switch
        /// always shows the right actors.
        public void SetVisualChapter(string chapterId)
        {
            VisualChapterId = chapterId ?? "";
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (UnityEngine.GameObject root in scene.GetRootGameObjects())
            {
                foreach (Chapters.ChapterObjectVisibility gate in root.GetComponentsInChildren<
                    Chapters.ChapterObjectVisibility>(true))
                {
                    gate.RefreshVisibility();
                }
            }
        }

        public void SetInputMode(InputMode mode)
        {
            if (InputMode == mode) return;
            InputMode = mode;
            // Mode switches swallow the triggering press so it cannot leak
            // into the new mode (e.g. the click that closed a dialogue must
            // not also interact with the world on the same frame).
            if (InputRouter.Instance != null) InputRouter.Instance.ConsumePendingPresses();
        }

        public void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            if (paused)
            {
                pausedReturnMode = InputMode;
                Time.timeScale = 0f;
                if (Audio != null) Audio.SetGameplayPaused(true);
            }
            else
            {
                InputMode = pausedReturnMode;
                Time.timeScale = 1f;
                if (Audio != null) Audio.SetGameplayPaused(false);
                // The ESC press that closes the pause menu must be consumed.
                if (InputRouter.Instance != null) InputRouter.Instance.ConsumePendingPresses();
            }
        }
    }
}
