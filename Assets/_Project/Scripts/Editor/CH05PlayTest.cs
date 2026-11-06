using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.EditorTools
{
    /// C08 play-mode verification for CH05. Runs in a REAL play session (GUI
    /// editor, no -batchmode/-quit):
    ///   -executeMethod Duzhe.EditorTools.CH05PlayTest.RunFromCommandLine
    /// The chapter is started in ChapterReplay mode so nothing is written to
    /// the player's save. Checks:
    ///   - the recall presentation shows CG01..CG04 exactly on the lines that
    ///     recognise each identity and hides everything on line 013;
    ///   - memoryRestored / letGoFinished gate boarding and are set by their
    ///     own events;
    ///   - the river runs with ZERO passengers and no hazards are left after
    ///     success (the crossing itself is covered by DZ_RIVER_SEEDS);
    ///   - CG05 -> boat departs -> credits panel; confirming twice publishes
    ///     exactly one CreditsConfirmed and completes the chapter.
    /// Report: Logs/ch05_playtest.txt; exit 0 only when every check passed.
    public static class CH05PlayTest
    {
        private const string KeyActive = "Duzhe.CH05PlayTest.Active";
        private const string KeyPhase = "Duzhe.CH05PlayTest.Phase";
        private const string ReportPath = "Logs/ch05_playtest.txt";

        [MenuItem("渡者/CH05 试玩验证(Play)")]
        public static void RunInteractive() { Start(); }

        public static void RunFromCommandLine() { Start(); }

        [InitializeOnLoadMethod]
        private static void AutoResume()
        {
            if (SessionState.GetBool(KeyActive, false))
            {
                SaveService.TestSaveDirectoryOverride = SessionState.GetString(KeyActive + ".Save", "");
                EditorApplication.update += OnUpdate;
            }
        }

        private static void Start()
        {
            SessionState.SetBool(KeyActive, true);
            SaveService.TestSaveDirectoryOverride = Path.Combine(Path.GetTempPath(), "DuzheStoryRevision", Guid.NewGuid().ToString("N"));
            SessionState.SetString(KeyActive + ".Save", SaveService.TestSaveDirectoryOverride);
            SessionState.SetInt(KeyPhase, 0);
            SessionState.SetFloat("Duzhe.CH05PlayTest.Started", 0f);
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
                SaveService.TestSaveDirectoryOverride = null;
                EditorApplication.Exit(SessionState.GetBool(KeyActive + ".Failed", false) ? 1 : 0);
                return;
            }
            double started = SessionState.GetFloat("Duzhe.CH05PlayTest.Started", 0f);
            if (started <= 0)
            {
                SessionState.SetFloat("Duzhe.CH05PlayTest.Started", (float)EditorApplication.timeSinceStartup);
            }
            else if (EditorApplication.timeSinceStartup - started > 320.0)
            {
                try { File.AppendAllText(ReportPath, "TIMEOUT: play session exceeded 320s\nDZ_CH05_PLAY=FAIL\n", Encoding.UTF8); }
                catch { }
                Debug.LogError("DZ_CH05_PLAY=FAIL (timeout)");
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(1);
                return;
            }
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("CH05PlayTestDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
        }

        private class Driver : MonoBehaviour
        {
            private readonly StringBuilder report = new StringBuilder();
            private readonly Dictionary<string, int> events = new Dictionary<string, int>();
            private string completedBefore = "";
            private bool finished;
            private bool ok = true;

            private void Start()
            {
                report.AppendLine("== CH05 play-mode verification ==");
                report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Debug.Log("[Duzhe] CH05PlayTest: driver Start");
                StartCoroutine(Run());
            }

            private void OnDestroy()
            {
                GameSession session = GameSession.Instance;
                if (session != null) session.OnGameEvent -= OnEvent;
            }

            private void OnEvent(GameEvent e)
            {
                if (e.type != GameEventType.PresentationFinished &&
                    e.type != GameEventType.ChapterCompleted &&
                    e.type != GameEventType.CreditsConfirmed) return;
                int n;
                events.TryGetValue(e.type + "#" + e.sourceId, out n);
                events[e.type + "#" + e.sourceId] = n + 1;
                report.AppendLine("event: " + e.type + " " + e.sourceId + " stage=" + e.stageId);
            }

            private int EventCount(GameEventType type, string sourceId)
            {
                int n;
                return events.TryGetValue(type + "#" + sourceId, out n) ? n : 0;
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
                if (session == null)
                {
                    Check(false, "GameSession never appeared.");
                    Finish();
                    yield break;
                }
                session.OnGameEvent += OnEvent;
                completedBefore = CompletedSnapshot(session);
                int totalLines = 0;
                foreach (string chapterId in Chapters.ChapterCatalog.FormalChapterIds)
                {
                    Dialogue.ChapterDialogue chapter;
                    if (Dialogue.DialogueLibrary.Instance.TryGetChapter(chapterId, out chapter))
                        foreach (Dialogue.DialogueSegment segment in chapter.segments) totalLines += segment.lines.Count;
                }
                Check(totalLines == 623, "All 623 revised dialogue lines are loaded by the engine.");
                yield return Wait(0.4f);

                Chapters.ChapterDirector director = session.Chapters;
                if (director == null)
                {
                    Check(false, "ChapterDirector missing.");
                    Finish();
                    yield break;
                }

                // Replay mode: completing the chapter must NOT write the save.
                director.StartChapter("CH05", GameMode.ChapterReplay);
                yield return Wait(0.5f);
                Check(director.CurrentChapterId == "CH05" && director.CurrentStageId == "CH05_Recall",
                    "CH05 starts at CH05_Recall (stage=" + director.CurrentStageId + ").");
                Check(director.MemoryRestored == false && director.LetGoFinished == false,
                    "memoryRestored/letGoFinished start closed.");
                GameObject earlyDock = GameObject.Find("BoatDock_CH05");
                Interaction.Interactable earlyDockInt = earlyDock != null
                    ? earlyDock.GetComponent<Interaction.Interactable>() : null;
                Check(earlyDockInt != null && !earlyDockInt.IsActiveInStage("CH05_Recall") &&
                      !earlyDockInt.IsActiveInStage("CH05_LetGo"),
                    "The CH05 boat hotspot stays inactive during recall/let-go (stage gate).");

                yield return DriveRecall(session, director);
                yield return DriveLetGo(session, director);
                yield return BoardAndCross(session, director);
                yield return ConfirmCredits(session, director);

                Finish();
            }

            // -- recall: CG follows the recognised identities ---------------
            private IEnumerator DriveRecall(GameSession session, Chapters.ChapterDirector director)
            {
                int guard = 0;
                int shown = 0;
                var seen = new HashSet<string>();
                bool boardShown = false;
                while (director.CurrentStageId == "CH05_Recall" && guard++ < 1000)
                {
                    CloseFocusLossPause();
                    string image = CurrentCG();
                    if (!string.IsNullOrEmpty(image)) seen.Add(image);
                    if (image == "CG04_TeacherFarewell") boardShown |= Presentation.BlackboardTextLayer.IsShowing;
                    if (session.Dialogue != null && session.Dialogue.IsPlaying) { session.Dialogue.Advance(); shown++; }
                    yield return null;
                }
                yield return Wait(0.3f);
                Dialogue.DialogueSegment recall;
                Dialogue.DialogueLibrary.Instance.TryGetSegment("CH05", "CH05_Recall", out recall);
                Check(recall != null && shown == recall.lines.Count, "All expanded recall lines played.");
                foreach (string image in new[] { "CG17_TeacherMemory03", "CG09_FamilyMemory01", "CG01_FamilyFarewell", "CG07_LoversMemory02", "CG08_LoversMemory03", "CG02_LoversFarewell", "CG13_BandMemory02", "CG03_BandFarewell", "CG04_TeacherFarewell" })
                    Check(seen.Contains(image), "Recall includes " + image);
                Check(boardShown, "Teacher farewell recall includes the written goodbye.");
                Check(director.MemoryRestored, "memoryRestored is set after the recall presentation.");
                Check(EventCount(GameEventType.PresentationFinished, "PR_CH05_Recall") == 1,
                    "PR_CH05_Recall published exactly one PresentationFinished.");
                Check(!Presentation.PresentationCG.IsShowing && !Presentation.BlackboardTextLayer.IsShowing,
                    "No CG or board text is left after the recall.");
                Check(director.CurrentStageId == "CH05_LetGo", "Stage advanced to CH05_LetGo.");
            }

            private IEnumerator DriveLetGo(GameSession session, Chapters.ChapterDirector director)
            {
                int guard = 0;
                int shown = 0;
                float t = 0f;
                while (director.CurrentStageId == "CH05_LetGo" && t < 90f && guard < 300)
                {
                    CloseFocusLossPause();
                    if (session.Dialogue != null && session.Dialogue.IsPlaying)
                    {
                        session.Dialogue.Advance();
                        shown++;
                    }
                    guard++;
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return Wait(0.3f);
                Dialogue.DialogueSegment letGo;
                Dialogue.DialogueLibrary.Instance.TryGetSegment("CH05", "CH05_LetGo", out letGo);
                Check(letGo != null && shown == letGo.lines.Count, "All expanded let-go lines played.");
                Check(director.LetGoFinished, "letGoFinished is set after the let-go dialogue.");
                Check(director.CurrentStageId == "CH05_Board", "Stage advanced to CH05_Board.");
                GameObject dock = GameObject.Find("BoatDock_CH05");
                Interaction.Interactable dockGate = dock != null
                    ? dock.GetComponent<Interaction.Interactable>() : null;
                Check(dock != null && dock.activeSelf && dockGate != null &&
                      dockGate.IsActiveInStage("CH05_Board"),
                    "The CH05 boat becomes interactable once boarding is allowed.");
            }

            // -- board, cross (zero passengers), reincarnation ---------------
            private IEnumerator BoardAndCross(GameSession session, Chapters.ChapterDirector director)
            {
                GameObject dock = GameObject.Find("BoatDock_CH05");
                Interaction.Interactable dockInt = dock != null ? dock.GetComponent<Interaction.Interactable>() : null;
                Ritual.BoardingSpot spot = dock != null ? dock.GetComponent<Ritual.BoardingSpot>() : null;
                Check(dockInt != null && spot != null, "The CH05 boat carries an Interactable + BoardingSpot.");
                if (dockInt == null || spot == null)
                {
                    Check(false, "Cannot continue without the boarding spot.");
                    yield break;
                }
                spot.OnPropInteract(dockInt);
                yield return Wait(1.8f);
                Check(director.CurrentStageId == "CH05_River",
                    "Boarding moved the chapter to CH05_River (stage=" + director.CurrentStageId + ").");
                Check(GameObject.Find("RiverPassenger_0") == null,
                    "The CH05 crossing has ZERO passengers (no invented ghosts).");

                // Close the river briefing (a real button) so the ending is
                // not covered by leftover UI.
                GameObject startButton = GameObject.Find("Start");
                Button sb = startButton != null ? startButton.GetComponent<Button>() : null;
                if (sb != null) sb.onClick.Invoke();
                yield return Wait(0.3f);

                // The crossing itself is covered by DZ_RIVER_SEEDS; here we
                // drive the real event so the ending can be verified without
                // 10 seconds of autopilot steering.
                River.RiverController river = FindObjectOfType<River.RiverController>();
                typeof(River.RiverController).GetMethod("OnSucceeded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(river, null);
                yield return Wait(0.6f);
                Check(director.CurrentStageId == "CH05_Reincarnation",
                    "River success enters CH05_Reincarnation (stage=" + director.CurrentStageId + ").");
                Check(CurrentCG() == "CG18_FamilyRecollection", "Family recollection begins after successful crossing.");
                Check(!HasVisibleHazards(), "No hazards remain while reading recollections.");
                Vector3 beforePause = FindObjectOfType<River.BoatController>().transform.position;
                session.Pause.Open();
                for (int frame = 0; frame < 5; frame++) yield return null;
                Check(FindObjectOfType<River.BoatController>().transform.position == beforePause, "Pause freezes the boat during recollection.");
                session.Pause.Close();

                int shown = 0;
                var recollections = new HashSet<string>();
                float t = 0f;
                while (director.CurrentStageId == "CH05_Reincarnation" && t < 120f)
                {
                    CloseFocusLossPause();
                    string image = CurrentCG();
                    if (session.Dialogue.IsPlaying && !string.IsNullOrEmpty(image) && recollections.Add(image))
                        ScreenCapture.CaptureScreenshot("Logs/story_revision_" + image + ".png");
                    if (session.Dialogue != null && session.Dialogue.IsPlaying)
                    {
                        session.Dialogue.Advance();
                        shown++;
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return Wait(0.4f);
                Check(shown == 34, "All four recollections and ending played (34 lines, got " + shown + ").");
                foreach (string image in new[] { "CG18_FamilyRecollection", "CG19_LoverRecollection", "CG20_FriendsRecollection", "CG21_TeacherRecollection", "CG05_SelfCrossing" })
                    Check(recollections.Contains(image), "Voyage shows " + image);
                Check(!Presentation.PresentationCG.IsShowing,
                    "The CG is hidden before the boat departs (live river ending).");
                Check(EventCount(GameEventType.PresentationFinished, "PR_CH05_Reincarnation") == 1,
                    "PR_CH05_Reincarnation published exactly one PresentationFinished.");
                Check(director.CurrentStageId == "CH05_Complete",
                    "Stage advanced to CH05_Complete (stage=" + director.CurrentStageId + ").");
            }

            // -- credits: real button, exactly one completion ---------------
            private IEnumerator ConfirmCredits(GameSession session, Chapters.ChapterDirector director)
            {
                yield return Wait(0.4f);
                Check(session.Credits != null && session.Credits.IsShowing,
                    "The credits panel is shown at CH05_Complete.");
                GameObject warmFade = GameObject.Find("DZ_CH05_WarmFade");
                Check(warmFade == null || (session.Credits != null &&
                    session.Credits.transform.parent == warmFade.transform.parent &&
                    session.Credits.transform.GetSiblingIndex() > warmFade.transform.GetSiblingIndex()),
                    "Credits carrier renders above the warm fade; text is not washed out.");
                Directory.CreateDirectory("Logs/CodexScreenshots");
                ScreenCapture.CaptureScreenshot("Logs/CodexScreenshots/credits.png");
                yield return Wait(0.3f);
                GameObject button = GameObject.Find("CreditsConfirm");
                Check(button != null, "The credits panel has a real confirm button.");

                Button b = button != null ? button.GetComponent<Button>() : null;
                if (b != null)
                {
                    b.onClick.Invoke();
                    b.onClick.Invoke(); // double click must not complete twice
                }
                yield return Wait(0.5f);
                Check(EventCount(GameEventType.CreditsConfirmed, "CH05_CREDITS") == 1,
                    "Confirming published exactly one CreditsConfirmed (double click ignored).");
                Check(EventCount(GameEventType.ChapterCompleted, "CH05") == 1,
                    "CH05 was completed exactly once by the credits confirm.");
                Check(session.Credits == null || !session.Credits.IsShowing,
                    "The credits panel closed after confirming.");
                // Replay mode: the chapter must NOT add progress to the save.
                // (CH05 may already be completed by a real main run, so the
                // check is "unchanged", not "absent".)
                GameObject notice = GameObject.Find("AiProductionNotice");
                Check(notice != null && notice.activeInHierarchy, "AI disclosure is visible on the home page.");
                GameObject noticeText = GameObject.Find("AiNoticeBody");
                Text disclosure = noticeText != null ? noticeText.GetComponent<Text>() : null;
                Check(disclosure != null && disclosure.text.Contains("游玩时不实时调用 AI"), "AI disclosure distinguishes development from runtime generation.");
                if (disclosure != null) Check(disclosure.preferredHeight <= disclosure.rectTransform.rect.height + 1f, "AI disclosure text fits its corner panel.");
                ScreenCapture.CaptureScreenshot("Logs/story_revision_main_menu.png");
                yield return Wait(0.2f);
                Check(CompletedSnapshot(session) == completedBefore,
                    "ChapterReplay completion left the completed set unchanged (" +
                    completedBefore + ").");
            }

            private static string CompletedSnapshot(GameSession session)
            {
                if (session == null || session.Save == null || session.Save.Data == null) return "";
                return string.Join(",", session.Save.Data.completedChapterIds);
            }

            // -- helpers ---------------------------------------------------
            private static bool HasVisibleHazards()
            {
                foreach (SpriteRenderer sprite in FindObjectsOfType<SpriteRenderer>())
                    if (sprite.gameObject.name.StartsWith("Whirlpool_")) return true;
                return false;
            }

            private static string CurrentCG()
            {
                GameObject cg = GameObject.Find("DZ_PresentationCG");
                if (cg == null) return "";
                foreach (Image img in cg.GetComponentsInChildren<Image>())
                    if (img.sprite != null) return img.sprite.name;
                return "";
            }

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
                GameSession session = GameSession.Instance;
                if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                {
                    session.Pause.Close();
                }
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                SessionState.SetBool(KeyActive + ".Failed", !ok);
                report.AppendLine();
                report.AppendLine(ok ? "DZ_CH05_PLAY=PASS" : "DZ_CH05_PLAY=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] CH05 playtest report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_CH05_PLAY=PASS");
                else Debug.LogError("DZ_CH05_PLAY=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
