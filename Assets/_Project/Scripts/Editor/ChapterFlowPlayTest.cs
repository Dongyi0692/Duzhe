using System;
using System.Collections;
using System.IO;
using System.Text;
using Duzhe.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Main-line continuity check (reported bug: finishing CH01 dropped the
    /// player back to the start menu instead of continuing).
    ///   -executeMethod Duzhe.EditorTools.ChapterFlowPlayTest.RunFromCommandLine
    /// Starts CH01 in MainRun and completes chapter after chapter with the
    /// DEV shortcut, asserting each next chapter really starts, and that the
    /// chain ends in the start menu only after CH05. Save writes are
    /// redirected to a temp folder, so the player's save is never touched.
    public static class ChapterFlowPlayTest
    {
        private const string KeyActive = "Duzhe.ChapterFlowPlayTest.Active";
        private const string KeyPhase = "Duzhe.ChapterFlowPlayTest.Phase";
        private const string ReportPath = "Logs/chapter_flow_playtest.txt";

        [MenuItem("渡者/章节连续流转验证(Play)")]
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
            SessionState.SetFloat("Duzhe.ChapterFlowPlayTest.Started", 0f);
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
            double started = SessionState.GetFloat("Duzhe.ChapterFlowPlayTest.Started", 0f);
            if (started <= 0)
            {
                SessionState.SetFloat("Duzhe.ChapterFlowPlayTest.Started", (float)EditorApplication.timeSinceStartup);
            }
            else if (EditorApplication.timeSinceStartup - started > 600.0)
            {
                try { File.AppendAllText(ReportPath, "TIMEOUT: play session exceeded 600s\nDZ_CHAPTER_FLOW=FAIL\n", Encoding.UTF8); }
                catch { }
                Debug.LogError("DZ_CHAPTER_FLOW=FAIL (timeout)");
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(1);
                return;
            }
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("ChapterFlowPlayTestDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private readonly StringBuilder report = new StringBuilder();
            private bool finished;
            private bool ok = true;

            private void Start()
            {
                report.AppendLine("== chapter continuity verification ==");
                report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                StartCoroutine(Run());
            }

            private void Check(bool condition, string what)
            {
                string line = (condition ? "PASS " : "FAIL ") + what;
                report.AppendLine(line);
                try { File.AppendAllText(ReportPath, line + "\n", Encoding.UTF8); }
                catch { }
                if (!condition) ok = false;
            }

            private IEnumerator Run()
            {
                GameSession session = null;
                float wait = 0f;
                while (session == null && wait < 15f)
                {
                    session = GameSession.Instance;
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (session == null || session.Chapters == null)
                {
                    Check(false, "GameSession/ChapterDirector never appeared.");
                    Finish();
                    yield break;
                }

                // Keep every write of this probe out of the player's save.
                Core.SaveService.TestSaveDirectoryOverride =
                    Path.Combine(Path.GetTempPath(), "DuzheChapterFlowProbe");
                yield return Wait(0.4f);

                Check(Chapters.ChapterCatalog.GetNextFormalChapterId("CH01") == "CH02" &&
                      Chapters.ChapterCatalog.GetNextFormalChapterId("CH04") == "CH05" &&
                      Chapters.ChapterCatalog.GetNextFormalChapterId("CH05") == "",
                    "Chapter chain order is CH01->CH02->...->CH05->end.");

                session.Chapters.StartChapter("CH01", GameMode.MainRun);
                yield return Wait(0.6f);
                Check(session.Chapters.CurrentChapterId == "CH01",
                    "Main run starts at CH01 (current=" + session.Chapters.CurrentChapterId + ").");

                string[] chain = { "CH02", "CH03", "CH04", "CH05" };
                foreach (string expected in chain)
                {
                    session.Chapters.DevMarkChapterCompleted();
                    yield return Wait(0.8f);
                    Check(session.Chapters.CurrentChapterId == expected,
                        "Completing the previous chapter continues to " + expected +
                        " (current=" + session.Chapters.CurrentChapterId + ").");
                    Check(session.Mode == GameMode.MainRun,
                        expected + " runs in MainRun (mode=" + session.Mode + ").");
                }

                // CH05 is the last chapter: its completion returns to the menu.
                if (session.Presentations != null) session.Presentations.ForceAbort();
                session.Chapters.DevMarkChapterCompleted();
                yield return Wait(1.2f);
                Check(session.Mode == GameMode.MainMenu,
                    "Finishing CH05 returns to the start menu (mode=" + session.Mode + ").");
                Check(session.Transitions == null || session.Transitions.CurrentSceneId == "MainMenu",
                    "The start menu scene is active after the final chapter.");

                // Replaying a completed first chapter must still flow into
                // CH02. It remains a replay run and does not alter progress.
                session.Chapters.StartChapter("CH01", GameMode.ChapterReplay);
                yield return Wait(0.6f);
                session.Chapters.DevMarkChapterCompleted();
                yield return Wait(0.8f);
                Check(session.Chapters.CurrentChapterId == "CH02",
                    "A replayed CH01 continues directly to CH02 (current=" + session.Chapters.CurrentChapterId + ").");
                Check(session.Mode == GameMode.ChapterReplay,
                    "The second playthrough stays in ChapterReplay mode.");
                foreach (string expected in new[] { "CH03", "CH04", "CH05" })
                {
                    session.Presentations.ForceAbort(); session.Dialogue.ForceStop();
                    session.Chapters.DevMarkChapterCompleted();
                    yield return Wait(0.8f);
                    Check(session.Chapters.CurrentChapterId == expected && session.Mode == GameMode.ChapterReplay,
                        "Replayed previous chapter continues directly to " + expected);
                }
                session.Presentations.ForceAbort(); session.Dialogue.ForceStop();
                session.Chapters.DevMarkChapterCompleted(); yield return Wait(1.2f);
                Check(session.Mode == GameMode.MainMenu && session.PendingMenuTab == MenuTab.Home,
                    "Replayed CH05 returns to Home, never the chapter picker");
                yield return VerifyPenLifts(session);
                yield return VerifyScenePictures(session);

                Core.SaveService.TestSaveDirectoryOverride = null;
                Finish();
            }

            private IEnumerator VerifyScenePictures(GameSession session)
            {
                session.Chapters.StartChapter("CH01",GameMode.ChapterReplay);yield return Wait(.7f);
                session.Dialogue.ForceStop();
                var cfg=Duzhe.Chapters.ChapterCatalog.GetChapter("CH01");
                int river=Array.FindIndex(cfg.stages,s=>s.stageId=="CH01_River");
                typeof(Duzhe.Chapters.ChapterDirector).GetMethod("EnterStage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(session.Chapters,new object[]{river});
                yield return Wait(1.2f);
                var boat=UnityEngine.Object.FindObjectOfType<Duzhe.River.BoatController>();
                Check(boat!=null && Mathf.Abs(boat.transform.localScale.x-.72f)<.01f,"compact ferry scale is applied in scene");
                Check(GameObject.Find("RiverPassenger_0")!=null&&GameObject.Find("RiverPassenger_1")!=null,"both grandparents present on compact ferry");
                CaptureWorld("polish4_ferry");
                session.ReturnToMenu(MenuTab.Home);yield return Wait(.7f);
                session.Chapters.StartChapter("CH04",GameMode.ChapterReplay);yield return Wait(.7f);
                session.Dialogue.ForceStop();
                cfg=Duzhe.Chapters.ChapterCatalog.GetChapter("CH04");
                int talk=Array.FindIndex(cfg.stages,s=>s.sceneId=="Classroom");
                typeof(Duzhe.Chapters.ChapterDirector).GetMethod("EnterStage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(session.Chapters,new object[]{talk});
                yield return Wait(1.2f);CaptureWorld("polish4_classroom");
                session.ReturnToMenu(MenuTab.Home);yield return Wait(.5f);
            }
            private void CaptureWorld(string name)
            {
                Camera cam=Duzhe.Core.GameBootstrap.Instance.CameraFollow.GetComponent<Camera>();
                var rt=new RenderTexture(1280,720,24);var previous=cam.targetTexture;var old=RenderTexture.active;
                cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                File.WriteAllBytes("Logs/"+name+".png",tex.EncodeToPNG());
                cam.targetTexture=previous;RenderTexture.active=old;UnityEngine.Object.Destroy(tex);rt.Release();UnityEngine.Object.Destroy(rt);
            }

            private IEnumerator VerifyPenLifts(GameSession session)
            {
                foreach (string id in new[] { "MG01_Cane", "MG04_Heart", "MG07_Goodbye" })
                {
                    var cfg = Duzhe.Minigames.MinigameCatalog.Get(id);
                    session.Minigames.Open(cfg); yield return Wait(0.1f);
                    var panel = UnityEngine.Object.FindObjectOfType<Duzhe.Minigames.TraceMinigame>();
                    var field = typeof(Duzhe.Minigames.TraceMinigame).GetField("drawArea", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var area = (RectTransform)field.GetValue(panel);
                    var strokes = Duzhe.Minigames.TraceTemplates.Get(cfg.templateId);
                    foreach (var stroke in strokes)
                    {
                        var points = new System.Collections.Generic.List<Vector2>();
                        for (int k=1;k<stroke.points.Count;k++)
                            for(int j=0;j<16;j++) points.Add(Vector2.Lerp(stroke.points[k-1],stroke.points[k],j/16f));
                        points.Add(stroke.points[stroke.points.Count-1]);
                        bool penDown=false;
                        for(int k=0;k<points.Count;k++)
                        {
                            Vector2 norm=points[k];
                            var local=new Vector3((norm.x-.5f)*area.rect.width,(norm.y-1f)*area.rect.height);
                            var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                            e.position=RectTransformUtility.WorldToScreenPoint(null,area.TransformPoint(local));
                            if(!penDown){panel.OnPointerDown(e);penDown=true;} else panel.OnDrag(e);
                            if(k==points.Count/4 || k==points.Count/2 || k==points.Count-1){panel.OnPointerUp(e);penDown=false;}
                        }
                        yield return Wait(0.05f);
                    }
                    Check(!session.Minigames.IsOpen,id+" completes through real pointer handlers with two lifts per stroke");
                    session.Minigames.Close();yield return Wait(.1f);
                }
            }

            private IEnumerator Wait(float seconds)
            {
                float t = 0f;
                while (t < seconds)
                {
                    GameSession session = GameSession.Instance;
                    if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                    {
                        session.Pause.Close();
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                report.AppendLine();
                report.AppendLine(ok ? "DZ_CHAPTER_FLOW=PASS" : "DZ_CHAPTER_FLOW=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] flow report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_CHAPTER_FLOW=PASS");
                else Debug.LogError("DZ_CHAPTER_FLOW=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
