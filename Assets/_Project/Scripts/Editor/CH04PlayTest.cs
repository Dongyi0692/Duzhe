using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Minigames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.EditorTools
{
    /// C07 play-mode verification for CH04. Runs in a REAL play session (GUI
    /// editor, no -batchmode/-quit):
    ///   -executeMethod Duzhe.EditorTools.CH04PlayTest.RunFromCommandLine
    /// Checks, through the real panels, the real cast and the real event bus:
    ///   MG01/MG04 still build inside their card (no regression from the
    ///        TraceMinigame chrome changes);
    ///   the blackboard gate never opens outside CH04_WriteGoodbye;
    ///   MG07 builds on the PRP07 board with the writing area inside it and
    ///        above the button band, and the full "再见" can actually be drawn;
    ///   PR_CH04_Farewell plays ONLY the farewell in the main path, and the
    ///        full lesson+farewell in a presentation replay (with the written
    ///        text appearing between them), fading only the teacher.
    /// Report: Logs/ch04_playtest.txt; exit 0 only when every check passed.
    public static class CH04PlayTest
    {
        private const string KeyActive = "Duzhe.CH04PlayTest.Active";
        private const string KeyPhase = "Duzhe.CH04PlayTest.Phase";
        private const string ReportPath = "Logs/ch04_playtest.txt";

        [MenuItem("渡者/CH04 试玩验证(Play)")]
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
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Classroom.unity", OpenSceneMode.Single);
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
            // Watchdog: a stuck play session must never keep the editor open
            // forever (the run still leaves its incremental report behind).
            double started = SessionState.GetFloat("Duzhe.CH04PlayTest.Started", 0f);
            if (started <= 0)
            {
                SessionState.SetFloat("Duzhe.CH04PlayTest.Started", (float)EditorApplication.timeSinceStartup);
            }
            else if (EditorApplication.timeSinceStartup - started > 600.0)
            {
                try
                {
                    File.AppendAllText(ReportPath, "TIMEOUT: play session exceeded 600s\nDZ_CH04_PLAY=FAIL\n", Encoding.UTF8);
                }
                catch { }
                Debug.LogError("DZ_CH04_PLAY=FAIL (timeout)");
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(1);
                return;
            }
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                Debug.Log("[Duzhe] CH04PlayTest: creating driver (playing=" + EditorApplication.isPlaying + ")");
                GameObject go = new GameObject("CH04PlayTestDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private readonly StringBuilder report = new StringBuilder();
            private readonly Dictionary<string, int> events = new Dictionary<string, int>();
            private bool finished;
            private bool ok = true;

            private void Start()
            {
                report.AppendLine("== CH04 play-mode verification ==");
                report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Debug.Log("[Duzhe] CH04PlayTest: driver Start");
                StartCoroutine(Run());
            }

            private void OnDestroy()
            {
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null) session.OnGameEvent -= OnEvent;
            }

            private void OnEvent(Core.GameEvent e)
            {
                if (e.type != Core.GameEventType.MinigameCompleted &&
                    e.type != Core.GameEventType.PresentationFinished) return;
                int n;
                events.TryGetValue(e.sourceId, out n);
                events[e.sourceId] = n + 1;
                report.AppendLine("event: " + e.type + " " + e.sourceId + " stage=" + e.stageId);
            }

            private int EventCount(string id)
            {
                int n;
                return events.TryGetValue(id, out n) ? n : 0;
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
                Core.GameSession session = null;
                float wait = 0f;
                while (session == null && wait < 15f)
                {
                    session = Core.GameSession.Instance;
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (session == null)
                {
                    Check(false, "GameSession never appeared.");
                    Finish();
                    yield break;
                }
                session.OnGameEvent += OnEvent;
                while (MinigameHost.Instance == null && wait < 20f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (MinigameHost.Instance == null)
                {
                    Check(false, "MinigameHost never appeared.");
                    Finish();
                    yield break;
                }
                yield return Wait(0.3f);

                yield return TestTracePanels();
                yield return TestGateRefusesOutsideStage();
                yield return TestWriteGoodbye();
                yield return TestFarewellMainPath();
                yield return TestFarewellReplay();
                yield return TestDevChapterPicker();

                Finish();
            }

            // -- DEV chapter picker (unblocks entering finished chapters) ---
            private IEnumerator TestDevChapterPicker()
            {
                Core.GameSession session = Core.GameSession.Instance;
                // Gallery completion loads MainMenu asynchronously; its
                // scene-ready cleanup must finish before opening pause.
                yield return Wait(1.0f);
                float settle = 0f;
                while (session.Transitions != null && session.Transitions.IsTransitioning && settle < 20f)
                {
                    settle += Time.unscaledDeltaTime;
                    yield return null;
                }
                // Menu routing: unfinished -> the next chapter; when EVERY
                // chapter is complete the menu replays CH01 (spec 9).
                string next = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(session.Save);
                bool allDone = session.Save != null && session.Save.HasCompletedChapter("CH05");
                string expected = allDone ? "CH01" : "CH05";
                report.AppendLine("  menu next chapter with current save: " + next +
                    " (allDone=" + allDone + ")");
                Check(next == expected,
                    "Menu routing matches the save state (got " + next + ", expected " + expected + ").");

                // Keep the probe's writes out of the player's real save.
                Core.SaveService.TestSaveDirectoryOverride =
                    Path.Combine(Path.GetTempPath(), "DuzhePickerProbe");
                session.Pause.Open();
                // NOTE: Wait() deliberately closes any open pause (focus-loss
                // auto-pause), so this test uses raw frame waits instead.
                yield return null;
                yield return null;
                Check(ClickButton("DevChapters"), "Pause menu offers DEV:选择章节.");
                yield return null;
                yield return null;
                GameObject picker = GameObject.Find("DevChapterPicker");
                Check(picker != null && picker.activeSelf, "DEV chapter picker opened.");
                Check(GameObject.Find("Pick_CH02") != null, "DEV chapter picker lists CH02.");

                // First hop: Classroom -> Underworld (a real scene load).
                Check(ClickButton("Pick_CH03"), "DEV picker CH03 button clicked.");
                yield return WaitRaw(2.0f);
                Check(session.Chapters.CurrentChapterId == "CH03",
                    "DEV picker actually starts CH03 (current=" + session.Chapters.CurrentChapterId + ").");
                GameObject drummer = GameObject.Find("NPC_CH03_DRUMMER");
                Check(drummer != null && drummer.activeSelf,
                    "CH03 drummer is visible after entering CH03.");

                // Second hop: Underworld -> Underworld (SAME scene). This is
                // the reported regression: route gates unsubscribed when they
                // hid themselves, so the next chapter's actors never came
                // back and the world looked empty.
                session.Pause.Open();
                yield return null;
                yield return null;
                Check(ClickButton("DevChapters"), "Pause menu reopens the DEV picker.");
                yield return null;
                yield return null;
                Check(ClickButton("Pick_CH04"), "DEV picker CH04 button clicked.");
                yield return WaitRaw(1.0f);
                Check(session.Chapters.CurrentChapterId == "CH04",
                    "DEV picker switches to CH04 in the SAME scene (current=" + session.Chapters.CurrentChapterId + ").");
                GameObject teacher = GameObject.Find("NPC_CH04_TEACHER");
                Check(teacher != null && teacher.activeSelf,
                    "CH04 teacher is visible after a same-scene chapter switch.");
                Check(drummer == null || !drummer.activeSelf,
                    "CH03 drummer is hidden again after switching to CH04.");
                Core.SaveService.TestSaveDirectoryOverride = null;
            }

            private static bool ClickButton(string objectName)
            {
                GameObject go = GameObject.Find(objectName);
                if (go == null) return false;
                Button b = go.GetComponent<Button>();
                if (b == null) return false;
                b.onClick.Invoke();
                return true;
            }

            /// Waits without touching the pause state (used while the pause
            /// menu itself is under test).
            private static IEnumerator WaitRaw(float seconds)
            {
                float t = 0f;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // -- MG01/MG04 regression --------------------------------------
            private IEnumerator TestTracePanels()
            {
                MinigameHost host = MinigameHost.Instance;
                foreach (string id in new[] { "MG01_Cane", "MG04_Heart" })
                {
                    host.Open(MinigameCatalog.Get(id));
                    yield return Wait(0.15f);
                    GameObject card = GameObject.Find("TraceCard");
                    GameObject draw = GameObject.Find("DrawArea");
                    Check(card != null && draw != null, id + " built its card and drawing area.");
                    Check(InsideCard("DrawArea", card) && InsideCard("TraceTitle", card) &&
                          InsideCard("Redraw", card) && InsideCard("CloseTrace", card),
                        id + " card, drawing area and buttons all sit inside the panel card.");
                    host.Close();
                    yield return Wait(0.1f);
                }
            }

            // -- the blackboard gate must stay shut outside the stage ------
            private IEnumerator TestGateRefusesOutsideStage()
            {
                // The blackboard is route-gated (inactive without a CH04 run),
                // so find it through the always-active scene cast.
                Scenes.ClassroomCast cast = FindObjectOfType<Scenes.ClassroomCast>();
                Scenes.CH04GoodbyeGate gate = cast != null && cast.blackboardProp != null
                    ? cast.blackboardProp.GetComponent<Scenes.CH04GoodbyeGate>()
                    : null;
                Check(gate != null, "The classroom blackboard carries CH04GoodbyeGate.");
                if (gate != null)
                {
                    bool opened = gate.OnPropInteract(null);
                    Check(!opened && !MinigameHost.Instance.IsOpen,
                        "Blackboard E does nothing while no CH04 writing stage is running.");
                }
                yield return Wait(0.1f);
            }

            // -- MG07: geometry + the real two characters ------------------
            private IEnumerator TestWriteGoodbye()
            {
                MinigameHost host = MinigameHost.Instance;
                host.Open(MinigameCatalog.Get("MG07_Goodbye"));
                yield return Wait(0.2f);

                GameObject card = GameObject.Find("TraceCard");
                GameObject board = GameObject.Find("BlackboardArt");
                GameObject draw = GameObject.Find("DrawArea");
                Check(board != null && draw != null && card != null, "MG07 built the PRP07 board and its drawing area.");
                if (board == null || draw == null || card == null) yield break;

                Check(InsideCard("BlackboardArt", card) && InsideCard("DrawArea", card) &&
                      InsideCard("TraceTitle", card) && InsideCard("TraceStatus", card),
                    "MG07 board, drawing area, title and status all sit inside the panel card.");
                Check(InsideRect("DrawArea", "BlackboardArt"),
                    "MG07 writing area lies inside the 640x360 blackboard image.");
                Check(AboveButtons("DrawArea", "Redraw", "CloseTrace"),
                    "MG07 writing area stays above the redraw/close button band.");

                // Draw every stroke of the delivered "再见" skeleton through the
                // real pointer path (down/drag/up on the DrawArea).
                List<TraceStroke> strokes = TraceTemplates.Get("Goodbye");
                int drawn = 0;
                // The pointer handlers live on the PANEL object (the panel
                // component owns them); ExecuteEvents.Execute only inspects
                // the target GameObject itself, so events must be sent there.
                GameObject panel = draw.transform.parent != null
                    ? draw.transform.parent.gameObject : draw;
                GameObject handler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(draw);
                report.AppendLine("  pointer handler: " + (handler != null ? handler.name : "(none)") +
                    " target=" + panel.name +
                    " paused=" + (Core.GameSession.Instance != null && Core.GameSession.Instance.Paused));
                foreach (TraceStroke stroke in strokes)
                {
                    if (!host.IsOpen) break;
                    CloseFocusLossPause();
                    yield return null;
                    DrawStroke(draw, panel, stroke.points);
                    drawn++;
                    yield return null;
                    Text status = FindText("TraceStatus");
                    report.AppendLine("  stroke " + drawn + " status: " + (status != null ? status.text : "(none)"));
                }
                yield return Wait(0.25f);
                Check(!host.IsOpen, "MG07 completes after drawing all " + strokes.Count + " strokes (drew " + drawn + ").");
                Check(EventCount("MG07_Goodbye") == 1, "MG07 published exactly one completion event.");
            }

            // -- PR_CH04_Farewell, main path: only the farewell ------------
            private IEnumerator TestFarewellMainPath()
            {
                Core.GameSession session = Core.GameSession.Instance;
                Scenes.ClassroomCast cast = FindObjectOfType<Scenes.ClassroomCast>();
                // The route gate hides CH04 objects when no chapter is running;
                // this test drives the presentation directly, so the gates are
                // disabled for the actors to make the real fade path runnable.
                PrepareActors(cast);
                yield return null;

                int before = EventCount("PR_CH04_Farewell");
                bool started = session.Presentations.Play("PR_CH04_Farewell", "CH04");
                Check(started, "PR_CH04_Farewell started (main path).");
                if (!started) yield break;

                yield return Wait(0.2f);
                Check(Presentation.PresentationCG.IsShowing, "CG04 tableau is shown during the farewell.");
                Check(Presentation.BlackboardTextLayer.IsShowing,
                    "The finished \u201c再见\u201d is on the CG04 board in the main path.");

                int lines = 0;
                float t = 0f;
                while (session.Presentations.IsPlaying && t < 60f)
                {
                    CloseFocusLossPause();
                    if (session.Dialogue != null && session.Dialogue.IsPlaying)
                    {
                        session.Dialogue.Advance();
                        lines++;
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Check(!session.Presentations.IsPlaying, "Farewell presentation finished.");
                Check(lines == 8, "Main path played ONLY the 8 farewell lines (played " + lines + "; the 21-line lesson was not repeated).");
                Check(!Presentation.PresentationCG.IsShowing && !Presentation.BlackboardTextLayer.IsShowing,
                    "CG04 and its text layer are hidden after the last line.");
                Check(cast != null && cast.teacher != null && !cast.teacher.activeSelf,
                    "Only the teacher is gone after the farewell.");
                Check(cast != null && cast.student != null && cast.student.activeSelf,
                    "The student stays in the classroom.");
                Check(EventCount("PR_CH04_Farewell") == before + 1,
                    "PR_CH04_Farewell published exactly one PresentationFinished (main path).");
            }

            /// Activates the classroom actors for a direct presentation test
            /// (their route gate would hide them outside a CH04 run).
            private static void PrepareActors(Scenes.ClassroomCast cast)
            {
                if (cast == null) return;
                foreach (GameObject actor in new[] { cast.student, cast.teacher })
                {
                    if (actor == null) continue;
                    Chapters.ChapterObjectVisibility gate =
                        actor.GetComponent<Chapters.ChapterObjectVisibility>();
                    if (gate != null) gate.enabled = false;
                    actor.SetActive(true);
                    SpriteRenderer sr = actor.GetComponentInChildren<SpriteRenderer>(true);
                    if (sr != null) sr.color = Color.white;
                }
            }

            // -- PR_CH04_Farewell, gallery replay: lesson then farewell ----
            private IEnumerator TestFarewellReplay()
            {
                Core.GameSession session = Core.GameSession.Instance;
                Scenes.ClassroomCast cast = FindObjectOfType<Scenes.ClassroomCast>();
                if (cast != null && cast.teacher != null)
                {
                    cast.teacher.SetActive(true);
                    SpriteRenderer sr = cast.teacher.GetComponentInChildren<SpriteRenderer>(true);
                    if (sr != null) sr.color = Color.white;
                }
                session.SetMode(Core.GameMode.PresentationReplay);
                int before = EventCount("PR_CH04_Farewell");
                bool started = session.Presentations.Play("PR_CH04_Farewell", "CH04");
                Check(started, "PR_CH04_Farewell started (presentation replay).");
                if (!started)
                {
                    session.SetMode(Core.GameMode.MainMenu);
                    yield break;
                }

                yield return Wait(0.2f);
                Check(!Presentation.BlackboardTextLayer.IsShowing,
                    "Replay starts with a BLANK board (the lesson has not played yet).");

                int lines = 0;
                bool textAppearedAfterLesson = false;
                float t = 0f;
                while (session.Presentations.IsPlaying && t < 120f)
                {
                    CloseFocusLossPause();
                    if (session.Dialogue != null && session.Dialogue.IsPlaying)
                    {
                        session.Dialogue.Advance();
                        lines++;
                        if (lines == 21)
                        {
                            yield return null; // let the between-segments hook run
                            textAppearedAfterLesson = Presentation.BlackboardTextLayer.IsShowing;
                        }
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Check(!session.Presentations.IsPlaying, "Replay presentation finished.");
                Check(lines == 29, "Replay played the lesson AND the farewell (29 lines, played " + lines + ").");
                Check(textAppearedAfterLesson,
                    "The written \u201c再见\u201d appears between the lesson and the farewell in the replay.");
                Check(EventCount("PR_CH04_Farewell") == before + 1,
                    "Replay published exactly one PresentationFinished.");
                session.SetMode(Core.GameMode.MainMenu);
            }

            // -- helpers ---------------------------------------------------
            private IEnumerator Wait(float seconds)
            {
                float t = 0f;
                while (t < seconds)
                {
                    CloseFocusLossPause();
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            private static void CloseFocusLossPause()
            {
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                {
                    session.Pause.Close();
                }
            }

            private static Vector3[] Corners(GameObject go)
            {
                Vector3[] c = new Vector3[4];
                ((RectTransform)go.transform).GetWorldCorners(c);
                return c;
            }

            private static Text FindText(string objectName)
            {
                GameObject go = GameObject.Find(objectName);
                return go != null ? go.GetComponent<Text>() : null;
            }

            private static bool InsideCard(string childName, GameObject card)
            {
                if (card == null) return false;
                return InsideRect(childName, card.name);
            }

            private static bool InsideRect(string childName, string containerName)
            {
                GameObject child = GameObject.Find(childName);
                GameObject container = GameObject.Find(containerName);
                if (child == null || container == null) return false;
                Vector3[] c = Corners(container);
                Vector3[] k = Corners(child);
                return k[0].x >= c[0].x - 1f && k[2].x <= c[2].x + 1f &&
                       k[0].y >= c[0].y - 1f && k[1].y <= c[1].y + 1f;
            }

            private static bool AboveButtons(string areaName, string buttonA, string buttonB)
            {
                GameObject area = GameObject.Find(areaName);
                GameObject a = GameObject.Find(buttonA);
                GameObject b = GameObject.Find(buttonB);
                if (area == null || a == null || b == null) return false;
                float areaBottom = Corners(area)[0].y;
                float buttonsTop = Mathf.Max(Corners(a)[1].y, Corners(b)[1].y);
                return areaBottom >= buttonsTop + 0.5f;
            }

            /// Converts one template point into a screen position on the real
            /// drawing area, mirroring TraceMinigame's own mapping.
            private static Vector2 ToScreen(GameObject drawArea, Vector2 normalized)
            {
                RectTransform rt = (RectTransform)drawArea.transform;
                // Same mapping as TraceMinigame.ToTex / TraceEvaluator:
                // normalized -> texture pixels with NO inset.
                const float texW = 480f, texH = 420f;
                float texX = Mathf.Clamp01(normalized.x) * texW;
                float texY = Mathf.Clamp01(normalized.y) * texH;
                float lx = rt.rect.width * (texX / texW - 0.5f);
                float ly = rt.rect.height * (texY / texH - 1f);
                Vector3 world = rt.TransformPoint(new Vector3(lx, ly, 0f));
                return RectTransformUtility.WorldToScreenPoint(null, world);
            }

            private static void DrawStroke(GameObject drawArea, GameObject target, List<Vector2> normalized)
            {
                if (normalized == null || normalized.Count < 2) return;
                // Emit dense intermediate points (~6 screen px apart) so the
                // evaluator's 20 stroke samples all find ink within the
                // corridor - the same thing a real mouse drag produces.
                List<Vector2> screen = new List<Vector2>();
                Vector2 prev = ToScreen(drawArea, normalized[0]);
                screen.Add(prev);
                for (int i = 1; i < normalized.Count; i++)
                {
                    Vector2 next = ToScreen(drawArea, normalized[i]);
                    float dist = Vector2.Distance(prev, next);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 6f));
                    for (int s = 1; s <= steps; s++)
                    {
                        screen.Add(Vector2.Lerp(prev, next, (float)s / steps));
                    }
                    prev = next;
                }

                PointerEventData ped = new PointerEventData(EventSystem.current)
                {
                    position = screen[0],
                    pressPosition = screen[0],
                    button = PointerEventData.InputButton.Left
                };
                ExecuteEvents.Execute<IPointerDownHandler>(target, ped, ExecuteEvents.pointerDownHandler);
                for (int i = 1; i < screen.Count; i++)
                {
                    ped.position = screen[i];
                    ExecuteEvents.Execute<IDragHandler>(target, ped, ExecuteEvents.dragHandler);
                }
                ExecuteEvents.Execute<IPointerUpHandler>(target, ped, ExecuteEvents.pointerUpHandler);
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                report.AppendLine();
                report.AppendLine(ok ? "DZ_CH04_PLAY=PASS" : "DZ_CH04_PLAY=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] CH04 playtest report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_CH04_PLAY=PASS");
                else Debug.LogError("DZ_CH04_PLAY=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
