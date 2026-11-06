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
    /// C10 audio-bus verification (numbers only; listening is a user item).
    ///   -executeMethod Duzhe.EditorTools.AudioPlayTest.RunFromCommandLine
    /// Checks the delivered clips load, the bus gains are applied once,
    /// master=0 really silences, the notes duck works, and pause/resume
    /// behaves for music and one-shots.
    public static class AudioPlayTest
    {
        private const string KeyActive = "Duzhe.AudioPlayTest.Active";
        private const string KeyPhase = "Duzhe.AudioPlayTest.Phase";
        private const string ReportPath = "Logs/audio_playtest.txt";

        [MenuItem("渡者/音频总线验证(Play)")]
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
            SessionState.SetFloat("Duzhe.AudioPlayTest.Started", 0f);
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
            double started = SessionState.GetFloat("Duzhe.AudioPlayTest.Started", 0f);
            if (started <= 0)
            {
                SessionState.SetFloat("Duzhe.AudioPlayTest.Started", (float)EditorApplication.timeSinceStartup);
            }
            else if (EditorApplication.timeSinceStartup - started > 300.0)
            {
                try { File.AppendAllText(ReportPath, "TIMEOUT\nDZ_AUDIO=FAIL\n", Encoding.UTF8); } catch { }
                Debug.LogError("DZ_AUDIO=FAIL (timeout)");
                SessionState.SetBool(KeyActive, false);
                PlayerSettings.runInBackground = false;
                EditorApplication.Exit(1);
                return;
            }
            if (SessionState.GetInt(KeyPhase, 0) == 0)
            {
                SessionState.SetInt(KeyPhase, 1);
                GameObject go = new GameObject("AudioPlayTestDriver");
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
                report.AppendLine("== audio bus verification ==");
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
                if (session == null || session.Audio == null)
                {
                    Check(false, "GameSession/AudioService never appeared.");
                    Finish();
                    yield break;
                }
                // Never write the player's save from this probe.
                SaveService.TestSaveDirectoryOverride =
                    Path.Combine(Path.GetTempPath(), "DuzheAudioProbe");
                yield return Wait(0.3f);
                Audio.AudioService audio = session.Audio;

                // Delivered clips load.
                Check(Resources.Load<AudioClip>("Audio/Notes/N01_Do") != null &&
                      Resources.Load<AudioClip>("Audio/SFX/SFX01_UIConfirm") != null &&
                      Resources.Load<AudioClip>("Audio/Music/BGM01_RiverMist") != null,
                    "Delivered note/SFX/BGM clips load from Resources.");

                // Bus gains: applied exactly once.
                session.Save.SetVolumes(1f, 0.5f, 0.5f);
                audio.ApplyVolumes(session.Save);
                Check(Mathf.Abs(audio.AppliedMusicVolume - 0.5f) < 0.01f,
                    "Music gain = master*music once (" + audio.AppliedMusicVolume.ToString("F2") + ").");
                Check(Mathf.Abs(audio.AppliedSfxVolume - 0.5f) < 0.01f,
                    "SFX gain = master*sfx once (" + audio.AppliedSfxVolume.ToString("F2") + ").");

                // Master 0 must be silent, not "treated as 1".
                session.Save.SetVolumes(0f, 1f, 1f);
                audio.ApplyVolumes(session.Save);
                Check(Mathf.Approximately(audio.AppliedMusicVolume, 0f) &&
                      Mathf.Approximately(audio.AppliedSfxVolume, 0f),
                    "Master volume 0 silences both buses.");
                session.Save.SetVolumes(1f, 0.5f, 0.5f);
                audio.ApplyVolumes(session.Save);

                // Notes duck.
                audio.SetNotesActive(true);
                Check(audio.MusicDuck < 0.3f && Mathf.Abs(audio.AppliedMusicVolume - 0.125f) < 0.01f,
                    "Notes duck lowers the music to 25% (" + audio.AppliedMusicVolume.ToString("F3") + ").");
                audio.SetNotesActive(false);
                Check(Mathf.Abs(audio.MusicDuck - 1f) < 0.01f, "Leaving the notes restores the music duck.");

                // Music playback + pause/resume.
                audio.PlayMusic("BGM01_RiverMist");
                yield return Wait(0.2f);
                Check(audio.MusicIsPlaying && audio.CurrentMusicId == "BGM01_RiverMist",
                    "Music starts and reports its id.");
                audio.SetGameplayPaused(true);
                yield return Wait(0.1f);
                Check(!audio.MusicIsPlaying, "Pausing stops the music source.");
                audio.PlaySfx("SFX01_UIConfirm");
                yield return Wait(0.1f);
                Check(!audio.SfxIsPlaying, "No new gameplay SFX plays while paused.");
                audio.SetGameplayPaused(false);
                yield return Wait(0.1f);
                Check(audio.MusicIsPlaying, "Resuming continues the paused music.");

                // One-shot and loop.
                audio.PlaySfx("SFX03_Pickup");
                yield return Wait(0.05f);
                Check(audio.SfxIsPlaying, "A one-shot SFX plays on the shared bus.");
                audio.PlaySfxLoop("SFX06_Rowing");
                yield return Wait(0.1f);
                Check(audio.LoopIsPlaying, "The rowing loop starts on the loop source.");
                audio.StopSfxLoop("SFX06_Rowing");
                yield return Wait(0.1f);
                Check(!audio.LoopIsPlaying, "Stopping the loop actually stops it.");

                SaveService.TestSaveDirectoryOverride = null;
                Finish();
            }

            private IEnumerator Wait(float seconds)
            {
                float t = 0f;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                report.AppendLine();
                report.AppendLine(ok ? "DZ_AUDIO=PASS" : "DZ_AUDIO=FAIL");
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Duzhe] audio report write failed: " + e.Message);
                }
                Debug.Log(report.ToString());
                if (ok) Debug.Log("DZ_AUDIO=PASS");
                else Debug.LogError("DZ_AUDIO=FAIL");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
