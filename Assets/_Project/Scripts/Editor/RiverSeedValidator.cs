using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Chapters;
using Duzhe.River;
using UnityEditor;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Batch-runnable reachability validation for the river crossing.
    /// The simulated boat reacts ONLY to what a real player can see: a wave
    /// becomes known when its whirlpools enter the visible band
    /// (arrivalTime - VisibleWindow), plus the 0.5s reaction delay - the old
    /// version cheated by reacting at spawn time (outside the screen) and is
    /// deliberately fixed. Collision is checked along the whole fall using
    /// the real box edges, not only the arrival instant.
    /// Both rule sets are verified: maxBanned=1 (CH01/DEV) and maxBanned=2
    /// (CH02-04), 32 seeds each, so the report covers the formal chapters'
    /// reachability without pretending a single DEV run covers everything.
    public static class RiverSeedValidator
    {
        private const int SeedCount = 32;
        private const string ReportPath = "Logs/river_seeds_report.txt";

        [MenuItem("渡者/河流种子可达性验证")]
        public static void RunInteractive()
        {
            var reportText = new StringBuilder();
            bool ok = Run(reportText);
            string text = reportText.ToString();
            Debug.Log(text);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, text, Encoding.UTF8);
            if (ok) Debug.Log("DZ_RIVER_SEEDS=PASS");
            else Debug.LogError("DZ_RIVER_SEEDS=FAIL (see " + ReportPath + ")");
        }

        public static void RunFromCommandLine()
        {
            var reportText = new StringBuilder();
            bool ok = Run(reportText);
            string text = reportText.ToString();
            Debug.Log(text);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, text, Encoding.UTF8);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Run(StringBuilder report)
        {
            report.AppendLine("== Duzhe river seed reachability validation (visibility-fair model) ==");
            report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            bool allOk = true;
            RiverSettings s1 = ChapterCatalog.GetChapter("CH01").riverSettings;
            foreach (string id in new[] { "CH01", "CH02", "CH03", "CH04" })
                allOk &= RunGroup(report, id + " updated diagonal rules", ChapterCatalog.GetChapter(id).riverSettings);

            float visibleWindow = RiverRules.VisibleWindow(s1);
            report.AppendLine();
            report.AppendLine("Model notes: reaction starts at arrivalTime - " +
                visibleWindow.ToString("F3") + "s (visible band) + " +
                RiverRules.ReactionTimeSeconds.ToString("F1") + "s reaction; " +
                "spawnLead=" + s1.spawnLeadSeconds + "s only decides spawn position " +
                "(outside the visible band) and is NOT used as reaction knowledge.");
            report.AppendLine(allOk
                ? "DZ_RIVER_SEEDS=PASS (" + (SeedCount * 4) + " seeds across four chapters verified)"
                : "DZ_RIVER_SEEDS=FAIL");
            return allOk;
        }

        private static RiverSettings CloneSettings(RiverSettings src)
        {
            RiverSettings s = new RiverSettings();
            s.passengerCount = src.passengerCount;
            s.maxBannedLanesPerWave = src.maxBannedLanesPerWave;
            s.sailDuration = src.sailDuration;
            s.forwardSpeed = src.forwardSpeed;
            s.steerSpeed = src.steerSpeed;
            s.steerRangeX = src.steerRangeX;
            s.laneSpacing = src.laneSpacing;
            s.waveArrivalTimes = (float[])src.waveArrivalTimes.Clone();
            s.spawnLeadSeconds = src.spawnLeadSeconds;
            s.boatHalfWidth = src.boatHalfWidth;
            s.boatHalfHeight = src.boatHalfHeight;
            s.whirlHalfWidth = src.whirlHalfWidth;
            s.whirlHalfHeight = src.whirlHalfHeight;
            s.whirlJitterX = src.whirlJitterX;
            s.cameraYOffset = src.cameraYOffset;
            s.whirlDriftSpeed = src.whirlDriftSpeed;
            return s;
        }

        private static bool RunGroup(StringBuilder report, string title, RiverSettings s)
        {
            report.AppendLine("-- " + title + " --");
            report.AppendLine("Settings: sail=" + s.sailDuration + "s fwd=" + s.forwardSpeed +
                " steer=" + s.steerSpeed + " lanes=" + s.laneSpacing +
                " maxBanned=" + s.maxBannedLanesPerWave +
                " spawnLead=" + s.spawnLeadSeconds +
                " visibleWindow=" + RiverRules.VisibleWindow(s).ToString("F3") + "s");
            bool groupOk = true;
            for (int seed = 1; seed <= SeedCount; seed++)
            {
                List<RiverWave> waves = RiverRules.PlanWaves(seed, s);
                string problem = RiverRules.ValidatePlan(waves, s);
                if (problem == null)
                {
                    problem = SimulateBoat(waves, s);
                }

                if (problem == null)
                {
                    report.Append("PASS seed " + seed + "  waves:");
                    foreach (RiverWave w in waves)
                    {
                        report.Append(" [t=" + w.arrivalTime.ToString("F0")
                            + " safe=" + w.safeLane
                            + " banned=" + (w.bannedLanes.Length == 0 ? "-" : string.Join(",", Array.ConvertAll(w.bannedLanes, l => l.ToString()))) + "]");
                    }
                    report.AppendLine();
                }
                else
                {
                    groupOk = false;
                    report.AppendLine("FAIL seed " + seed + ": " + problem);
                }
            }
            report.AppendLine(groupOk
                ? "GROUP RESULT: PASS (" + SeedCount + " seeds)"
                : "GROUP RESULT: FAIL");
            report.AppendLine();
            return groupOk;
        }

        /// Simulates the boat position over the whole run at fixed 50Hz steps
        /// with the visibility-fair reaction model:
        /// - a wave becomes known when its whirlpools ENTER the visible band,
        ///   i.e. at arrivalTime - VisibleWindow (not at spawn time);
        /// - the boat starts steering one reaction delay after that;
        /// - collision is checked every step against every whirlpool of every
        ///   wave over its ENTIRE descent (box edges, not arrival instant).
        private static string SimulateBoat(List<RiverWave> waves, RiverSettings s)
        {
            const float step = 0.02f;
            float visibleWindow = RiverRules.VisibleWindow(s);
            float boatLineY = -3f;
            float boatX = 0f;

            for (int i = 0; i < waves.Count; i++)
            {
                RiverWave w = waves[i];
                // The boat learns about the wave only when it is visible.
                float awareAt = Mathf.Max(0f, w.arrivalTime - visibleWindow + RiverRules.ReactionTimeSeconds);

                // Steer from wherever the boat is to the safe lane, arriving
                // no later than the wave reaches the boat line.
                float target = RiverRules.LaneX(w.safeLane, s.laneSpacing);
                float travel = Mathf.Abs(target - boatX);
                float timeNeeded = travel / s.steerSpeed;
                if (awareAt + timeNeeded > w.arrivalTime)
                {
                    return "seed wave " + i + " unreachable: aware at t=" +
                        awareAt.ToString("F2") + ", needs " + timeNeeded.ToString("F2") +
                        "s, wave arrives at " + w.arrivalTime.ToString("F0") +
                        "s (boat at " + boatX.ToString("F2") + " -> lane " + w.safeLane + ")";
                }
                boatX = target;
            }

            // Full-descent collision sweep: the boat follows the safe-lane
            // schedule computed above; check box overlap against every
            // whirlpool of every wave during its whole fall.
            float[] boatKeyframes = BuildBoatSchedule(waves, s, visibleWindow);
            for (float t = 0f; t <= s.sailDuration + 0.001f; t += step)
            {
                float bx = BoatXAt(boatKeyframes, waves, s, t);
                for (int i = 0; i < waves.Count; i++)
                {
                    RiverWave w = waves[i];
                    float spawnAt = Mathf.Max(0f, w.arrivalTime - s.spawnLeadSeconds);
                    if (t < spawnAt) continue;
                    float whirlY = (boatLineY + s.forwardSpeed * s.spawnLeadSeconds) -
                        s.forwardSpeed * (t - spawnAt);
                    // Behind the boat line and past it = irrelevant.
                    if (whirlY < boatLineY - 1f) continue;
                    if (Mathf.Abs(whirlY - boatLineY) >= s.whirlHalfHeight + s.boatHalfHeight) continue;
                    for (int b = 0; b < w.bannedLanes.Length; b++)
                    {
                        if (Mathf.Abs((w.whirlXs[b] + w.whirlVelocitiesX[b] * (t - w.arrivalTime)) - bx) < s.whirlHalfWidth + s.boatHalfWidth)
                        {
                            return "simulated boat overlaps whirlpool of wave " + i +
                                " at t=" + t.ToString("F2") + " boatX=" + bx.ToString("F2") +
                                " whirlX=" + w.whirlXs[b].ToString("F2");
                        }
                    }
                }
            }
            return null;
        }

        /// Keyframed boat trajectory: stays at the previous safe lane until a
        /// wave becomes visible(+reaction), then moves at steerSpeed to the
        /// next safe lane. Returns [t0, x0, t1, x1, ...] pairs.
        private static float[] BuildBoatSchedule(List<RiverWave> waves, RiverSettings s, float visibleWindow)
        {
            List<float> keys = new List<float> { 0f, 0f };
            float x = 0f;
            foreach (RiverWave w in waves)
            {
                float awareAt = Mathf.Max(0f, w.arrivalTime - visibleWindow + RiverRules.ReactionTimeSeconds);
                float target = RiverRules.LaneX(w.safeLane, s.laneSpacing);
                float travel = Mathf.Abs(target - x) / s.steerSpeed;
                if (travel > 0.0001f)
                {
                    keys.Add(awareAt);
                    keys.Add(x);
                    keys.Add(awareAt + travel);
                    keys.Add(target);
                }
                x = target;
            }
            return keys.ToArray();
        }

        private static float BoatXAt(float[] keys, List<RiverWave> waves, RiverSettings s, float t)
        {
            if (keys.Length < 2) return 0f;
            for (int i = keys.Length - 2; i >= 0; i -= 2)
            {
                if (t >= keys[i])
                {
                    float t0 = keys[i], x0 = keys[i + 1];
                    if (i + 3 >= keys.Length) return x0;
                    float t1 = keys[i + 2], x1 = keys[i + 3];
                    if (t1 <= t0) return x1;
                    return Mathf.Lerp(x0, x1, Mathf.Clamp01((t - t0) / (t1 - t0)));
                }
            }
            return keys[1];
        }
    }
}
