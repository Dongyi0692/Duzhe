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
    /// C09 play-mode verification: start menu pages, chapter list, gallery
    /// replay routing and the run-reset contract.
    ///   -executeMethod Duzhe.EditorTools.MenuGalleryPlayTest.RunFromCommandLine
    /// Runs against the real save READ-ONLY (all writes redirected to a temp
    /// folder); a gallery replay must never change completed/unlocked sets.
    public static class MenuGalleryPlayTest
    {
        private const string KeyActive = "Duzhe.MenuGalleryPlayTest.Active";
        private const string KeyPhase = "Duzhe.MenuGalleryPlayTest.Phase";
        private const string ReportPath = "Logs/menu_gallery_playtest.txt";

        [MenuItem("渡者/菜单与回忆馆验证(Play)")]
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
            SessionState.SetFloat("Duzhe.MenuGalleryPlayTest.Started", 0f);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);
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
            double started = SessionState.GetFloat("Duzhe.MenuGalleryPlayTest.Started", 0f);
            if (started <= 0)
            {
                SessionState.SetFloat("Duzhe.MenuGalleryPlayTest.Started", (float)EditorApplication.timeSinceStartup);
            }
            else if (EditorApplication.timeSinceStartup - started > 600.0)
            {
                try { File.AppendAllText(ReportPath, "TIMEOUT\nDZ_MENU_GALLERY=FAIL\n", Encoding.UTF8); }
                catch { }
                Debug.LogError("DZ_MENU_GALLERY=FAIL (timeout)");
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(1);
                return;
            }
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("MenuGalleryPlayTestDriver");
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
                report.AppendLine("== menu & gallery verification ==");
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
                if (session == null || session.MainMenu == null)
                {
                    Check(false, "GameSession/MainMenu never appeared.");
                    Finish();
                    yield break;
                }
                // Keep this probe out of the player's save.
                SaveService.TestSaveDirectoryOverride =
                    Path.Combine(Path.GetTempPath(), "DuzheMenuGalleryProbe");
                yield return Wait(0.4f);

                // Guarantee the replay branch is exercised even when the
                // user's save has no unlocked memories. Writes are isolated.
                session.Save.UnlockPresentation("PR_CH03_Farewell");

                int completedBefore = CompletedCount(session);
                int unlockedBefore = UnlockedCount(session);

                // --- menu pages -------------------------------------------
                Check(GameObject.Find("MenuHomePage") != null, "Home page exists.");
                Check(GameObject.Find("Start") != null, "Home page has the start button.");
                Check(session.MainMenu != null && session.Mode == GameMode.MainMenu, "The menu is showing at boot.");
                GameObject menuArt = GameObject.Find("MenuArt");
                Check(menuArt != null && menuArt.GetComponent<Image>() != null &&
                    menuArt.GetComponent<Image>().sprite != null &&
                    menuArt.GetComponent<Image>().sprite.name == "BG00_MainMenu",
                    "Home displays the formal background in a screen-filling UI layer.");
                Directory.CreateDirectory("Logs/CodexScreenshots");
                ScreenCapture.CaptureScreenshot("Logs/CodexScreenshots/main_menu.png");
                yield return Wait(0.3f);

                // Regression: closing the chapter list first must not let the
                // same ESC close the pause menu later in that same frame.
                session.Pause.Open();
                session.ChapterMenu.Show();
                var escapeSetter = typeof(InputRouter).GetProperty("EscapePressed").GetSetMethod(true);
                escapeSetter.Invoke(InputRouter.Instance, new object[] { true });
                session.ChapterMenu.SendMessage("Update");
                session.Pause.SendMessage("Update");
                Check(!session.ChapterMenu.IsShowing && session.Paused,
                    "One ESC returns from chapters to pause without closing pause.");
                session.Pause.Close();

                session.SetPendingMenuTab(MenuTab.Chapters);
                session.MainMenu.Refresh();
                yield return Wait(0.2f);
                GameObject chaptersPage = GameObject.Find("MenuChaptersPage");
                Check(chaptersPage != null && chaptersPage.activeSelf, "Chapter page opens from the menu.");
                int rows = CountRows("ChapterRows");
                Check(rows == completedBefore,
                    "Chapter page lists exactly the completed chapters (" + rows + "/" + completedBefore + ").");
                Check(GameObject.Find("MenuChapter_CH05") != null || completedBefore < 5,
                    "A completed chapter row exists for CH05 when CH05 is done.");

                session.SetPendingMenuTab(MenuTab.Gallery);
                session.MainMenu.Refresh();
                yield return Wait(0.2f);
                GameObject galleryPage = GameObject.Find("MenuGalleryPage");
                Check(galleryPage != null && galleryPage.activeSelf, "Gallery page opens from the menu.");
                int galleryRows = CountRows("GalleryRows");
                Check(galleryRows == unlockedBefore,
                    "Gallery lists exactly the eligible chapters (" + galleryRows + "/" + unlockedBefore + ").");

                // --- gallery replay (PR_CH03_Farewell is unlocked) ---------
                if (session.Save != null && session.Save.HasUnlockedPresentation("PR_CH03_Farewell"))
                {
                    session.SetPendingMenuTab(MenuTab.Gallery);
                    session.MainMenu.Refresh();
                    yield return Wait(0.2f);
                    GameObject row = GameObject.Find("MenuGallery_PR_CH03_Farewell");
                    Check(row != null, "The unlocked CH03 farewell has a gallery row.");
                    if (row != null)
                    {
                        Button b = row.GetComponent<Button>();
                        if (b != null) b.onClick.Invoke();
                        // A second click while the load runs must be ignored.
                        if (b != null) b.onClick.Invoke();
                        // The editor can run very slowly: poll for the real
                        // start instead of assuming a fixed load time.
                        float poll = 0f;
                        while (poll < 20f &&
                               (session.Presentations == null || !session.Presentations.IsPlaying))
                        {
                            poll += Time.unscaledDeltaTime;
                            yield return null;
                        }
                        Check(session.Mode == GameMode.PresentationReplay,
                            "Gallery replay runs in PresentationReplay (mode=" + session.Mode + ").");
                        Check(session.Transitions == null || session.Transitions.CurrentSceneId == "RecordingStudio",
                            "Gallery replay loaded the presentation's own scene.");
                        Check(session.Presentations != null && session.Presentations.IsPlaying,
                            "The presentation started once the scene was ready (waited " + poll.ToString("F1") + "s).");
                        Check(Presentation.PresentationCG.IsShowing, "Chapter montage is shown at the start of the gallery replay.");

                        // Quit mid-way: no unlock, back to the gallery list.
                        session.Presentations.ForceAbort();
                        session.ReturnToMenu(MenuTab.Gallery);
                        yield return Wait(1.6f);
                        Check(session.Mode == GameMode.MainMenu && session.PendingMenuTab == MenuTab.Gallery,
                            "Quitting a replay returns to the gallery list (mode=" + session.Mode +
                            ", tab=" + session.PendingMenuTab + ").");
                        Check(!Presentation.PresentationCG.IsShowing && !Presentation.BlackboardTextLayer.IsShowing,
                            "No CG or board text is left after quitting a replay.");
                    }
                }
                else
                {
                    report.AppendLine("SKIP gallery replay: PR_CH03_Farewell not unlocked in this save.");
                }

                // --- a completed replay must not change the save ----------
                Check(CompletedCount(session) == completedBefore,
                    "Completed chapters unchanged after the replay (" + CompletedCount(session) + ").");
                Check(UnlockedCount(session) == unlockedBefore,
                    "Unlocked presentations unchanged after the replay (" + UnlockedCount(session) + ").");

                SaveService.TestSaveDirectoryOverride = null;
                Finish();
            }

            private static int CountRows(string container)
            {
                GameObject rows = GameObject.Find(container);
                if (rows == null) return 0;
                int n = 0;
                foreach (Transform child in rows.transform)
                {
                    if (child.gameObject.activeSelf && child.GetComponent<Button>() != null) n++;
                }
                return n;
            }

            private static int CompletedCount(GameSession s)
            {
                if (s.Save == null || s.Save.Data == null) return 0;
                int n = 0;
                foreach (string id in s.Save.Data.completedChapterIds)
                {
                    if (Chapters.ChapterCatalog.IsFormalChapter(id)) n++;
                }
                return n;
            }

            private static int UnlockedCount(GameSession s)
            {
                if (s.Save == null) return 0;
                int n = 0;
                foreach (UI.GalleryController.GalleryEntry e in UI.GalleryController.Entries)
                {
                    if (UI.GalleryController.CanView(s.Save, e.presentationId)) n++;
                }
                return n;
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
                report.AppendLine(ok ? "DZ_MENU_GALLERY=PASS" : "DZ_MENU_GALLERY=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] menu/gallery report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_MENU_GALLERY=PASS");
                else Debug.LogError("DZ_MENU_GALLERY=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
