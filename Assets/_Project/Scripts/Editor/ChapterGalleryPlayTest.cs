using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Duzhe.Core;
using Duzhe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Exercises the five visible entries, their complete playlists and exits
    /// against an isolated save. Does not automate puzzles or player movement.
    public static class ChapterGalleryPlayTest
    {
        private const string Active = "Duzhe.ChapterGallery.Active";
        private const string Phase = "Duzhe.ChapterGallery.Phase";
        private const string Failed = "Duzhe.ChapterGallery.Failed";
        private const string ReportPath = "Logs/chapter_gallery_playtest.txt";

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(Active, true);
            SessionState.SetBool(Failed, false);
            SessionState.SetInt(Phase, 0);
            SessionState.SetFloat(Active + ".Start", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Active + ".Background", PlayerSettings.runInBackground);
            SaveService.TestSaveDirectoryOverride = Path.Combine(Path.GetTempPath(), "DuzheChapterGallery", Guid.NewGuid().ToString("N"));
            SessionState.SetString(Active + ".Save", SaveService.TestSaveDirectoryOverride);
            Directory.CreateDirectory("Logs");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);
            PlayerSettings.runInBackground = true;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Active, false)) return;
            SaveService.TestSaveDirectoryOverride = SessionState.GetString(Active + ".Save", "");
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Active + ".Start", 0f) > 240)
            {
                File.AppendAllText(ReportPath, "TIMEOUT\nDZ_CHAPTER_GALLERY=FAIL\n");
                SessionState.SetBool(Failed, true);
                EditorApplication.isPlaying = false;
            }
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                SessionState.SetBool(Active, false);
                SaveService.TestSaveDirectoryOverride = null;
                PlayerSettings.runInBackground = SessionState.GetBool(Active + ".Background", false);
                EditorApplication.Exit(SessionState.GetBool(Failed, false) ? 1 : 0);
                return;
            }
            if (SessionState.GetInt(Phase, 0) != 0) return;
            SessionState.SetInt(Phase, 1);
            GameObject go = new GameObject("ChapterGalleryProbe");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Driver>();
        }

        public class Driver : MonoBehaviour
        {
            private readonly StringBuilder report = new StringBuilder();
            private readonly List<string> segments = new List<string>();
            private readonly List<string> presentations = new List<string>();
            private readonly HashSet<string> checkedLines = new HashSet<string>();
            private string currentLine = "";
            private string observedRun = "";
            private bool ok = true;
            private GameSession session;

            private void Check(bool condition, string message)
            {
                report.AppendLine((condition ? "PASS " : "FAIL ") + message);
                File.WriteAllText(ReportPath, report.ToString());
                if (!condition) ok = false;
            }

            private IEnumerator Wait(float seconds)
            {
                float t = 0f;
                while (t < seconds || (session != null && session.Transitions.IsTransitioning && t < 20f))
                {
                    if (session != null && session.Pause != null && session.Pause.IsOpen) session.Pause.Close();
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return null;
            }

            private void ShowList()
            {
                session.SetPendingMenuTab(MenuTab.Gallery);
                session.MainMenu.Refresh();
            }

            private static int CountRows(string name, string parent = "")
            {
                GameObject rows = GameObject.Find(name);
                if (!string.IsNullOrEmpty(parent))
                {
                    GameObject owner = GameObject.Find(parent);
                    Transform child = owner != null ? owner.transform.Find(name) : null;
                    rows = child != null ? child.gameObject : null;
                }
                if (rows == null) return -1;
                int count = 0;
                foreach (Transform child in rows.transform)
                    if (child.gameObject.activeSelf && child.GetComponent<UnityEngine.UI.Button>() != null) count++;
                return count;
            }

            private void ObserveLine(Duzhe.Dialogue.DialogueLine line) { currentLine = line.lineId; }

            private void ObserveEvent(GameEvent e)
            {
                // Gallery may reset the run inside this same multicast event.
                if (e.runId != observedRun) return;
                if (e.type == GameEventType.DialogueFinished) segments.Add(e.sourceId);
                if (e.type == GameEventType.PresentationFinished) presentations.Add(e.sourceId);
            }

            private void InspectMemoryImage()
            {
                if (!currentLine.Contains("_Memory") || !currentLine.EndsWith("_001") || checkedLines.Contains(currentLine)) return;
                string ch = currentLine.Substring(0, 4);
                int number = int.Parse(currentLine.Substring(11, 2));
                int first = ch == "CH01" ? 9 : ch == "CH02" ? 6 : ch == "CH03" ? 12 : 15;
                string family = ch == "CH01" ? "Family" : ch == "CH02" ? "Lovers" : ch == "CH03" ? "Band" : "Teacher";
                string expected = "CG" + (first + number - 1).ToString("00") + "_" + family + "Memory" + number.ToString("00");
                GameObject cg = GameObject.Find("DZ_PresentationCG");
                UnityEngine.UI.Image image = cg != null ? cg.GetComponent<UnityEngine.UI.Image>() : null;
                Check(image != null && image.sprite != null && image.sprite.name == expected, currentLine + " displays " + expected);
                if (ch == "CH04") Check(!Duzhe.Presentation.BlackboardTextLayer.IsShowing, "blackboard writing is hidden during teacher montage");
                checkedLines.Add(currentLine);
            }

            private IEnumerator Start()
            {
                float t = 0;
                while (GameSession.Instance == null && t < 15) { t += Time.unscaledDeltaTime; yield return null; }
                session = GameSession.Instance;
                if (session == null) { Check(false, "session exists"); Finish(); yield break; }
                session.Save.Load();
                yield return Wait(0.5f);
                session.Dialogue.LineShown += ObserveLine;
                session.OnGameEvent += ObserveEvent;
                Check(GalleryController.Entries.Length == 5, "exactly five chapter entries registered");
                ShowList(); yield return Wait(0.1f);
                Check(CountRows("GalleryRows") == 0, "fresh save has no unlocked rows");
                session.Save.Data.unlockedPresentationIds.Add("PR_CH01_Memory");
                session.Save.Data.unlockedPresentationIds.Add("PR_CH05_Recall");
                ShowList(); yield return Wait(0.1f);
                Check(CountRows("GalleryRows") == 0, "partial memories do not expose unseen chapter endings");
                session.Save.Data.completedChapterIds.Add("CH02");
                session.Save.Data.unlockedPresentationIds.Add("PR_CH03_Farewell");
                ShowList(); yield return Wait(0.1f);
                Check(CountRows("GalleryRows") == 2, "completed chapter and legacy farewell unlock each give one row");
                session.Save.Data.unlockedPresentationIds.Clear();
                session.Save.Data.completedChapterIds.Clear();
                foreach (string ch in Duzhe.Chapters.ChapterCatalog.FormalChapterIds) session.Save.Data.completedChapterIds.Add(ch);
                ShowList(); yield return Wait(0.2f);
                Check(CountRows("GalleryRows") == 5, "completed legacy save lists five rows with no new unlock flags");
                foreach (GalleryController.GalleryEntry entry in GalleryController.Entries)
                    Check(GameObject.Find("MenuGallery_" + entry.presentationId) != null, "one visible row for " + entry.chapterId);
                session.Gallery.Show(); yield return Wait(0.1f);
                Check(CountRows("GalleryRows", "GalleryPanel") == 5, "standalone gallery also lists five rows");
                session.Gallery.Hide();
                ScreenCapture.CaptureScreenshot("Logs/gallery_five_entries.png"); yield return Wait(0.2f);

                string[][] expected = {
                    new[] { "CH01_Memory01", "CH01_Memory02", "CH01_Memory03", "CH01_Farewell" },
                    new[] { "CH02_Memory01", "CH02_Memory02", "CH02_Memory03", "CH02_Farewell" },
                    new[] { "CH03_Memory01", "CH03_Memory02", "CH03_Memory03", "CH03_Farewell" },
                    new[] { "CH04_Lesson", "CH04_Memory01", "CH04_Memory02", "CH04_Memory03", "CH04_Farewell" },
                    new[] { "CH05_Recall", "CH05_LetGo", "CH05_RecollectionFamily", "CH05_RecollectionLover", "CH05_RecollectionFriends", "CH05_RecollectionTeacher", "CH05_Reincarnation" }
                };
                for (int i = 0; i < GalleryController.Entries.Length; i++)
                {
                    GalleryController.GalleryEntry entry = GalleryController.Entries[i];
                    string before = JsonUtility.ToJson(session.Save.Data);
                    currentLine = ""; segments.Clear(); presentations.Clear();
                    session.Gallery.PlayFromMenu(entry.presentationId);
                    string replayRun = session.RunId;
                    observedRun = replayRun;
                    session.Gallery.PlayFromMenu(entry.presentationId);
                    Check(session.RunId == replayRun, entry.chapterId + " ignores double click during entry load");
                    yield return Wait(0.5f);
                    t = 0;
                    while (session.Mode == GameMode.PresentationReplay && t < 35f)
                    {
                        InspectMemoryImage();
                        if (session.Dialogue.IsPlaying) session.Dialogue.Advance();
                        yield return Wait(0.045f);
                        t += 0.045f;
                    }
                    yield return Wait(0.3f);
                    Check(string.Join(",", segments) == string.Join(",", expected[i]), entry.chapterId + " complete segment order: " + string.Join(",", segments));
                    Check(string.Join(",", presentations) == string.Join(",", entry.presentationIds), entry.chapterId + " each presentation finishes once");
                    Check(session.Mode == GameMode.MainMenu && session.PendingMenuTab == MenuTab.Gallery, entry.chapterId + " returns after whole recollection");
                    Check(CountRows("GalleryRows") == 5, entry.chapterId + " returns to five rows");
                    Check(before == JsonUtility.ToJson(session.Save.Data), entry.chapterId + " preserves save progress");
                    Check(!Duzhe.Presentation.PresentationCG.IsShowing && !Duzhe.Presentation.BlackboardTextLayer.IsShowing,
                        entry.chapterId + " clears images and writing after replay");
                    if (session.Mode != GameMode.MainMenu) { session.Gallery.LeaveReplay(); yield return Wait(0.5f); }
                }
                Check(checkedLines.Count == 12, "all twelve montage frames played through the chapter entries");

                // ESC while a montage is running must cancel the remaining farewell too.
                string saveBeforeExit = JsonUtility.ToJson(session.Save.Data);
                session.Gallery.PlayFromMenu("PR_CH01_Farewell"); yield return Wait(0.5f);
                typeof(InputRouter).GetProperty("EscapePressed").GetSetMethod(true).Invoke(InputRouter.Instance, new object[] { true });
                session.Pause.SendMessage("Update"); yield return Wait(0.7f);
                Check(session.Mode == GameMode.MainMenu && !session.Presentations.IsPlaying, "ESC cancels current montage and the queued farewell");

                // Fifth entry crosses from Underworld to River; exit during that load.
                session.Gallery.PlayFromMenu("PR_CH05_Reincarnation"); yield return Wait(0.5f);
                t = 0;
                while (session.Mode == GameMode.PresentationReplay && !session.Transitions.IsTransitioning && t < 15f)
                {
                    if (session.Dialogue.IsPlaying) session.Dialogue.Advance();
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Check(session.Transitions.IsTransitioning, "CH05 starts its inter-scene transition");
                session.Gallery.LeaveReplay(); yield return Wait(1f);
                Check(session.Mode == GameMode.MainMenu && session.PendingMenuTab == MenuTab.Gallery && !session.Presentations.IsPlaying,
                    "exit during CH05 scene transition returns to list without starting the ending");
                Check(saveBeforeExit == JsonUtility.ToJson(session.Save.Data), "early exits preserve save progress");

                // A post-update save with every internal presentation unlocked still has five rows.
                foreach (string ch in new[] { "CH01", "CH02", "CH03", "CH04" }) session.Save.Data.unlockedPresentationIds.Add("PR_" + ch + "_Memory");
                foreach (GalleryController.GalleryEntry entry in GalleryController.Entries) session.Save.Data.unlockedPresentationIds.Add(entry.presentationId);
                session.Save.Data.unlockedPresentationIds.Add("PR_CH05_Recall");
                ShowList(); yield return Wait(0.1f);
                Check(CountRows("GalleryRows") == 5, "all ten internal unlocks still produce only five rows");

                // CH04's main story already played class and montage; only farewell runs here.
                session.Chapters.StartChapter("CH04", GameMode.ChapterReplay); yield return Wait(0.5f);
                session.Dialogue.ForceStop(); currentLine = "";
                var config = Duzhe.Chapters.ChapterCatalog.GetChapter("CH04");
                int farewell = Array.FindIndex(config.stages, stage => stage.stageId == "CH04_Farewell");
                typeof(Duzhe.Chapters.ChapterDirector).GetMethod("EnterStage", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(session.Chapters, new object[] { farewell });
                yield return Wait(0.5f);
                Check(currentLine == "CH04_Farewell_001", "main chapter farewell does not repeat lesson or montage");
                session.ReturnToMenu(MenuTab.Gallery); yield return Wait(0.5f);
                Finish();
            }

            private void Finish()
            {
                if (session != null) { session.Dialogue.LineShown -= ObserveLine; session.OnGameEvent -= ObserveEvent; }
                report.AppendLine(ok ? "DZ_CHAPTER_GALLERY=PASS" : "DZ_CHAPTER_GALLERY=FAIL");
                File.WriteAllText(ReportPath, report.ToString());
                Debug.Log(report.ToString());
                SessionState.SetBool(Failed, !ok);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
