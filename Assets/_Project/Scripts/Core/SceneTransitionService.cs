using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duzhe.Core
{
    /// sceneId -> build scene name binding plus async load + player placement.
    /// Only this service loads game scenes; scenes never create their own
    /// managers (bootstrap is global and persistent).
    public class SceneTransitionService : MonoBehaviour
    {
        public static SceneTransitionService Instance { get; private set; }

        /// Fixed scene registration (TechnicalAndInteractionSpec section 1):
        /// MainMenu plus six environments. CH05 reuses the River ending layer,
        /// no seventh environment.
        private static readonly Dictionary<string, string> SceneBindings = new Dictionary<string, string>
        {
            { DuzheIds.SceneMainMenu, "MainMenu" },
            { DuzheIds.SceneUnderworld, "Underworld" },
            { DuzheIds.SceneRiver, "River" },
            { DuzheIds.SceneFarmCourtyard, "FarmCourtyard" },
            { DuzheIds.ScenePark, "Park" },
            { DuzheIds.SceneRecordingStudio, "RecordingStudio" },
            { DuzheIds.SceneClassroom, "Classroom" }
        };

        public bool IsTransitioning { get; private set; }
        public string CurrentSceneId { get; private set; } = "";

        /// Fired after a scene is active and the player has been placed;
        /// payload = sceneId.
        public event Action<string> OnSceneReady;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static bool TryGetSceneName(string sceneId, out string sceneName)
        {
            return SceneBindings.TryGetValue(sceneId ?? "", out sceneName);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene current, Scene next)
        {
            // Direct play of a game scene in the editor is allowed: resolve
            // the sceneId from the freshly loaded scene's identity component.
            SceneIdentity identity = FindIdentityInScene(next);
            if (identity != null) CurrentSceneId = identity.sceneId;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            SceneIdentity identity = FindIdentityInScene(scene);
            if (identity != null) CurrentSceneId = identity.sceneId;

            // A controlled load reports ready in its own routine AFTER the
            // player is placed and IsTransitioning is cleared. Only an
            // uncontrolled load (editor direct play / the first scene) may
            // notify here.
            if (!IsTransitioning) NotifyReady(CurrentSceneId);
        }

        /// Announces a scene exactly once, after the player is placed.
        private void NotifyReady(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Duzhe] Scene ready: " + sceneId + " (controlled=" + IsTransitioning + ")");
#endif
            GameSession session = GameSession.Instance;
            // Re-apply route visibility for the freshly loaded scene before
            // any cast runs, so a same-chapter re-entry shows the right actors.
            if (session != null) session.SetVisualChapter(session.VisualChapterId);
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.NotifySceneReady(sceneId);
            if (OnSceneReady != null) OnSceneReady(sceneId);
        }

        private static SceneIdentity FindIdentityInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject go in roots)
            {
                SceneIdentity id = go.GetComponentInChildren<SceneIdentity>(true);
                if (id != null) return id;
            }
            return null;
        }

        /// Load a bound scene and place the player at spawnPointId (default
        /// spawn when null/empty). Input is locked during the transition.
        public void LoadScene(string sceneId, string spawnPointId = null)
        {
            string sceneName;
            if (!TryGetSceneName(sceneId, out sceneName))
            {
                Debug.LogError("[Duzhe] LoadScene: unbound sceneId '" + sceneId + "'.");
                return;
            }
            if (IsTransitioning)
            {
                Debug.LogWarning("[Duzhe] LoadScene ignored: another transition is already running.");
                return;
            }
            StartCoroutine(LoadSceneRoutine(sceneId, sceneName, spawnPointId));
        }

        private IEnumerator LoadSceneRoutine(string sceneId, string sceneName, string spawnPointId)
        {
            GameSession session = GameSession.Instance;
            IsTransitioning = true;
            if (session != null) session.SetInputMode(InputMode.Transition);
            // The menu mode must switch BEFORE the load completes so that the
            // scene-ready hook (main menu refresh) already sees it.
            if (session != null && sceneId == DuzheIds.SceneMainMenu)
            {
                session.SetMode(GameMode.MainMenu);
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError("[Duzhe] LoadSceneAsync returned null for '" + sceneName + "'. Is the scene in Build Settings?");
                IsTransitioning = false;
                if (session != null && session.Mode != GameMode.MainMenu) session.SetInputMode(InputMode.Exploring);
                yield break;
            }
            while (!op.isDone) yield return null;

            // Scene is loaded; OnSceneLoaded already updated CurrentSceneId
            // but did NOT report ready (a controlled load was running).
            GameBootstrap.Instance.PlacePlayerAtScene(spawnPointId);
            IsTransitioning = false;

            // Base input state first, THEN the single ready notification; the
            // director may switch to Dialogue/Ritual inside that callback and
            // nothing here may overwrite it afterwards.
            if (session != null)
            {
                if (sceneId == DuzheIds.SceneMainMenu)
                {
                    session.SetInputMode(InputMode.Menu);
                }
                else if (session.InputMode == InputMode.Transition)
                {
                    session.SetInputMode(InputMode.Exploring);
                }
            }

            NotifyReady(sceneId);
        }
    }
}
