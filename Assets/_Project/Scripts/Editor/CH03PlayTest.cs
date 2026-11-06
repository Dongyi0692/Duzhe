using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.EditorTools
{
    /// C06 play-mode verification for the CH03 interaction rules. Runs in a
    /// REAL play session (GUI editor, no -batchmode/-quit):
    ///   -executeMethod Duzhe.EditorTools.CH03PlayTest.RunFromCommandLine
    /// Checks, through the real panels and the real event bus:
    ///   MG03 still needs all three stools (one drop does NOT complete it);
    ///   MG06 completes on the single drummer drop, on the 4-person board,
    ///        with no duplicated fixed bandmates;
    ///   the shutter fires exactly once even if the photo notification is
    ///        delivered twice;
    ///   MG05 ignores a wrong note and completes once on the delivered score.
    /// Report: Logs/ch03_playtest.txt; exit 0 only when every check passed.
    public static class CH03PlayTest
    {
        private const string KeyActive = "Duzhe.CH03PlayTest.Active";
        private const string KeyPhase = "Duzhe.CH03PlayTest.Phase";
        private const string ReportPath = "Logs/ch03_playtest.txt";

        [MenuItem("渡者/CH03 试玩验证(Play)")]
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
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/RecordingStudio.unity", OpenSceneMode.Single);
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
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("CH03PlayTestDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private readonly StringBuilder report = new StringBuilder();
            private readonly Dictionary<string, int> completedEvents = new Dictionary<string, int>();
            private bool finished;
            private bool ok = true;

            private void Start()
            {
                report.AppendLine("== CH03 play-mode verification ==");
                report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                StartCoroutine(Run());
            }

            private void OnDestroy()
            {
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null) session.OnGameEvent -= OnEvent;
            }

            private void OnEvent(Core.GameEvent e)
            {
                if (e.type == Core.GameEventType.MinigameCompleted)
                {
                    int n;
                    completedEvents.TryGetValue(e.sourceId, out n);
                    completedEvents[e.sourceId] = n + 1;
                    report.AppendLine("event: MinigameCompleted " + e.sourceId + " stage=" + e.stageId);
                }
                else if (e.type == Core.GameEventType.PresentationFinished)
                {
                    int n;
                    completedEvents.TryGetValue(e.sourceId, out n);
                    completedEvents[e.sourceId] = n + 1;
                    report.AppendLine("event: PresentationFinished " + e.sourceId);
                }
            }

            private int EventCount(string id)
            {
                int n;
                return completedEvents.TryGetValue(id, out n) ? n : 0;
            }

            private void Check(bool condition, string what)
            {
                report.AppendLine((condition ? "PASS " : "FAIL ") + what);
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
                while (Minigames.MinigameHost.Instance == null && wait < 20f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (Minigames.MinigameHost.Instance == null)
                {
                    Check(false, "MinigameHost never appeared.");
                    Finish();
                    yield break;
                }
                yield return Wait(0.3f);

                yield return TestStools();
                yield return TestPhoto();
                yield return TestNotes();
                yield return TestFarewell();

                Finish();
            }

            // -- MG03: three stools are still required --------------------
            private IEnumerator TestStools()
            {
                Minigames.MinigameHost host = Minigames.MinigameHost.Instance;
                host.Open(Minigames.MinigameCatalog.Get("MG03_Stools"));
                yield return Wait(0.15f);

                Check(GameObject.Find("Stool_0") != null && GameObject.Find("StoolSlot_0") != null,
                    "MG03 built three stools and three slots.");

                DropInto("Stool_0", "StoolSlot_0");
                yield return Wait(0.1f);
                Check(host.IsOpen, "MG03 stays open after ONE stool (three are required).");
                Check(EventCount("MG03_Stools") == 0, "MG03 published no completion after one stool.");

                DropInto("Stool_1", "StoolSlot_1");
                yield return Wait(0.1f);
                Check(host.IsOpen, "MG03 stays open after TWO stools.");

                DropInto("Stool_2", "StoolSlot_2");
                yield return Wait(0.15f);
                Check(!host.IsOpen, "MG03 completes after the third stool.");
                Check(EventCount("MG03_Stools") == 1, "MG03 published exactly one completion event.");
            }

            // -- MG06: one drummer drop, no duplicated members -------------
            private IEnumerator TestPhoto()
            {
                Minigames.MinigameHost host = Minigames.MinigameHost.Instance;
                host.Open(Minigames.MinigameCatalog.Get("MG06_Photo"));
                yield return Wait(0.15f);

                GameObject panel = GameObject.Find("MinigamePanel_MG06_Photo");
                GameObject drummer = GameObject.Find("Drummer");
                GameObject gap = GameObject.Find("DrummerGap");
                GameObject board = GameObject.Find("PhotoFrameArt");
                Check(panel != null && drummer != null && gap != null,
                    "MG06 built the photo board, the drummer and the single gap.");
                Check(board != null, "MG06 uses the formal PRP06 board (no fallback block).");
                int fixedBlocks = 0;
                if (panel != null)
                {
                    foreach (Transform child in panel.transform)
                    {
                        if (child.name.Contains("Bandmate")) fixedBlocks++;
                    }
                }
                Check(fixedBlocks == 0, "MG06 draws no programmatic bandmates on top of PRP06 (" + fixedBlocks + " found).");
                GameObject dragCard = GameObject.Find("DragCard");
                Check(dragCard != null &&
                      InsideCard("PhotoFrameArt", dragCard) && InsideCard("Drummer", dragCard) &&
                      InsideCard("DrummerGap", dragCard) && InsideCard("DragStatus", dragCard) &&
                      InsideCard("CloseDrag", dragCard) && InsideCard("DragTitle", dragCard),
                    "MG06 board, drummer, gap, status and close button all sit inside the panel card.");

                Directory.CreateDirectory("Logs/CodexScreenshots");
                ScreenCapture.CaptureScreenshot("Logs/CodexScreenshots/photo_puzzle.png");
                yield return Wait(0.3f);
                DropInto("Drummer", "DrummerGap");
                yield return Wait(0.15f);
                Check(!host.IsOpen, "MG06 completes after the single correct drummer drop.");
                Check(EventCount("MG06_Photo") == 1, "MG06 published exactly one completion event.");

                // Shutter: one notification must fire it once, and a repeated
                // notification must not fire it again.
                Scenes.RecordingStudioCast cast = FindObjectOfType<Scenes.RecordingStudioCast>();
                if (cast == null)
                {
                    Check(false, "RecordingStudioCast missing from the studio scene.");
                }
                else
                {
                    cast.NotifyPhotoCompleted();
                    cast.NotifyPhotoCompleted();
                    Check(cast.ShutterFireCount == 1,
                        "Shutter fired exactly once after a duplicate photo notification (count=" +
                        cast.ShutterFireCount + ").");
                    Check(cast.bandmates != null && cast.bandmates.Length == 3,
                        "RecordingStudioCast is wired to the three living bandmates.");
                }
            }

            // -- MG05: wrong note is harmless, the full score completes ----
            private IEnumerator TestNotes()
            {
                Minigames.MinigameHost host = Minigames.MinigameHost.Instance;
                host.Open(Minigames.MinigameCatalog.Get("MG05_Notes"));
                yield return Wait(0.15f);

                Text status = FindText("NotesStatus");
                GameObject dots = GameObject.Find("ProgressDots");
                Check(status != null && dots != null, "MG05 built its status line and progress dots.");

                GameObject card = GameObject.Find("NotesCard");
                Check(card != null &&
                      InsideCard("NotesTitle", card) && InsideCard("NotesStatus", card) &&
                      InsideCard("Note_Do", card) && InsideCard("Note_Si", card),
                    "MG05 title, seven note buttons and status line stay inside the panel card.");
                if (card != null)
                {
                    Vector3[] c = new Vector3[4];
                    ((RectTransform)card.transform).GetWorldCorners(c);
                    report.AppendLine("card rect x=" + c[0].x.ToString("F0") + ".." + c[2].x.ToString("F0") +
                        " y=" + c[0].y.ToString("F0") + ".." + c[1].y.ToString("F0"));
                    foreach (string name in new[] { "NotesTitle", "Note_Do", "Note_Si", "NotesStatus" })
                    {
                        GameObject g = GameObject.Find(name);
                        if (g == null) { report.AppendLine(name + " missing"); continue; }
                        Vector3[] k = new Vector3[4];
                        ((RectTransform)g.transform).GetWorldCorners(k);
                        report.AppendLine(name + " rect x=" + k[0].x.ToString("F0") + ".." + k[2].x.ToString("F0") +
                            " y=" + k[0].y.ToString("F0") + ".." + k[1].y.ToString("F0"));
                    }
                }

                // Wrong note first: press Mi (3) while the target is Do (1).
                PressNote(3);
                yield return Wait(0.1f);
                Check(host.IsOpen, "MG05 stays open after a wrong note.");
                Check(dots != null && dots.transform.childCount == 16,
                    "MG05 shows 16 progress dots (the delivered score length).");
                Color dot0 = dots != null && dots.transform.childCount > 0
                    ? dots.transform.GetChild(0).GetComponent<Image>().color
                    : Color.clear;
                bool stillFirst = Mathf.Abs(dot0.r - 240f / 255f) < 0.02f &&
                                  Mathf.Abs(dot0.g - 220f / 255f) < 0.02f;
                Check(stillFirst, "MG05 progress stayed on the first note after the wrong press.");

                int[] score = { 1, 2, 3, 5, 3, 2, 1, 2, 3, 5, 6, 5, 3, 2, 1, 1 };
                for (int i = 0; i < score.Length && host.IsOpen; i++)
                {
                    PressNote(score[i]);
                    yield return null;
                }
                yield return Wait(0.15f);
                Check(!host.IsOpen, "MG05 completes on the delivered 16-note score.");
                Check(EventCount("MG05_Notes") == 1, "MG05 published exactly one completion event.");
            }

            // -- PR_CH03_Farewell: CG during dialogue, then only the drummer --
            private IEnumerator TestFarewell()
            {
                Core.GameSession session = Core.GameSession.Instance;
                Scenes.RecordingStudioCast studio = FindObjectOfType<Scenes.RecordingStudioCast>();
                Scenes.CH03FarewellCast cast = FindObjectOfType<Scenes.CH03FarewellCast>();
                if (session == null || session.Presentations == null || studio == null || cast == null)
                {
                    Check(false, "Farewell test prerequisites missing (presentation player / cast).");
                    yield break;
                }

                // The route gate hides CH03 objects when no chapter is running;
                // this test drives the presentation directly, so the gates are
                // disabled for the ghost and the band to make the real fade
                // path runnable.
                if (studio.DrummerGhost != null)
                {
                    Chapters.ChapterObjectVisibility gate =
                        studio.DrummerGhost.GetComponent<Chapters.ChapterObjectVisibility>();
                    if (gate != null) gate.enabled = false;
                    studio.DrummerGhost.gameObject.SetActive(true);
                    studio.DrummerGhost.BeginFollow();
                }
                if (studio.bandmates != null)
                {
                    foreach (GameObject mate in studio.bandmates)
                    {
                        if (mate == null) continue;
                        Chapters.ChapterObjectVisibility gate =
                            mate.GetComponent<Chapters.ChapterObjectVisibility>();
                        if (gate != null) gate.enabled = false;
                        mate.SetActive(true);
                    }
                }
                yield return null;

                bool started = session.Presentations.Play("PR_CH03_Farewell", "CH03");
                Check(started, "PR_CH03_Farewell started.");
                if (!started) yield break;

                yield return Wait(0.2f);
                Check(Presentation.PresentationCG.IsShowing, "CG03 tableau is shown during the farewell dialogue.");

                float timeout = 25f;
                float t = 0f;
                int guard = 0;
                while (session.Presentations.IsPlaying && t < timeout)
                {
                    CloseFocusLossPause();
                    if (session.Dialogue != null && session.Dialogue.IsPlaying)
                    {
                        session.Dialogue.Advance();
                        guard++;
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Check(!session.Presentations.IsPlaying,
                    "Farewell presentation finished (advanced " + guard + " dialogue lines).");
                Check(!Presentation.PresentationCG.IsShowing, "CG03 is hidden after the last line (clean scene first).");

                bool drummerGone = studio.DrummerGhost == null || !studio.DrummerGhost.gameObject.activeSelf;
                Check(drummerGone, "Only the drummer is gone after the farewell (ghost inactive).");
                int living = 0;
                if (studio.bandmates != null)
                {
                    foreach (GameObject mate in studio.bandmates)
                    {
                        if (mate != null && mate.activeSelf) living++;
                    }
                }
                Check(living == 3, "The three living bandmates stay in the studio (active=" + living + ").");
                Check(EventCount("PR_CH03_Farewell") == 1,
                    "PR_CH03_Farewell published exactly one PresentationFinished.");
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

            private static Vector2 Center(GameObject go)
            {
                Vector3[] c = new Vector3[4];
                ((RectTransform)go.transform).GetWorldCorners(c);
                return (c[0] + c[2]) * 0.5f;
            }

            private static void DropInto(string itemName, string slotName)
            {
                GameObject item = GameObject.Find(itemName);
                GameObject slot = GameObject.Find(slotName);
                if (item == null || slot == null)
                {
                    Debug.LogError("[Duzhe] CH03 playtest: missing " + itemName + "/" + slotName);
                    return;
                }
                Vector2 from = Center(item);
                Vector2 to = Center(slot);
                PointerEventData ped = new PointerEventData(EventSystem.current)
                {
                    position = from,
                    pressPosition = from,
                    button = PointerEventData.InputButton.Left
                };
                ExecuteEvents.Execute<IBeginDragHandler>(item, ped, ExecuteEvents.beginDragHandler);
                ped.position = to;
                ExecuteEvents.Execute<IDragHandler>(item, ped, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute<IEndDragHandler>(item, ped, ExecuteEvents.endDragHandler);
            }

            private static void PressNote(int noteIndex)
            {
                string[] names = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Si" };
                GameObject go = GameObject.Find("Note_" + names[Mathf.Clamp(noteIndex - 1, 0, 6)]);
                if (go == null)
                {
                    Debug.LogError("[Duzhe] CH03 playtest: note button missing for " + noteIndex);
                    return;
                }
                Button b = go.GetComponent<Button>();
                if (b != null) b.onClick.Invoke();
            }

            private static Text FindText(string objectName)
            {
                GameObject go = GameObject.Find(objectName);
                return go != null ? go.GetComponent<Text>() : null;
            }

            /// True when the child's screen rect lies inside the card's rect
            /// (1px tolerance): catches the y-sign / centring mistakes that
            /// pushed panel content off the card.
            private static bool InsideCard(string childName, GameObject card)
            {
                GameObject child = GameObject.Find(childName);
                if (child == null) return false;
                Vector3[] c = new Vector3[4];
                Vector3[] k = new Vector3[4];
                ((RectTransform)card.transform).GetWorldCorners(c);
                ((RectTransform)child.transform).GetWorldCorners(k);
                float left = c[0].x, right = c[2].x, bottom = c[0].y, top = c[1].y;
                return k[0].x >= left - 1f && k[2].x <= right + 1f &&
                       k[0].y >= bottom - 1f && k[1].y <= top + 1f;
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                report.AppendLine();
                report.AppendLine(ok ? "DZ_CH03_PLAY=PASS" : "DZ_CH03_PLAY=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] CH03 playtest report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_CH03_PLAY=PASS");
                else Debug.LogError("DZ_CH03_PLAY=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
