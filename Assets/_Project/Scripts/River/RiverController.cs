using System.Collections.Generic;
using Duzhe.Chapters;
using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.River
{
    /// Scene-level owner of the whole crossing: briefing (shown once, its
    /// time never counts), sailing with deterministic seeded whirlpools,
    /// crash handling with a local retry, and the single RiverSucceeded
    /// event. A crash resets ONLY the RiverSession (boat/time/obstacles/
    /// retry UI) - puzzles, handover, ritual and played dialogue flags live
    /// in ChapterRuntimeState and survive every retry.
    /// Collision is a pure AABB test evaluated BEFORE the 10-second success
    /// check, so the same frame can never produce both results.
    /// All timing uses Time.deltaTime: the pause overlay (timeScale=0) and
    /// focus loss (runInBackground=off) freeze the crossing for free.
    public class RiverController : MonoBehaviour
    {
        [Tooltip("Boat of this scene; assigned by ProjectSetupUtility.")]
        public BoatController boat;

        [Tooltip("Whirlpool manager of this scene; assigned by ProjectSetupUtility.")]
        public WhirlpoolSpawner spawner;

        private RiverSession session;
        private List<RiverWave> waves;
        private RiverSettings settings;
        private bool succeededFired;
        private GameObject briefingPanel;
        private GameObject retryPanel;
        private GameObject[] passengers;
        private string runIdAtStart = "";
        private Transform previousCameraTarget;
        private float previousCameraSize;
        private bool cameraBound;

        private void Start()
        {
            session = new RiverSession();
            GameSession gs = GameSession.Instance;
            if (gs != null)
            {
                runIdAtStart = gs.RunId;
                gs.RunReset += OnRunReset;
            }
        }

        /// A run reset (menu return / chapter switch / gallery) stops this
        /// crossing at once: no more timing, no leftover panels or hazards.
        private void OnRunReset()
        {
            if (spawner != null) spawner.ClearAll();
            if (briefingPanel != null) briefingPanel.SetActive(false);
            if (retryPanel != null) retryPanel.SetActive(false);
            GameSession gs = GameSession.Instance;
            if (gs != null && gs.Audio != null) gs.Audio.StopSfxLoop("SFX06_Rowing");
            session = new RiverSession();
            succeededFired = false;
        }

        /// Only a real chapter crossing may start: gallery replays and any
        /// non-River stage must leave the scene idle.
        private static bool CanStartCrossing(GameSession gs)
        {
            if (gs.Mode != GameMode.MainRun && gs.Mode != GameMode.ChapterReplay) return false;
            if (gs.Chapters == null || string.IsNullOrEmpty(gs.Chapters.CurrentChapterId)) return false;
            string stage = gs.Chapters.CurrentStageId;
            return !string.IsNullOrEmpty(stage) && stage.EndsWith("_River");
        }

        private void Update()
        {
            GameSession gs = GameSession.Instance;
            if (gs == null) return;
            // A stale controller from an old run may never keep sailing.
            if (!string.IsNullOrEmpty(runIdAtStart) && gs.RunId != runIdAtStart) return;

            if (session == null) session = new RiverSession();

            if (session.phase == RiverSession.Phase.Idle)
            {
                // The ChapterDirector enters River input mode once the stage
                // is entered; pick it up here (works for editor direct-play).
                if (gs.InputMode == InputMode.River && CanStartCrossing(gs))
                {
                    Initialize(gs);
                }
                return;
            }

            if (session.phase != RiverSession.Phase.Sailing) return;
            if (gs.Paused) return; // explicit freeze guard (deltaTime is 0 anyway)

            ChapterDirector director = gs.Chapters;
            if (director == null) return;

            float dt = Time.deltaTime;

            // 1) Steer the boat (continuous, clamped).
            if (boat != null)
            {
                boat.Steer(InputRouter.Instance != null ? InputRouter.Instance.Move.x : 0f, dt);
                boat.Advance(dt);
                session.boatX = boat.BoatX;
                SyncPlayerToBoat();
            }

            // 2) Spawn due waves, move existing whirlpools.
            if (spawner != null)
            {
                spawner.FollowBoat(boat != null ? boat.BoatY : -3f);
                spawner.SpawnDue(session);
                spawner.MoveAll(dt, settings);
            }

            // 3) Collision arbitration FIRST: if the boat overlaps a
            //    whirlpool this frame, the crossing ends now - success cannot
            //    happen in the same frame.
            if (spawner != null && spawner.HitsBoat(session.boatX, boat != null ? boat.BoatY : -3f, settings))
            {
                OnCrashed();
                return;
            }

            // 4) Accumulate effective sailing time and check success.
            session.elapsed += dt;
            if (session.elapsed >= settings.sailDuration)
            {
                OnSucceeded();
            }
        }

        // ------------------------------------------------------------------

        private void Initialize(GameSession gs)
        {
            ChapterDirector director = gs.Chapters;
            if (director == null || boat == null || spawner == null)
            {
                Debug.LogError("[Duzhe] River not fully assembled (director/boat/spawner missing).");
                return;
            }
            ChapterConfig cfg = ChapterCatalog.GetChapter(director.CurrentChapterId);
            settings = cfg != null ? cfg.riverSettings : new RiverSettings();

            // Same seed for every retry of this run; new seed per chapter run.
            waves = RiverRules.PlanWaves(director.CurrentRiverSeed, settings);
            spawner.Bind(waves, settings, boat.boatLineY);
            boat.Bind(settings);
            session.boatX = 0f;

            // Raise the camera so whirlpools enter the visible band early.
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.CameraFollow != null)
            {
                // Hold a wide view so forward travel is visible on screen.
                // The top edge preserves the original reaction window at arrival.
                if (!cameraBound)
                {
                    previousCameraTarget = boot.CameraFollow.followTarget;
                    Camera camera = boot.CameraFollow.GetComponent<Camera>();
                    previousCameraSize = camera != null ? camera.orthographicSize : GameBootstrap.CameraHalfHeight;
                    GameObject anchor = new GameObject("RiverCameraAnchor");
                    anchor.transform.SetParent(transform, false);
                    anchor.transform.position = new Vector3(0f, boat.boatLineY + BoatController.VoyageDistance + settings.cameraYOffset + GameBootstrap.CameraHalfHeight - 8.5f, 0f);
                    boot.CameraFollow.followTarget = anchor.transform;
                    if (camera != null) camera.orthographicSize = 8.5f;
                    cameraBound = true;
                }
                boot.CameraFollow.yOffset = 0f;
                boot.CameraFollow.SnapToTarget();
            }

            CreatePassengers();
            SyncPlayerToBoat();

            if (session.briefingShown)
            {
                // Retries after a crash go straight back to sailing.
                session.phase = RiverSession.Phase.Sailing;
            }
            else
            {
                session.phase = RiverSession.Phase.Briefing;
                ShowBriefingPanel();
            }
            Debug.Log("[Duzhe] River initialized: seed=" + director.CurrentRiverSeed +
                " passengers=" + settings.passengerCount +
                " maxBanned=" + settings.maxBannedLanesPerWave);
        }

        private void CreatePassengers()
        {
            if (passengers != null) return;
            int count = settings.passengerCount;
            passengers = new GameObject[count];
            string chapterId = GameSession.Instance != null && GameSession.Instance.Chapters != null
                ? GameSession.Instance.Chapters.CurrentChapterId
                : "";
            for (int i = 0; i < count; i++)
            {
                GameObject p = new GameObject("RiverPassenger_" + i);
                p.transform.SetParent(boat.transform, false);
                float side = (i % 2 == 0) ? -1f : 1f;
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                // C12: the passenger is the chapter's own spirit (grandparents
                // for CH01, the girl / drummer / teacher afterwards), not a
                // generic placeholder body.
                sr.sprite = PassengerSprite(chapterId, i);
                sr.sortingLayerName = DuzheSortingLayers.Actors;
                // Explicit order above the ferry: the boat sits at -2 and the
                // whirlpools at -3, so no passenger can ever be hidden behind
                // the hull regardless of the Y-sort axis.
                sr.sortingOrder = 2;
                sr.color = new Color(1f, 1f, 1f, 0.9f);

                // Feet on the deck line: feet-pivot art (0.5,0) needs no lift,
                // centre-pivot art is raised by half its height.
                float footLift = 0f;
                if (sr.sprite != null && sr.sprite.pixelsPerUnit > 0f)
                {
                    footLift = sr.sprite.pivot.y / sr.sprite.pixelsPerUnit;
                }
                float hullScale = boat.transform.localScale.x;
                p.transform.localScale = Vector3.one / hullScale;
                p.transform.localPosition = new Vector3(count > 1 ? side * 0.46f : 0f,
                    0.25f + footLift, 0f) / hullScale;
                passengers[i] = p;
            }
        }

        /// Local Y (inside the boat) where the passengers' feet stand. The
        /// ferry art is 2 units tall around the centre, so -0.2 is the deck
        /// slightly toward the stern, clear of the ferryman.
        private const float PassengerDeckLocalY = -0.35f;

        /// The chapter's spirit sprite for the crossing (CH05 has none).
        private static Sprite PassengerSprite(string chapterId, int index)
        {
            string assetId = "";
            switch (chapterId)
            {
                case "CH01": assetId = index == 0 ? "CHR02_Grandfather" : "CHR03_Grandmother"; break;
                case "CH02": assetId = "CHR05_LoverSpirit"; break;
                case "CH03": assetId = "CHR07_DrummerSpirit"; break;
                case "CH04": assetId = "CHR11_TeacherSpirit"; break;
            }
            if (!string.IsNullOrEmpty(assetId))
            {
                Sprite formal = Core.FormalArt.LoadCharacterStand(assetId);
                if (formal != null) return formal;
            }
            return Core.PlaceholderArt.LoadNpcSprite();
        }

        // ------------------------------------------------------------------
        // UI (code-built, attached under the persistent UI canvas)

        private Transform UiCanvasTransform()
        {
            GameObject canvas = GameObject.Find(GameBootstrap.UiCanvasName);
            return canvas != null ? canvas.transform : null;
        }

        private GameObject CreateRiverPanel(string name)
        {
            Transform parent = UiCanvasTransform();
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent != null ? parent : transform, false);
            // River panels sit at the BOTTOM of the canvas stack so the pause
            // menu and its confirm overlay always render above them; the
            // pause menu also bumps itself to the top when opened.
            panel.transform.SetAsFirstSibling();
            return panel;
        }

        private void ShowBriefingPanel()
        {
            if (briefingPanel != null) return; // built at most once per visit
            briefingPanel = CreateRiverPanel("RiverBriefingPanel");
            DuzheUiFactory.MakeFullStretch(briefingPanel);
            DuzheUiFactory.AddFullStretchImage(briefingPanel.transform, "Dim", new Color32(0, 0, 0, 120), false);

            DuzheUiFactory.AddPanel(briefingPanel.transform, "Card",
                new Vector2(400f, -240f), new Vector2(480f, 272f), new Color32(24, 22, 30, 242));
            DuzheUiFactory.AddLabel(briefingPanel.transform, "Title",
                new Vector2(420f, -270f), new Vector2(440f, 44f), 32,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("river.instructions_title", "渡过冥河");
            DuzheUiFactory.AddLabel(briefingPanel.transform, "Text",
                new Vector2(420f, -325f), new Vector2(440f, 70f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.UpperLeft).text =
                UiText.Get("river.instructions", "船会自行前进。使用 A、D 或左右方向键，避开漩涡。");
            DuzheUiFactory.AddLabel(briefingPanel.transform, "ActiveHint",
                new Vector2(420f, -400f), new Vector2(440f, 34f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.UpperLeft).text =
                UiText.Get("river.active_hint", "A / D 或 ← / →：左右避开漩涡");
            DuzheUiFactory.AddButton(briefingPanel.transform, "Start",
                new Vector2(440f, -448f), new Vector2(400f, 52f), UiText.Get("button.start_crossing", "开始渡河"), 26,
                () =>
                {
                    session.briefingShown = true;
                    session.phase = RiverSession.Phase.Sailing;
                    if (briefingPanel != null) briefingPanel.SetActive(false);
                    GameSession gs = GameSession.Instance;
                    if (gs != null && gs.Audio != null) gs.Audio.PlaySfxLoop("SFX06_Rowing");
                });
        }

        private void OnCrashed()
        {
            session.phase = RiverSession.Phase.Crashed;
            if (spawner != null) spawner.ClearAll();
            if (boat != null)
            {
                boat.ResetPosition();
                if (spawner != null) spawner.FollowBoat(boat.BoatY);
                SyncPlayerToBoat();
            }
            GameSession crashSession = GameSession.Instance;
            if (crashSession != null && crashSession.Audio != null)
            {
                crashSession.Audio.StopSfxLoop("SFX06_Rowing");
                crashSession.Audio.PlaySfx("SFX07_Whirlpool");
            }
            Debug.Log("[Duzhe] River crash on attempt " + (session.attemptCount + 1));

            // Reuse one retry panel for the whole visit; hiding it between
            // crashes keeps the canvas free of leftover hidden copies.
            if (retryPanel == null)
            {
                retryPanel = CreateRiverPanel("RiverRetryPanel");
                DuzheUiFactory.MakeFullStretch(retryPanel);
                DuzheUiFactory.AddFullStretchImage(retryPanel.transform, "Dim", new Color32(0, 0, 0, 130), false);

                DuzheUiFactory.AddPanel(retryPanel.transform, "Card",
                    new Vector2(400f, -260f), new Vector2(480f, 240f), new Color32(24, 22, 30, 242));
                DuzheUiFactory.AddLabel(retryPanel.transform, "Msg",
                    new Vector2(420f, -290f), new Vector2(440f, 44f), 28,
                    new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("river.retry_title", "被漩涡卷回了渡口");
                DuzheUiFactory.AddLabel(retryPanel.transform, "Body",
                    new Vector2(420f, -344f), new Vector2(440f, 60f), 22,
                    new Color32(180, 174, 162, 255), TextAnchor.UpperLeft).text =
                    UiText.Get("river.retry_body", "之前的任务已经完成。准备好后，再渡一次河。");
                DuzheUiFactory.AddButton(retryPanel.transform, "Retry",
                    new Vector2(440f, -416f), new Vector2(400f, 52f), UiText.Get("button.retry_river", "重新渡河"), 26,
                    () =>
                    {
                        if (retryPanel != null) retryPanel.SetActive(false);
                        // Local retry only: same seed, same waves; chapter flags
                        // (puzzle/handover/ritual/dialogue) are untouched.
                        session.ResetAttempt();
                        session.phase = RiverSession.Phase.Sailing;
                        GameSession gs = GameSession.Instance;
                        if (gs != null && gs.Audio != null) gs.Audio.PlaySfxLoop("SFX06_Rowing");
                        Debug.Log("[Duzhe] River retry attempt " + (session.attemptCount + 1) + " (same seed)");
                    });
            }
            else
            {
                retryPanel.SetActive(true);
            }
        }

        private void OnSucceeded()
        {
            if (succeededFired) return; // exactly one event, never a clear
            succeededFired = true;
            session.phase = RiverSession.Phase.Succeeded;
            // CH05 ends ON the river (no scene change): clear the hazards and
            // close any panel so the reincarnation beat starts on a clean
            // picture. Steering stops because the phase is no longer Sailing.
            if (spawner != null) spawner.ClearAll();
            if (briefingPanel != null) briefingPanel.SetActive(false);
            if (retryPanel != null) retryPanel.SetActive(false);
            GameSession gs = GameSession.Instance;
            if (gs != null && gs.Audio != null) gs.Audio.StopSfxLoop("SFX06_Rowing");
            gs.PublishEvent(GameEventType.RiverSucceeded,
                gs.Chapters != null ? gs.Chapters.CurrentChapterId : "",
                gs.Chapters != null ? gs.Chapters.CurrentStageId : "",
                "River");
            Debug.Log("[Duzhe] River succeeded after " + session.elapsed.ToString("F2") +
                "s, attempts=" + (session.attemptCount + 1));
        }

        // ------------------------------------------------------------------

        private void SyncPlayerToBoat()
        {
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot == null || boot.Player == null || boat == null) return;
            // Keep the ferryman standing on the boat without parenting him to
            // a scene object (he must survive scene loads).
            boot.Player.transform.position = new Vector3(boat.BoatX, boat.BoatY - 0.85f, 0f);
        }

        private void OnDestroy()
        {
            GameSession gs = GameSession.Instance;
            if (gs != null)
            {
                gs.RunReset -= OnRunReset;
                if (gs.Audio != null) gs.Audio.StopSfxLoop("SFX06_Rowing");
            }
            // Restore the camera for the next scene.
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.CameraFollow != null)
            {
                boot.CameraFollow.yOffset = 0f;
                if (cameraBound)
                {
                    boot.CameraFollow.followTarget = previousCameraTarget;
                    Camera camera = boot.CameraFollow.GetComponent<Camera>();
                    if (camera != null) camera.orthographicSize = previousCameraSize;
                }
            }
            // The river panels were parented under the PERSISTENT canvas, so
            // they would otherwise survive the scene load and leak into the
            // main menu. Destroy them on every exit path (this method runs on
            // success, retry-free exits and pause-menu leave alike, because
            // all of them unload this scene object).
            if (briefingPanel != null)
            {
                Destroy(briefingPanel);
                briefingPanel = null;
            }
            if (retryPanel != null)
            {
                Destroy(retryPanel);
                retryPanel = null;
            }
        }
    }
}
