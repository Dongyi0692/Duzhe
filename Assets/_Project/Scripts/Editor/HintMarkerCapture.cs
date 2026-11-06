using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Quick visual check for InteractableHintMarker: starts CH02 and captures
    /// the Underworld with the girl's and the necklace's "!" markers visible.
    public static class HintMarkerCapture
    {
        [MenuItem("渡者/引导标记捕获")]
        public static void RunInteractive() { Start(); }
        public static void RunFromCommandLine() { Start(); }

        private const string KeyActive = "Duzhe.HintMarkerCapture.Active";
        private const string KeyPhase = "Duzhe.HintMarkerCapture.Phase";
        private const string ShotDir = "Logs/panel_geometry";

        [InitializeOnLoadMethod]
        private static void AutoResume()
        {
            if (SessionState.GetBool(KeyActive, false)) EditorApplication.update += OnUpdate;
        }

        private static void Start()
        {
            SessionState.SetBool(KeyActive, true);
            SessionState.SetInt(KeyPhase, 0);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Underworld.unity", OpenSceneMode.Single);
            EditorApplication.delayCall += () => { PlayerSettings.runInBackground = true; };
            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= OnUpdate;
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(0);
                return;
            }
            int phase = SessionState.GetInt(KeyPhase, 0);
            if (phase == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("HintCaptureDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private float t;
            private bool started;

            private void Start()
            {
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null)
                {
                    session.SetMode(Core.GameMode.MainRun);
                    if (session.MainMenu != null) session.MainMenu.Refresh();
                    session.Chapters.StartChapter("CH02", Core.GameMode.MainRun);
                }
            }

            private void Update()
            {
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                {
                    session.Pause.Close();
                }
                t += Time.unscaledDeltaTime;
                if (!started && t >= 3f)
                {
                    started = true;
                    Directory.CreateDirectory(ShotDir);
                    ScreenCapture.CaptureScreenshot(ShotDir + "/hint_marker_underworld.png");
                    SessionState.SetInt(KeyPhase, 2);
                    EditorApplication.isPlaying = false;
                }
            }
        }
    }
}
