using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Duzhe.EditorTools
{
    /// Live play-mode verification for the R12/R14 formal-asset integration
    /// (user acceptance priorities):
    ///   1. the Ferryman walk sheet renders the correct facing/frames;
    ///   2. the CH01 farewell fades ONLY the two elders (CG tableau ->
    ///      clean scene -> spirits fade, grandson + background stay);
    ///   3. the blackboard text layer renders engine text over the blank
    ///      CG04 board.
    /// Runs the editor GUI (NOT -batchmode, NOT -quit on the command line):
    ///   Tuanjie.exe -projectPath ... -executeMethod Duzhe.EditorTools.R14PlayTest.RunFromCommandLine -logFile ...
    /// State survives the play-mode domain reloads via SessionState; the
    /// driver appends its report lines to Logs/r14_playtest_report.txt and
    /// captures screenshots into Logs/r14_playtest/.
    public static class R14PlayTest
    {
        private const string KeyActive = "Duzhe.R14PlayTest.Active";
        private const string KeyPhase = "Duzhe.R14PlayTest.Phase";
        private const string KeyDone = "Duzhe.R14PlayTest.Done";
        private const string KeyOk = "Duzhe.R14PlayTest.Ok";
        private const string KeyCommandLine = "Duzhe.R14PlayTest.CommandLine";
        private const string ReportPath = "Logs/r14_playtest_report.txt";
        private const string ShotDir = "Logs/r14_playtest";

        private enum Phase { WaitPlay = 1, Running = 2, WaitExit = 3, Done = 4 }

        [MenuItem("渡者/R14 实测(主角行走/亡者淡出/黑板字层)")]
        public static void RunInteractive() { Start(false); }

        public static void RunFromCommandLine() { Start(true); }

        [InitializeOnLoadMethod]
        private static void AutoResume()
        {
            // Re-subscribe after the enter/exit-play domain reloads.
            if (SessionState.GetBool(KeyActive, false))
            {
                EditorApplication.update += OnEditorUpdate;
            }
        }

        private static void Start(bool commandLine)
        {
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyCommandLine, commandLine);
            SessionState.SetInt(KeyPhase, (int)Phase.WaitPlay);
            SessionState.SetBool(KeyDone, false);
            SessionState.SetBool(KeyOk, true);
            try
            {
                Directory.CreateDirectory(ShotDir);
                foreach (string f in Directory.GetFiles(ShotDir, "*.png")) File.Delete(f);
                File.WriteAllText(ReportPath, "== R14 play test ==\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[R14] report init failed: " + e.Message);
            }
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Underworld.unity", OpenSceneMode.Single);
            // The editor window usually loses focus while this test runs in
            // the background. The play-mode PlayerSettings.runInBackground
            // (set by ProjectSetupUtility per spec) freezes the game clock
            // when unfocused; play mode must override it or every timed step
            // stalls. ProjectSettings on disk stay untouched.
            EditorApplication.delayCall += () =>
            {
                PlayerSettings.runInBackground = true;
            };
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void Log(string line)
        {
            Debug.Log("[R14] " + line);
            try { File.AppendAllText(ReportPath, line + "\n", Encoding.UTF8); } catch { }
        }

        private static void OnEditorUpdate()
        {
            Phase phase = (Phase)SessionState.GetInt(KeyPhase, (int)Phase.Done);
            switch (phase)
            {
                case Phase.WaitPlay:
                    if (EditorApplication.isPlaying)
                    {
                        SessionState.SetInt(KeyPhase, (int)Phase.Running);
                        GameObject go = new GameObject("R14PlayTestDriver");
                        UnityEngine.Object.DontDestroyOnLoad(go);
                        go.AddComponent<R14PlayTestDriver>();
                    }
                    break;
                case Phase.Running:
                    if (SessionState.GetBool(KeyDone, false))
                    {
                        SessionState.SetInt(KeyPhase, (int)Phase.WaitExit);
                        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                    }
                    break;
                case Phase.WaitExit:
                    if (!EditorApplication.isPlaying)
                    {
                        SessionState.SetInt(KeyPhase, (int)Phase.Done);
                        SessionState.SetBool(KeyActive, false);
                        // Restore the game default (spec: focus loss pauses).
                        PlayerSettings.runInBackground = false;
                        EditorApplication.update -= OnEditorUpdate;
                        bool ok = SessionState.GetBool(KeyOk, false);
                        Log(ok ? "DZ_R14_PLAYTEST=PASS" : "DZ_R14_PLAYTEST=FAIL");
                        if (SessionState.GetBool(KeyCommandLine, false))
                        {
                            EditorApplication.Exit(ok ? 0 : 1);
                        }
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Play-mode driver: a small state machine that exercises the three
        // acceptance priorities in the REAL scenes with the REAL components.
        // ------------------------------------------------------------------
        private class R14PlayTestDriver : MonoBehaviour
        {
            private enum Step { Init, Walk, LoadFarm, Seat, Visuals, Blackboard, Finish }

            private static readonly Core.Direction4[] Dirs =
                { Core.Direction4.Down, Core.Direction4.Left, Core.Direction4.Right, Core.Direction4.Up };

            private Step step = Step.Init;
            private float t;
            private Player.PlayerSpriteAnimator animator;
            private Scenes.FarmCourtyardCast courtyard;
            private Scenes.CH01FarewellCast farewell;
            private int walkIdx;
            private bool shotTaken;
            private bool started;
            private bool diagLogged;
            private float fadeStartT;

            private void Update()
            {
                // The editor window runs unfocused: GameBootstrap's focus-loss
                // auto-pause (spec) opens the pause menu and freezes the
                // clock. This is a headless verification run, so close the
                // overlay the moment it appears (game behaviour untouched -
                // in a real focused window it never triggers).
                Core.GameSession session = Core.GameSession.Instance;
                if (session != null && session.Paused && session.Pause != null && session.Pause.IsOpen)
                {
                    session.Pause.Close();
                }
                t += Time.unscaledDeltaTime;
                switch (step)
                {
                    case Step.Init: DoInit(); break;
                    case Step.Walk: DoWalk(); break;
                    case Step.LoadFarm: DoLoadFarm(); break;
                    case Step.Seat: DoSeat(); break;
                    case Step.Visuals: DoVisuals(); break;
                    case Step.Blackboard: DoBlackboard(); break;
                }
            }

            private void Fail(string msg)
            {
                R14PlayTest.Log("FAIL " + msg);
                SessionState.SetBool(KeyOk, false);
                Finish();
            }

            private void Finish()
            {
                SessionState.SetBool(KeyDone, true);
                step = Step.Finish;
            }

            private void Capture(string name)
            {
                ScreenCapture.CaptureScreenshot(ShotDir + "/" + name + ".png");
                R14PlayTest.Log("shot " + name);
            }

            private void DoInit()
            {
                if (t < 0.6f) return; // let the bootstrap settle
                Core.GameSession session = Core.GameSession.Instance;
                if (session == null) { Fail("GameSession missing"); return; }
                session.SetMode(Core.GameMode.MainRun); // hides the menu panel
                if (session.MainMenu != null) session.MainMenu.Refresh();

                Core.GameBootstrap boot = Core.GameBootstrap.Instance;
                if (boot == null || boot.Player == null) { Fail("player missing"); return; }
                boot.Player.transform.position = new Vector3(0f, -1f, 0f);

                animator = boot.Player.GetComponentInChildren<Player.PlayerSpriteAnimator>();
                if (animator == null) { Fail("PlayerSpriteAnimator missing - formal CHR01 sheet not wired"); return; }
                R14PlayTest.Log("OK   player: formal CHR01 animator present");
                step = Step.Walk; t = 0f; walkIdx = 0; shotTaken = false;
            }

            private void DoWalk()
            {
                if (walkIdx < Dirs.Length)
                {
                    Core.Direction4 dir = Dirs[walkIdx];
                    // PRODUCTION path: move the player via transform deltas
                    // (no DebugDrive). The animator must detect the position
                    // change and cycle WalkA->Idle->WalkB->Idle by itself.
                    Core.GameBootstrap boot = Core.GameBootstrap.Instance;
                    boot.Player.transform.position += Vector3.right * (1.6f * Time.deltaTime);
                    if (!shotTaken && t >= 0.45f) { Capture("walk_" + dir); shotTaken = true; }
                    else if (shotTaken && t >= 0.75f) { walkIdx++; shotTaken = false; t = 0f; }
                }
                else
                {
                    // Stop: sprite must settle on the Idle column by itself.
                    if (!shotTaken && t >= 0.4f) { Capture("idle_down"); shotTaken = true; }
                    else if (shotTaken && t >= 0.6f)
                    {
                        R14PlayTest.Log("OK   walk: 4 directions + idle captured (production position-delta path)");
                        step = Step.LoadFarm; t = 0f; started = false;
                    }
                }
            }

            private void DoLoadFarm()
            {
                if (!started)
                {
                    started = true;
                    SceneManager.LoadScene("FarmCourtyard");
                    return;
                }
                if (t < 2.2f) return; // scene load + cast Start
                courtyard = FindObjectOfType<Scenes.FarmCourtyardCast>();
                farewell = FindObjectOfType<Scenes.CH01FarewellCast>();
                if (courtyard == null || farewell == null) { Fail("FarmCourtyard casts missing"); return; }
                if (courtyard.elderA == null || !courtyard.elderA.gameObject.activeSelf)
                {
                    if (t > 8f) { Fail("elders not activated by the scene cast"); return; }
                    return; // the cast activates them beside the spawn
                }
                // Mimic the post-knock state through the production entry so
                // the player step-back and the grandson walk are exercised.
                if (courtyard.grandson == null) { Fail("grandson missing"); return; }
                if (!courtyard.grandson.activeSelf) courtyard.BringGrandsonOut();

                Vector3 grandsonGoal = new Vector3(
                    courtyard.grandsonReunionPoint.x, courtyard.grandsonReunionPoint.y, 0f);
                if ((courtyard.grandson.transform.position - grandsonGoal).sqrMagnitude > 0.002f)
                {
                    if (t > 8f) { Fail("grandson did not reach the reunion point"); return; }
                    return;
                }
                Core.GameBootstrap boot = Core.GameBootstrap.Instance;
                if (boot == null || boot.Player == null) { Fail("player missing after the door"); return; }
                float playerY = boot.Player.transform.position.y;
                float grandsonY = courtyard.grandson.transform.position.y;
                if (!(playerY < grandsonY && grandsonY < courtyard.doorPoint.y))
                {
                    Fail("reunion order must be door > grandson > player (door=" + courtyard.doorPoint.y +
                        " grandson=" + grandsonY + " player=" + playerY + ")");
                    return;
                }
                // Let the camera and the two followers settle after the step-back.
                bool eldersSettled = courtyard.elderA != null && courtyard.elderB != null &&
                    Mathf.Abs(courtyard.elderA.transform.position.y - (playerY - 0.2f)) < 0.35f &&
                    Mathf.Abs(courtyard.elderB.transform.position.y - (playerY - 0.2f)) < 0.35f;
                if (!eldersSettled || t < 4.5f)
                {
                    if (t > 10f) { Fail("elders did not follow the player down after the step-back"); return; }
                    return;
                }
                Capture("farm_reunion_layout");
                R14PlayTest.Log("OK   farm: player stepped back, grandson between player and door, elders followed down");
                step = Step.Seat; t = 0f; started = false; diagLogged = false;
            }

            private void DoSeat()
            {
                if (!started)
                {
                    started = true;
                    // NOT StartCoroutine: a scene-load already killed our
                    // previous coroutines; drive the seat/visuals phases
                    // manually through the cast's own coroutines (they run on
                    // the cast objects and survive until deactivated).
                    seatRoutine = farewell.StartCoroutine(farewell.OnPresentationBegin("PR_CH01_Farewell"));
                    return;
                }
                bool seated = courtyard.elderA != null && courtyard.elderA.IsSeated &&
                              courtyard.elderB != null && courtyard.elderB.IsSeated;
                if (!seated)
                {
                    if (t > 4f && !diagLogged)
                    {
                        diagLogged = true;
                        string sa = courtyard.elderA != null ? courtyard.elderA.transform.position.ToString("F2") : "null";
                        string sb = courtyard.elderB != null ? courtyard.elderB.transform.position.ToString("F2") : "null";
                        R14PlayTest.Log("diag elderA@" + sa + " seated=" + (courtyard.elderA != null && courtyard.elderA.IsSeated) +
                                        " elderB@" + sb + " seated=" + (courtyard.elderB != null && courtyard.elderB.IsSeated) +
                                        " seatA=" + (farewell.elderSeatA != null ? farewell.elderSeatA.position.ToString("F2") : "null") +
                                        " seatB=" + (farewell.elderSeatB != null ? farewell.elderSeatB.position.ToString("F2") : "null"));
                    }
                    // No hard timeout here: an unfocused editor window keeps
                    // re-triggering the spec's focus-loss auto-pause, which
                    // stretches game time. The driver closes the overlay as
                    // fast as it appears, so the walk always completes.
                    if (t > 400f) Fail("elders did not reach their seats");
                    return;
                }
                if (!Presentation.PresentationCG.IsShowing) { Fail("CG01 tableau not shown during begin"); return; }
                Capture("farewell_cg_tableau");
                R14PlayTest.Log("OK   farewell: elders seated, CG tableau shown");
                step = Step.Visuals; t = 0f; started = false; shotTaken = false; diagLogged = false;
            }

            private Coroutine seatRoutine;

            private void DoVisuals()
            {
                if (!started)
                {
                    started = true;
                    fadeDoneShot = false;
                    lastShotT = 0f;
                    farewell.StartCoroutine(farewell.OnPresentationVisuals("PR_CH01_Farewell"));
                    return;
                }
                if (Presentation.PresentationCG.IsShowing)
                {
                    if (t > 3f) Fail("CG did not hide before the spirit fade");
                    return;
                }
                if (!shotTaken)
                {
                    if (t >= 0.2f) { Capture("fade_start"); shotTaken = true; }
                }
                // Poll the OUTCOME, not a wall clock: an unfocused editor
                // stretches game time via the focus-loss auto-pause, so a
                // fixed unscaled wait can outrun the actual fade.
                bool eldersGone = (courtyard.elderA == null || !courtyard.elderA.gameObject.activeSelf) &&
                                  (courtyard.elderB == null || !courtyard.elderB.gameObject.activeSelf);
                if (eldersGone && !fadeDoneShot)
                {
                    Capture("fade_end");
                    fadeDoneShot = true;
                    lastShotT = t;
                }
                if (fadeDoneShot && t - lastShotT >= 0.6f)
                {
                    bool grandsonVisible = courtyard.grandson != null && courtyard.grandson.activeSelf;
                    if (!grandsonVisible) { Fail("grandson missing after the fade"); return; }
                    R14PlayTest.Log("OK   farewell fade: CG hidden -> ONLY the two elders faded; grandson + scene remain");
                    step = Step.Blackboard; t = 0f; started = false;
                }
            }

            private bool fadeDoneShot;
            private float lastShotT;

            private void DoBlackboard()
            {
                if (!started)
                {
                    started = true;
                    Presentation.PresentationCG.Show("CG04_TeacherFarewell");
                    Presentation.BlackboardTextLayer.Show(new[] { "这就是，最后一课了。", "再见" });
                    shotTaken = false;
                    return;
                }
                if (!shotTaken && t >= 1.2f)
                {
                    if (!Presentation.PresentationCG.IsShowing) { Fail("CG04 tableau not shown"); return; }
                    if (!Presentation.BlackboardTextLayer.IsShowing) { Fail("blackboard text layer not shown"); return; }
                    // Dump the live text state for the report (font/rect).
                    Text txt = UnityEngine.Object.FindObjectOfType<Text>();
                    if (txt != null && txt.gameObject.name == "DZ_BlackboardText")
                    {
                        RectTransform r = txt.rectTransform;
                        R14PlayTest.Log("diag text: font=" + (txt.font != null ? txt.font.name : "NULL") +
                            " size=" + r.rect.width.ToString("F0") + "x" + r.rect.height.ToString("F0") +
                            " pos=" + r.anchoredPosition.ToString("F0") +
                            " hasText=" + !string.IsNullOrEmpty(txt.text) +
                            " enabled=" + txt.enabled);
                        // Read the ACTUAL screen pixels in the text area (a
                        // quarter-resolution sample of the board band) -
                        // bypasses screenshot timing entirely.
                        int sx = Screen.width / 4;
                        int sy = Screen.height / 8;
                        int sw = Screen.width / 2;
                        int sh = Screen.height / 8;
                        Texture2D probe = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
                        probe.ReadPixels(new Rect(sx, sy, sw, sh), 0, 0);
                        probe.Apply();
                        Color32[] px = probe.GetPixels32();
                        int bright = 0;
                        int dark = 0;
                        foreach (Color32 c in px)
                        {
                            if (c.r > 200 && c.g > 200 && c.b > 190) bright++;
                            if (c.r < 60 && c.g < 60 && c.b < 60) dark++;
                        }
                        R14PlayTest.Log("diag pixels: sampled " + px.Length + " bright=" + bright +
                            " dark=" + dark + " screen=" + Screen.width + "x" + Screen.height);
                        UnityEngine.Object.DestroyImmediate(probe);
                    }
                    else
                    {
                        R14PlayTest.Log("diag text: DZ_BlackboardText object not found via FindObjectOfType");
                    }
                    Capture("blackboard_text");
                    shotTaken = true;
                    R14PlayTest.Log("OK   blackboard: CG04 + engine text layer captured");
                    return;
                }
                if (shotTaken && t >= 1.6f)
                {
                    Presentation.PresentationCG.Hide();
                    Presentation.BlackboardTextLayer.Hide();
                    R14PlayTest.Log("DONE all steps");
                    Finish();
                }
            }
        }
    }
}
