using System;
using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Duzhe.Minigames
{
    /// Contract implemented by every minigame panel (one MonoBehaviour per
    /// minigame type). The host drives pause notifications; each panel builds
    /// and tears down its own UI under the persistent canvas.
    public interface IMinigamePanel
    {
        MinigameConfig Config { get; }
        GameObject PanelGO { get; }
        /// The pause overlay opened over this minigame: unfinished drag/stroke
        /// work is restored per spec (dragged object back to origin, current
        /// stroke cleared), finished work is kept.
        void OnGamePaused();
    }

    /// The single gatekeeper for minigame panels: opens one panel at a time
    /// (stage authorisation happens upstream via Interactable.activeStageIds
    /// or the dev tester), locks input into MiniGame mode, publishes exactly
    /// one MinigameCompleted event per session and closes cleanly on any
    /// exit path.
    [DefaultExecutionOrder(-60)]
    public class MinigameHost : MonoBehaviour
    {
        public static MinigameHost Instance { get; private set; }

        public bool IsOpen { get; private set; }

        private IMinigamePanel activePanel;
        private MinigameConfig activeConfig;
        private bool completedFired;
        private bool lastPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(MinigameConfig cfg)
        {
            if (cfg == null)
            {
                Debug.LogError("[Duzhe] MinigameHost.Open: null config.");
                return;
            }
            if (IsOpen)
            {
                Debug.LogWarning("[Duzhe] MinigameHost.Open ignored: '" + activeConfig.minigameId + "' is already open.");
                return;
            }
            GameSession session = GameSession.Instance;
            if (session == null) return;

            completedFired = false;
            activeConfig = cfg;
            activePanel = CreatePanel(cfg);
            if (activePanel == null)
            {
                Debug.LogError("[Duzhe] No panel implementation for type " + cfg.type);
                return;
            }
            IsOpen = true;
            lastPaused = session.Paused;
            session.SetInputMode(InputMode.MiniGame);
            Debug.Log("[Duzhe] Minigame open: " + cfg.minigameId);
        }

        /// Creates the panel GameObject under the persistent canvas (full
        /// stretch, bottom of the sibling stack) and puts the panel COMPONENT
        /// ON THE SAME OBJECT - UGUI pointer/drag events bubble up the
        /// hierarchy, so a handler on GameRoot would never receive them.
        private IMinigamePanel CreatePanel(MinigameConfig cfg)
        {
            GameObject panelGo = new GameObject("MinigamePanel_" + cfg.minigameId);
            RectTransform rt = panelGo.AddComponent<RectTransform>(); // explicit
            panelGo.transform.SetParent(UiCanvasTransformSafe(), false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f); // top-left pivot = anchor, for snap math
            panelGo.transform.SetAsFirstSibling(); // below pause/menus

            MinigamePanelBase panel;
            switch (cfg.type)
            {
                case MinigameType.Trace: panel = panelGo.AddComponent<TraceMinigame>(); break;
                case MinigameType.Drag: panel = panelGo.AddComponent<DragPlacementMinigame>(); break;
                case MinigameType.Door: panel = panelGo.AddComponent<DoorKnockMinigame>(); break;
                case MinigameType.Notes: panel = panelGo.AddComponent<NoteSequenceMinigame>(); break;
                default:
                    Destroy(panelGo);
                    return null;
            }
            return panel.Init(cfg, this);
        }

        /// Panels report success through this; publishes one MinigameCompleted
        /// event then closes. Event carries runId/stageId/sourceId per
        /// contract; ChapterDirector's stage filters decide whether it
        /// advances anything (the dev tester's events do not).
        public void Complete()
        {
            if (!IsOpen || completedFired) return;
            completedFired = true;
            GameSession session = GameSession.Instance;
            string minigameId = activeConfig != null ? activeConfig.minigameId : "";
            if (session.Hud != null)
            {
                session.Hud.ShowToast(Duzhe.UI.UiText.Get("feedback.puzzle_complete", "完成了。"));
            }
            if (session.Audio != null) session.Audio.PlaySfx("SFX12_PuzzleSuccess");
            session.PublishEvent(GameEventType.MinigameCompleted,
                session.Chapters != null ? session.Chapters.CurrentChapterId : "",
                session.Chapters != null ? session.Chapters.CurrentStageId : "",
                minigameId);
            Debug.Log("[Duzhe] Minigame completed: " + minigameId);
            Close();
        }

        /// Closes the active panel and restores exploring input.
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (activePanel != null)
            {
                GameObject panelGo = activePanel.PanelGO;
                if (panelGo != null) Destroy(panelGo);
                Destroy(activePanel as Component);
            }
            activePanel = null;
            activeConfig = null;
            GameSession session = GameSession.Instance;
            if (session != null)
            {
                // Restore exploring only if the current mode is still ours
                // (a pause overlay keeps its own mode management).
                if (!session.Paused && session.InputMode == InputMode.MiniGame)
                {
                    session.SetInputMode(InputMode.Exploring);
                }
            }
        }

        public static bool IsPausedForPanel()
        {
            GameSession s = GameSession.Instance;
            return s != null && s.Paused;
        }

        private void Update()
        {
            if (!IsOpen) return;
            GameSession session = GameSession.Instance;
            if (session == null) return;
            if (session.Paused != lastPaused)
            {
                lastPaused = session.Paused;
                // Pause froze the clock: unfinished drag/stroke work returns to
                // its origin now (spec: restore on resume), finished work stays.
                if (activePanel != null) activePanel.OnGamePaused();
            }
        }

        /// Development-only test entry (dev tester button): opens the picker
        /// panel. Never present in a shipped menu (C11: compiled out of
        /// release builds together with its panel and the pause button).
        public void OpenTester()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsOpen) Close();
            GameObject go = new GameObject("MinigameTesterPanel");
            RectTransform rt = go.AddComponent<RectTransform>(); // explicit: no auto-conversion race
            go.transform.SetParent(UiCanvasTransformSafe(), false);
            // Full-stretch root: child coordinates are 1280x720 reference space.
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.transform.SetAsLastSibling(); // tester renders above panels it opens later
            go.AddComponent<MinigameTesterPanel>();
#endif
        }

        private static Transform UiCanvasTransformSafe()
        {
            GameObject canvas = GameObject.Find(GameBootstrap.UiCanvasName);
            return canvas != null ? canvas.transform : null;
        }
    }

    /// DEV-only picker panel for the seven minigames (never in shipped menus;
    /// C11 compiles it out of release builds).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class MinigameTesterPanel : MonoBehaviour
    {
        private void Start()
        {
            Build();
        }

        private void Build()
        {
            DuzheUiFactory.AddFullStretchImage(transform, "Dim", new Color32(0, 0, 0, 170), true);
            DuzheUiFactory.AddPanel(transform, "Card",
                new Vector2(360f, -90f), new Vector2(560f, 540f), new Color32(24, 22, 30, 246));
            DuzheUiFactory.AddLabel(transform, "Title",
                new Vector2(360f, -104f), new Vector2(560f, 36f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                "机关测试（DEV · Placeholder）";

            string[] ids =
            {
                "MG01_Cane", "MG02_Door", "MG03_Stools", "MG04_Heart",
                "MG05_Notes", "MG06_Photo", "MG07_Goodbye"
            };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                int col = i % 2, row = i / 2;
                DuzheUiFactory.AddButton(transform, "Test_" + id,
                    new Vector2(380f + col * 270f, -150f - row * 62f), new Vector2(250f, 50f),
                    id, 20, () =>
                    {
                        MinigameHost host = MinigameHost.Instance;
                        if (host == null) return;
                        Destroy(gameObject); // close the tester first
                        host.Open(MinigameCatalog.Get(id));
                    });
            }

            // enablePerformance=false path: the notes minigame completes
            // immediately without performance (mirrors CH03 with performance
            // switched off).
            DuzheUiFactory.AddButton(transform, "Test_NotesOff",
                new Vector2(380f, -150f - 4 * 62f), new Vector2(520f, 50f),
                "MG05_Notes（enablePerformance=false 关闭路径）", 20, () =>
                {
                    MinigameHost host = MinigameHost.Instance;
                    if (host == null) return;
                    Destroy(gameObject);
                    host.Open(MinigameCatalog.Get("MG05_Notes"));
                    host.Complete(); // the off-path: auto-completed, one event
                });

            DuzheUiFactory.AddButton(transform, "CloseTester",
                new Vector2(380f, -150f - 5 * 62f), new Vector2(520f, 50f),
                "关闭测试面板", 20, () => Destroy(gameObject));
        }
    }
#endif

    /// Shared base for the four panels: builds UI under the persistent canvas
    /// at the bottom of the sibling stack (the pause overlay always renders
    /// above), and knows its config/host.
    public abstract class MinigamePanelBase : MonoBehaviour, IMinigamePanel
    {
        public MinigameConfig Config { get; private set; }
        public GameObject PanelGO { get; private set; }
        protected MinigameHost Host;

        public MinigamePanelBase Init(MinigameConfig cfg, MinigameHost host)
        {
            Config = cfg;
            Host = host;
            // The panel COMPONENT lives on the same object as its UI (created
            // by the host), so pointer/drag events bubbling up from the panel
            // reach the component's handlers.
            PanelGO = gameObject;
            BuildUi(PanelGO.transform, cfg);
            Debug.Log("[Duzhe] Panel built: " + cfg.minigameId + " active=" +
                PanelGO.activeSelf + " rect=" + ((RectTransform)PanelGO.transform).rect.size);
            return this;
        }

        private static Transform UiCanvasTransform()
        {
            GameObject canvas = GameObject.Find(GameBootstrap.UiCanvasName);
            return canvas != null ? canvas.transform : null;
        }

        protected abstract void BuildUi(Transform parent, MinigameConfig cfg);

        public abstract void OnGamePaused();
    }
}
