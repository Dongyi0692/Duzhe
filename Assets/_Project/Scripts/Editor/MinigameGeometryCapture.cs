using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Captures the three C03 minigame panels in a REAL play session and
    /// records their actual on-screen geometry for the art handoff:
    /// MG02_Door, MG06_Photo, MG07_Goodbye. GUI editor run (no -batchmode,
    /// no -quit): -executeMethod Duzhe.EditorTools.MinigameGeometryCapture.RunFromCommandLine
    /// Screenshots: Logs/panel_geometry/. Exit 0 on completion.
    public static class MinigameGeometryCapture
    {
        private const string KeyActive = "Duzhe.MinigameGeometryCapture.Active";
        private const string KeyPhase = "Duzhe.MinigameGeometryCapture.Phase";
        private const string ShotDir = "Logs/panel_geometry";

        [MenuItem("渡者/机关面板几何捕获(供美术导出)")]
        public static void RunInteractive() { Start(); }

        public static void RunFromCommandLine() { Start(); }

        [InitializeOnLoadMethod]
        private static void AutoResume()
        {
            if (SessionState.GetBool(KeyActive, false)) EditorApplication.update += OnUpdate;
        }

        private static void Start()
        {
            SessionState.SetBool(KeyActive, true);
            SessionState.SetInt(KeyPhase, 0);
            try
            {
                Directory.CreateDirectory(ShotDir);
                foreach (string f in Directory.GetFiles(ShotDir, "*.png")) File.Delete(f);
            }
            catch { }
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
                GameObject go = new GameObject("GeometryCaptureDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private string[] ids = { "MG02_Door", "MG06_Photo", "MG07_Goodbye" };
            private int index;
            private float t;
            private bool opened;
            private bool shotTaken;
            private bool interactionDone;

            private void Start()
            {
                // Hide the menu so the panel screenshots are unobstructed.
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null)
                {
                    session.SetMode(Core.GameMode.MainRun);
                    if (session.MainMenu != null) session.MainMenu.Refresh();
                }
            }

            private void Update()
            {
                // Close the focus-loss auto-pause as soon as it appears.
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                {
                    session.Pause.Close();
                }
                t += Time.unscaledDeltaTime;

                if (index >= ids.Length)
                {
                    if (t > 0.5f)
                    {
                        SessionState.SetInt(KeyPhase, 2);
                        EditorApplication.isPlaying = false;
                    }
                    return;
                }

                if (!opened)
                {
                    // Open as soon as the host is ready (frame-count safe).
                    if (Minigames.MinigameHost.Instance != null && !Minigames.MinigameHost.Instance.IsOpen)
                    {
                        Minigames.MinigameHost.Instance.Open(Minigames.MinigameCatalog.Get(ids[index]));
                        opened = true;
                    }
                    return;
                }

                if (!shotTaken && t >= 1.2f)
                {
                    ScreenCapture.CaptureScreenshot(ShotDir + "/" + ids[index] + ".png");
                    shotTaken = true;
                    return;
                }

                if (!interactionDone && t >= 1.35f)
                {
                    interactionDone = true;
                    if (ids[index] == "MG06_Photo") RunPhotoDragTest();
                    else if (ids[index] == "MG07_Goodbye") CheckBlackboardLayout();
                }

                if (t >= 1.8f)
                {
                    if (Minigames.MinigameHost.Instance != null && Minigames.MinigameHost.Instance.IsOpen)
                    {
                        Minigames.MinigameHost.Instance.Close();
                    }
                    index++;
                    t = 0f;
                    opened = false;
                    shotTaken = false;
                    interactionDone = false;
                }
            }

            private static Vector3[] ScreenCorners(GameObject go)
            {
                Vector3[] c = new Vector3[4];
                ((RectTransform)go.transform).GetWorldCorners(c);
                return c;
            }

            private static void RunPhotoDragTest()
            {
                GameObject drummer = GameObject.Find("Drummer");
                GameObject gap = GameObject.Find("DrummerGap");
                if (drummer == null || gap == null)
                {
                    Debug.LogError("DZ_MG06_DRAG=FAIL missing drummer/gap");
                    return;
                }

                Vector3[] dc = ScreenCorners(drummer);
                Vector3[] gc = ScreenCorners(gap);
                Vector2 from = (dc[0] + dc[2]) * 0.5f;
                Vector2 to = (gc[0] + gc[2]) * 0.5f;
                var ped = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    position = from,
                    pressPosition = from,
                    button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
                };
                UnityEngine.EventSystems.ExecuteEvents.Execute<UnityEngine.EventSystems.IBeginDragHandler>(
                    drummer, ped, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
                ped.position = to;
                UnityEngine.EventSystems.ExecuteEvents.Execute<UnityEngine.EventSystems.IDragHandler>(
                    drummer, ped, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
                UnityEngine.EventSystems.ExecuteEvents.Execute<UnityEngine.EventSystems.IEndDragHandler>(
                    drummer, ped, UnityEngine.EventSystems.ExecuteEvents.endDragHandler);

                bool closed = Minigames.MinigameHost.Instance == null || !Minigames.MinigameHost.Instance.IsOpen;
                Debug.Log(closed
                    ? "DZ_MG06_DRAG=PASS drummer snapped into the unique gap and the panel completed"
                    : "DZ_MG06_DRAG=FAIL panel stayed open after the drummer drag");
            }

            private static void CheckBlackboardLayout()
            {
                GameObject board = GameObject.Find("BlackboardArt");
                GameObject draw = GameObject.Find("DrawArea");
                GameObject redraw = GameObject.Find("Redraw");
                GameObject close = GameObject.Find("CloseTrace");
                if (board == null || draw == null || redraw == null || close == null)
                {
                    Debug.LogError("DZ_MG07_LAYOUT=FAIL missing board/draw/buttons");
                    return;
                }

                float boardBottom = ScreenCorners(board)[0].y;
                float drawBottom = ScreenCorners(draw)[0].y;
                float drawTop = ScreenCorners(draw)[1].y;
                float redrawTop = ScreenCorners(redraw)[1].y;
                float closeTop = ScreenCorners(close)[1].y;
                bool insideBoard = drawBottom >= boardBottom - 0.5f;
                bool aboveButtons = drawBottom >= Mathf.Max(redrawTop, closeTop) + 0.5f;
                Debug.Log("DZ_MG07_LAYOUT=" + (insideBoard && aboveButtons ? "PASS" : "FAIL") +
                    " boardBottom=" + boardBottom.ToString("F1") +
                    " draw=" + drawBottom.ToString("F1") + ".." + drawTop.ToString("F1") +
                    " buttonsTop=" + Mathf.Max(redrawTop, closeTop).ToString("F1") +
                    " insideBoard=" + insideBoard + " aboveButtons=" + aboveButtons);
            }
        }
    }
}
