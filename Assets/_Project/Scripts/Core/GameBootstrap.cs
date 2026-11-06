using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duzhe.Core
{
    /// Global bootstrap. Creates the one persistent GameRoot (session, services,
    /// player, camera, UI) exactly once per run, no matter which scene is
    /// played first. Scene files contain only static world data; everything
    /// behavioural is built here so ProjectSetupUtility stays idempotent.
    [DefaultExecutionOrder(-200)]
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public const string RootName = "DZ_GameRoot";
        public const string PlayerName = "DZ_Player";
        public const string CameraName = "DZ_Camera";
        public const string UiCanvasName = "DZ_UICanvas";

        public const float PixelsPerUnit = 32f;
        public const float CameraHalfHeight = 5.625f; // 640x360 viewport

        public Player.PlayerController2D Player { get; private set; }
        public Player.CameraFollow2D CameraFollow { get; private set; }

        private static Font uiFont;

        public static Font UiFont
        {
            get
            {
                if (uiFont == null)
                {
                    // Bundled TTF first: OS dynamic fonts proved unreliable for
                    // CJK glyph rasterization in player builds on this engine;
                    // a packaged font is build-stable.
                    Font bundled = Resources.Load<Font>("Fonts/simhei");
                    if (bundled != null) { uiFont = bundled; return uiFont; }
                    uiFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 32);
                    if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("SimHei", 32);
                }
                return uiFont;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRoot()
        {
            if (FindObjectOfType<GameBootstrap>() != null) return; // idempotent per domain
            GameObject root = new GameObject(RootName);
            root.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Debug.Log("[Duzhe] Bootstrap start");
            // C12: honour the persisted window mode/resolution (windowed vs
            // fullscreen, 1280x720 / 1600x900 / 1920x1080). First launch has
            // no stored value and falls back to the reference 1280x720 window.
            DisplaySettings.Load();
            DisplaySettings.Apply();
            BuildServices();
            Debug.Log("[Duzhe] Services built");
            BuildCamera();
            Debug.Log("[Duzhe] Camera built");
            BuildPlayer();
            Debug.Log("[Duzhe] Player built");
            BuildUi();
            Debug.Log("[Duzhe] UI built, font=" + (UiFont != null ? UiFont.name : "NULL"));
            ConfigureLayerCollisions();
            Application.focusChanged += OnFocusChanged;
            GameSession session = GameSession.Instance;
            session.SetInputMode(InputMode.Menu);
            // Whatever scene we started in, treat it as ready so the player
            // and camera bind correctly (editor direct-play of a game scene).
            SceneIdentity identity = FindObjectOfType<SceneIdentity>();
            string sceneId = identity != null ? identity.sceneId : DuzheIds.SceneMainMenu;
            Debug.Log("[Duzhe] Startup sceneId=" + sceneId);
            NotifySceneReady(sceneId);
            Debug.Log("[Duzhe] NotifySceneReady done, mode=" + session.Mode);
        }

        private void OnDestroy()
        {
            Application.focusChanged -= OnFocusChanged;
            if (Instance == this) Instance = null;
        }

        private void BuildServices()
        {
            GameObject go = gameObject;
            GameSession session = go.AddComponent<GameSession>();
            go.AddComponent<InputRouter>();
            SaveService save = go.AddComponent<SaveService>();
            go.AddComponent<SceneTransitionService>();
            Audio.AudioService audio = go.AddComponent<Audio.AudioService>();
            Dialogue.DialogueRunner dialogue = go.AddComponent<Dialogue.DialogueRunner>();
            Chapters.ChapterDirector chapters = go.AddComponent<Chapters.ChapterDirector>();
            Minigames.MinigameHost minigames = go.AddComponent<Minigames.MinigameHost>();
            Presentation.PresentationPlayer presentations = go.AddComponent<Presentation.PresentationPlayer>();

            session.Save = save;
            session.Transitions = SceneTransitionService.Instance;
            session.Audio = audio;
            session.Dialogue = dialogue;
            session.Chapters = chapters;
            session.Minigames = minigames;
            session.Presentations = presentations;
            audio.LoadVolumesFrom(save);
        }

        private void BuildCamera()
        {
            GameObject camGo = new GameObject(CameraName);
            camGo.transform.SetParent(transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraHalfHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(18, 16, 24, 255);
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 50f;
            cam.transparencySortMode = TransparencySortMode.CustomAxis; // Tuanjie name for CustomSort
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f); // actors sort by feet Y
            camGo.AddComponent<AudioListener>();
            CameraFollow = camGo.AddComponent<Player.CameraFollow2D>();
        }

        private void BuildPlayer()
        {
            GameObject playerGo = new GameObject(PlayerName);
            playerGo.transform.SetParent(transform, false);
            playerGo.layer = DuzheLayers.Player;

            // Visual carrier: a CHILD renderer keeps the player's transform
            // semantics (feet collider at -0.38) untouched. The formal R12
            // CHR01 sheet uses a feet pivot (0.5,0): its child sits 0.5u down
            // so the sprite's feet land where the old centre-pivot
            // placeholder's bottom edge was.
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(playerGo.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = DuzheSortingLayers.Actors;

            Sprite[] ferrymanFrames = FormalArt.LoadFerrymanFrames();
            if (ferrymanFrames != null)
            {
                visual.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                Player.PlayerSpriteAnimator animator = visual.AddComponent<Player.PlayerSpriteAnimator>();
                animator.Bind(ferrymanFrames);
                sr.sprite = ferrymanFrames[(int)Direction4.Down * 3 + 1];
                Debug.Log("[Duzhe] Player visual: formal CHR01 walk sheet (12 frames, feet pivot).");
            }
            else
            {
                visual.transform.localPosition = Vector3.zero;
                sr.sprite = PlaceholderArt.LoadPlayerSprite();
                Debug.LogWarning("[Duzhe] Player visual: CHR01 formal sheet missing; placeholder in use.");
            }

            BoxCollider2D feet = playerGo.AddComponent<BoxCollider2D>();
            feet.size = new Vector2(0.4f, 0.24f);
            feet.offset = new Vector2(0f, -0.38f);

            Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // top-down, no gravity
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Player = playerGo.AddComponent<Player.PlayerController2D>();
            playerGo.AddComponent<Interaction.InteractionController>();
            playerGo.SetActive(false); // hidden in menus, activated per scene
        }

        private void BuildUi()
        {
            GameSession session = GameSession.Instance;
            GameObject canvas = UI.DuzheUiFactory.CreateCanvas(transform, UiCanvasName);
            session.Hud = UI.DuzheUiFactory.EnsureComponent<UI.ObjectiveHud>(canvas.transform);
            session.DialogueView = UI.DuzheUiFactory.EnsureComponent<Dialogue.DialogueView>(canvas.transform);
            session.Pause = UI.DuzheUiFactory.EnsureComponent<UI.PauseMenu>(canvas.transform);
            session.MainMenu = UI.DuzheUiFactory.EnsureComponent<UI.MainMenuPanel>(canvas.transform);
            session.Credits = UI.DuzheUiFactory.EnsureComponent<UI.CreditsPanel>(canvas.transform);
            session.Gallery = UI.DuzheUiFactory.EnsureComponent<UI.GalleryController>(canvas.transform);
            session.ChapterMenu = UI.DuzheUiFactory.EnsureComponent<UI.ChapterMenuPanel>(canvas.transform);
            session.DialogueView.Bind(session.Dialogue);
            session.Hud.Hide();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // TEMPORARY click diagnostics (development builds only; C11).
            GameObject probeGo = new GameObject("DZ_ClickProbe");
            probeGo.transform.SetParent(transform, false);
            probeGo.AddComponent<ClickProbe>();
#endif
            UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
            int raycasters = FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>().Length;
            Debug.Log("[Duzhe] UI diagnostics: eventSystem=" + (es != null ? es.name : "NULL") +
                " module=" + (es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NONE") +
                " graphicRaycasters=" + raycasters);
        }

        /// Only the collision pairs we actually need; everything else between
        /// our layers is ignored so unrelated colliders never interfere.
        private void ConfigureLayerCollisions()
        {
            int[] layers = { DuzheLayers.Player, DuzheLayers.WorldSolid, DuzheLayers.Interactable,
                             DuzheLayers.Trigger, DuzheLayers.Boat, DuzheLayers.RiverHazard };
            for (int i = 0; i < layers.Length; i++)
            {
                for (int j = i; j < layers.Length; j++)
                {
                    bool keep;
                    if (i == j)
                    {
                        keep = false; // same-layer objects never interact
                    }
                    else
                    {
                        bool playerInvolved = layers[i] == DuzheLayers.Player || layers[j] == DuzheLayers.Player;
                        bool boatVsHazard = (layers[i] == DuzheLayers.Boat && layers[j] == DuzheLayers.RiverHazard) ||
                                           (layers[j] == DuzheLayers.Boat && layers[i] == DuzheLayers.RiverHazard);
                        keep = playerInvolved || boatVsHazard;
                    }
                    Physics2D.IgnoreLayerCollision(layers[i], layers[j], !keep);
                }
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            GameSession session = GameSession.Instance;
            if (session == null) return;
            // Auto-pause on focus loss; when focus returns the game stays on
            // the pause screen (nothing auto-resumes).
            if (!hasFocus && session.Mode != GameMode.MainMenu && !session.Paused)
            {
                if (session.Pause != null) session.Pause.Open();
            }
        }

        /// Called by SceneTransitionService after any scene load (and once for
        /// the startup scene).
        public void NotifySceneReady(string sceneId)
        {
            SceneContent content = FindObjectOfType<SceneContent>();
            bool isMenu = sceneId == DuzheIds.SceneMainMenu;
            if (isMenu)
            {
                if (Player != null) Player.gameObject.SetActive(false);
                // The start screen ALWAYS shows its own fixed backdrop; this
                // is re-applied on every menu entry so a broken/missing scene
                // reference (or a leftover from an older assembly) can never
                // leave another chapter's art - or the bare clear colour - on
                // the title screen.
                EnsureMenuBackdrop(content);
                if (CameraFollow != null)
                {
                    // Fixed menu framing. Before C12 the camera kept the LAST
                    // gameplay position (followTarget stayed bound to the
                    // hidden player), so the menu showed a piece of the
                    // previous scene instead of the dedicated menu backdrop.
                    CameraFollow.followTarget = null;
                    CameraFollow.yOffset = 0f;
                    CameraFollow.BindBounds(null);
                    CameraFollow.transform.position =
                        new Vector3(0f, 0f, CameraFollow.transform.position.z);
                }
            }
            else
            {
                if (Player != null)
                {
                    Player.gameObject.SetActive(true);
                    PlacePlayer(content, null);
                }
                if (CameraFollow != null)
                {
                    // The river raises the camera through yOffset; leaving it
                    // set would shift every later scene's framing.
                    CameraFollow.yOffset = 0f;
                    CameraFollow.followTarget = Player != null ? Player.transform : null;
                    CameraFollow.BindBounds(content != null ? content.GetWorldBounds() : new Bounds());
                    CameraFollow.SnapToTarget();
                }
            }
            // Refresh always: entering a game scene must hide the menu panel,
            // returning to the menu must show it.
            if (GameSession.Instance != null && GameSession.Instance.MainMenu != null)
            {
                GameSession.Instance.MainMenu.Refresh();
            }
            // C12: the menu must be a clean, fixed picture - clear the last
            // objective line / toast so nothing from the finished run stays.
            if (isMenu && GameSession.Instance != null && GameSession.Instance.Hud != null)
            {
                GameSession.Instance.Hud.Hide();
            }
            if (isMenu) HideRunOverlays();
            // C10: scene-based music (MusicCueMap).
            if (GameSession.Instance != null && GameSession.Instance.Audio != null)
            {
                if (isMenu)
                {
                    GameSession.Instance.Audio.StopMusic();
                }
                else
                {
                    string chapterId = GameSession.Instance.Chapters != null
                        ? GameSession.Instance.Chapters.CurrentChapterId : "";
                    GameSession.Instance.Audio.PlayMusicForScene(sceneId, chapterId);
                }
            }
        }

        /// The title screen always uses its OWN backdrop: the delivered
        /// `BG00_MainMenu` when it exists, otherwise the placeholder. Applied
        /// on every menu entry (not only at build time), so a missing or
        /// stale sprite reference in the scene can never leave another
        /// chapter's art - or the bare clear colour - behind the menu.
        private void EnsureMenuBackdrop(SceneContent content)
        {
            Sprite menuSprite = Resources.Load<Sprite>("Art/Backgrounds/BG00_MainMenu");
            if (menuSprite == null) menuSprite = Resources.Load<Sprite>("Art/Backgrounds/BG_MainMenu_Placeholder");

            SpriteRenderer sr = content != null ? content.backgroundRenderer : null;
            if (sr == null)
            {
                GameObject existing = GameObject.Find("Background");
                if (existing != null) sr = existing.GetComponent<SpriteRenderer>();
            }
            if (sr == null)
            {
                // No usable backdrop object at all: create one so the title
                // screen is never the camera's clear colour.
                GameObject go = new GameObject("Background");
                sr = go.AddComponent<SpriteRenderer>();
                Debug.LogWarning("[Duzhe] MainMenu had no background renderer; created one.");
            }
            if (menuSprite != null) sr.sprite = menuSprite;
            else Debug.LogWarning("[Duzhe] MainMenu backdrop sprite missing (BG00_MainMenu / BG_MainMenu_Placeholder).");
            sr.sortingLayerName = DuzheSortingLayers.Background;
            sr.sortingOrder = 0;
            sr.color = Color.white;
            sr.enabled = true;
            sr.transform.position = Vector3.zero;
            sr.transform.localScale = Vector3.one;
        }

        /// Defensive cleanup when the title screen appears: no run-owned
        /// overlay may stay on top of the menu. Every call is a no-op when
        /// the overlay is already closed, so this is safe to run every time.
        private static void HideRunOverlays()
        {
            GameSession s = GameSession.Instance;
            if (s == null) return;
            if (s.Pause != null && s.Pause.IsOpen) s.Pause.Close();
            if (s.DialogueView != null) s.DialogueView.HideLine(); // view only: never re-enters Exploring
            if (s.Minigames != null && s.Minigames.IsOpen) s.Minigames.Close();
            if (s.Credits != null) s.Credits.HideWithoutEvent();
            if (s.ChapterMenu != null) s.ChapterMenu.Hide();
            UI.FadeOverlay.Cancel();
            Presentation.PresentationCG.Hide();
            Presentation.BlackboardTextLayer.Hide();
        }

        /// Called by SceneTransitionService after a LoadScene completes.
        public void PlacePlayerAtScene(string spawnPointId)
        {
            SceneContent content = FindObjectOfType<SceneContent>();
            PlacePlayer(content, spawnPointId);
            if (CameraFollow != null && Player != null)
            {
                CameraFollow.followTarget = Player.transform;
                CameraFollow.SnapToTarget();
            }
        }

        private void PlacePlayer(SceneContent content, string spawnPointId)
        {
            if (Player == null) return;
            Transform spawn = content != null ? content.GetSpawnPoint(spawnPointId) : null;
            Vector3 pos = spawn != null ? spawn.position : new Vector3(0f, -3f, 0f);
            Player.transform.position = new Vector3(pos.x, pos.y, 0f);
            Player.ResetFacing();
        }
    }
}
