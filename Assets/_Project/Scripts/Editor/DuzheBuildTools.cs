using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Windows x64 player build. Runs the setup first so the build always
    /// uses registered scenes, then reports the true outcome to
    /// Logs/duzhe_build.txt - never claims success without BuildPlayer's own
    /// result. Mono scripting backend is used (IL2CPP module is not
    /// installed); if the module turns out to be required, the reported error
    /// tells the exact install action.
    public static class DuzheBuildTools
    {
        private const string OutputRoot = "Builds/Candidates";
        private const string ExeName = "Duzhe";

        [MenuItem("渡者/构建/Windows x64")]
        public static void BuildWindows()
        {
            var reportText = new StringBuilder();
            bool ok = Build(reportText);
            string report = reportText.ToString();
            Debug.Log(report);
            WriteReport(ok, report);
            if (ok) Debug.Log("DZ_BUILD=OK");
            else Debug.LogError("DZ_BUILD=FAIL");
        }

        public static void BuildWindowsFromCommandLine()
        {
            var reportText = new StringBuilder();
            bool ok = Build(reportText);
            string report = reportText.ToString();
            Debug.Log(report);
            WriteReport(ok, report);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(StringBuilder report)
        {
            // Full setup + validation first, and CHECK the result: a build
            // must never ship scenes whose assembly/import step failed.
            if (!ProjectSetupUtility.RunAll(report))
            {
                report.AppendLine("FAIL: setup/validation failed; build not started.");
                return false;
            }
            AssetDatabase.SaveAssets();

            string[] scenes = ProjectSetupUtility.GetScenePaths();
            if (scenes == null || scenes.Length < 7)
            {
                report.AppendLine("FAIL: expected MainMenu + 6 scenes in Build Settings, got " +
                    (scenes == null ? 0 : scenes.Length) + ".");
                return false;
            }
            foreach (string scene in scenes)
            {
                if (!File.Exists(scene))
                {
                    report.AppendLine("FAIL: scene file missing on disk: " + scene);
                    return false;
                }
            }
            if (Array.IndexOf(scenes, "Assets/_Project/Scenes/MainMenu.unity") < 0)
            {
                report.AppendLine("FAIL: MainMenu is not part of the build scene list.");
                return false;
            }

            string projectRoot = Path.GetDirectoryName(Path.GetFullPath("Assets"));
            if (string.IsNullOrEmpty(projectRoot))
            {
                report.AppendLine("FAIL: project root is unavailable.");
                return false;
            }
            // Every build lands in its own timestamped folder. Previous
            // playable builds stay where they are so a bad build can never
            // remove the version the user is testing.
            string buildId = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" +
                Guid.NewGuid().ToString("N").Substring(0, 8);
            string outputDir = Path.Combine(projectRoot, OutputRoot, buildId);
            string exePath = Path.Combine(outputDir, ExeName + ".exe");
            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL: could not create build dir " + outputDir + ": " + e.Message);
                return false;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            report.AppendLine("Build id: " + buildId);
            report.AppendLine("Building " + scenes.Length + " scenes -> " + exePath);
            BuildReport result;
            try
            {
                result = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL: BuildPipeline threw: " + e.Message);
                WriteReleaseReport(buildId, false, report);
                return false;
            }
            report.AppendLine("Build result: " + result.summary.result +
                " | errors=" + result.summary.totalErrors +
                " | size=" + result.summary.totalSize + " bytes" +
                " | time=" + result.summary.totalTime);

            foreach (BuildStep step in result.steps)
            {
                if (step.messages != null)
                {
                    foreach (BuildStepMessage m in step.messages)
                    {
                        if (m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Assert)
                        {
                            report.AppendLine("ERROR [" + step.name + "] " + m.content);
                        }
                    }
                }
            }

            bool ok = result.summary.result == BuildResult.Succeeded &&
                      result.summary.totalErrors == 0 && File.Exists(exePath);
            if (!ok)
            {
                report.AppendLine("If the error mentions a missing build module, install it with:");
                report.AppendLine("  tuanjie install-modules 1.10.4 --module <module-id>");
            }
            else
            {
                report.AppendLine("Build output directory: " + outputDir);
            }
            WriteReleaseReport(buildId, ok, report);
            return ok;
        }

        /// Per-build report under Logs/Release/<buildId>/ (kept for the
        /// handoff even when the build fails).
        private static void WriteReleaseReport(string buildId, bool ok, StringBuilder report)
        {
            try
            {
                string dir = Path.Combine("Logs", "Release", buildId);
                Directory.CreateDirectory(dir);
                report.AppendLine("RESULT: " + (ok ? "SUCCEEDED" : "FAILED"));
                File.WriteAllText(Path.Combine(dir, "build_report.txt"), report.ToString(), Encoding.UTF8);
                report.AppendLine("Release report: " + Path.GetFullPath(Path.Combine(dir, "build_report.txt")));
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] Could not write release report: " + e.Message);
            }
        }

        private static void WriteReport(bool ok, string report)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/duzhe_build.txt", report, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] Could not write build report: " + e.Message);
            }
        }
    }
}
